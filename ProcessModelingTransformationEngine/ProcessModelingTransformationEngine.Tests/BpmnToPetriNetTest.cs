using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using ProcessModelingTransformationEngine.API.Model.BPMN;
using SEA_Models.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.Application;
using SEA_Models.PetriNet;
using Xunit;

using Task = System.Threading.Tasks.Task;
using BpmnTask = SEA_Models.BPMN.Task;

namespace ProcessModelingTransformationEngine.Tests;

public class BpmnToPetriNetTest
{
    private int curId = 0;
    private BpmnDto curBpmnDto;
    private Dictionary<int, object> curIdToObject;
    private PetriNet curPetriNetDto;
    
    #region PetriUtils
    private int GenId()
    {
        return curId++;
    }

    private PetriNet CreatePetriNetDto()
    {
        var petriNetDto = new PetriNet()
        {
            Id = GenId(), Name = "PetriNet",
            Places = new List<Place>(),
            Transitions = new List<Transition>(),
            Arcs = new List<Arc>()
        };
        return petriNetDto;
    }

    private T AssertCast<T>(object obj)
    {
        Assert.IsType<T>(obj);
        return (T)obj;
    }

    private Transition AssertTransition(int id)
    {
        return AssertCast<Transition>(curIdToObject[id]);
    }

    private Place AssertPlace(int id)
    {
        var place = AssertCast<Place>(curIdToObject[id]);
        Assert.Equal(place.Name == "start" ? 1 : 0, place.NumberOfTokens);
        return place;
    }

    private int AssertStart(int id)
    {
        var place = AssertPlace(id);
        Assert.StartsWith("start", place.Name);
        int transId = OutgoingIds(curPetriNetDto, place.PlaceId, 1).First();
        var trans = AssertTransition(transId);
        return OutgoingIds(curPetriNetDto, trans.TransitionId, 1).First();
    }
    
    private void AssertEnd(int id)
    {
        var place = AssertPlace(id);
        int transId = OutgoingIds(curPetriNetDto, place.PlaceId, 1).First();
        var trans = AssertTransition(transId);
        Assert.StartsWith("end", trans.Name);
        OutgoingIds(curPetriNetDto, trans.TransitionId, 0);
    }
    
    private int AssertTask(int id)
    {
        var place = AssertPlace(id);
        int transId = OutgoingIds(curPetriNetDto, place.PlaceId, 1).First();
        var trans = AssertTransition(transId);
        Assert.StartsWith("task", trans.Name);
        return OutgoingIds(curPetriNetDto, trans.TransitionId, 1).First();
    }
    
    private int[] AssertParallel(int id, int expectedNumOutgoing, 
        out Transition parallelTrans)
    {
        var place = AssertPlace(id);
        int transId = OutgoingIds(curPetriNetDto, place.PlaceId, 1).First();
        parallelTrans = AssertTransition(transId);
        Assert.StartsWith("parallel", parallelTrans.Name);
        return OutgoingIds(curPetriNetDto, parallelTrans.TransitionId, 
            expectedNumOutgoing).ToArray();
    }
    
    private int[] AssertExclusive(int id, int expectedNumOutgoing, 
        out Place exclusivePlace)
    {
        exclusivePlace = AssertPlace(id);
        Assert.StartsWith("exclusive", exclusivePlace.Name);
        List<int> transIds = OutgoingIds(curPetriNetDto, exclusivePlace.PlaceId, 
            expectedNumOutgoing);
        List<int> placeIds = new List<int>(expectedNumOutgoing);
        foreach (int transId in transIds)
        {
            AssertTransition(transId);
            placeIds.Add(OutgoingIds(curPetriNetDto, transId, 1).First());
        }

        return placeIds.ToArray();
    }

    private Dictionary<int, object> IdToNode(PetriNet petriNetDto)
    {
        var idToNode = new Dictionary<int, object>();
        foreach (var place in petriNetDto.Places)
        {
            idToNode[place.PlaceId] = place;
        }

        foreach (var transition in petriNetDto.Transitions)
        {
            idToNode[transition.TransitionId] = transition;
        }

        return idToNode;
    }

    private Place FindStartPlace(PetriNet petriNetDto)
    {
        List<Place> startPlaces = petriNetDto.Places
            .Where(p => p.Name == "start").ToList();
        Assert.Single(startPlaces);
        return startPlaces.First();
    }

    private List<int> OutgoingIds(PetriNet petriNetDto,
        int sourceId, int expectedCount)
    {
        var ids = petriNetDto.Arcs.Where(a => a.SourceNode == sourceId)
            .Select(a => a.TargetNode).ToList();
        Assert.Equal(expectedCount, ids.Count);
        return ids;
    }

    private List<object> OutgoingNodes(PetriNet petriNetDto, 
        Dictionary<int, object> idToNode, int sourceId, int expectedCount)
    {
        return OutgoingIds(petriNetDto, sourceId, expectedCount)
            .Select(targetId => idToNode[targetId]).ToList();
    }
    
    private void GetPetriNetInfo(PetriNet petriNetDto, 
        out Dictionary<int, object> idToNode, out Place startPlace)
    {
        idToNode = IdToNode(petriNetDto);
        startPlace = FindStartPlace(petriNetDto);
        Assert.Equal(1, startPlace.NumberOfTokens);
    }
    #endregion
    
    #region BpmnUtils
    private BpmnDto CreateBpmnDto(int numEndEvents = 1, int numTasks = 0, 
        int numParallelGateways = 0, int numExclusiveGateways = 0)
    {
        var bpmnDto = new BpmnDto()
        {
            Id = GenId(), Name = "Bpmn",
            StartEvent = new StartEventDto() { Id = GenId() },
            EndEvents = new List<EndEventDto>(),
            IntermediateEvents = new List<IntermediateEventDto>(),
            SequenceFlows = new List<SequenceFlowDto>(),
            Tasks = new List<TaskDto>(),
            ExclusiveGateways = new List<ExclusiveGatewayDto>(),
            ParallelGateways = new List<ParallelGatewayDto>()
        };
        for (int i = 0; i < numEndEvents; i++)
        {
            AddEndEvent(bpmnDto);
        }
        for (int i = 0; i < numTasks; i++)
        {
            AddTask(bpmnDto);
        }
        for (int i = 0; i < numParallelGateways; i++)
        {
            AddParallelGateway(bpmnDto);
        }
        for (int i = 0; i < numExclusiveGateways; i++)
        {
            AddExclusiveGateway(bpmnDto);
        }
        return bpmnDto;
    }

    private EndEventDto AddEndEvent(BpmnDto bpmnDto)
    {
        int id = GenId();
        var endEvent = new EndEventDto() { Id = id };
        bpmnDto.EndEvents.Add(endEvent);
        return endEvent;
    }

    private TaskDto AddTask(BpmnDto bpmnDto)
    {
        int id = GenId();
        var task = new TaskDto() { Id = id, Name = "task" + id };
        bpmnDto.Tasks.Add(task);
        return task;
    }

    private ParallelGatewayDto AddParallelGateway(BpmnDto bpmnDto)
    {
        int id = GenId();
        var parallelGateway = new ParallelGatewayDto() { Id = id };
        bpmnDto.ParallelGateways.Add(parallelGateway);
        return parallelGateway;
    }
    
    private ExclusiveGatewayDto AddExclusiveGateway(BpmnDto bpmnDto)
    {
        int id = GenId();
        var exclusiveGateway = new ExclusiveGatewayDto() { Id = id };
        bpmnDto.ExclusiveGateways.Add(exclusiveGateway);
        return exclusiveGateway;
    }

    private SequenceFlowDto Connect(BpmnDto bpmnDto, int sourceId, int targetId)
    {
        int flowId = GenId();
        var flow = new SequenceFlowDto() { Id = flowId, Name = "f" + flowId, 
            SourceId = sourceId, TargetId = targetId };
        bpmnDto.SequenceFlows.Add(flow);
        return flow;
    }

    private List<SequenceFlowDto> Connect(BpmnDto bpmnDto, int sourceId, 
        int[] targetIds)
    {
        var flows = new List<SequenceFlowDto>();
        foreach (int targetId in targetIds)
        {
            flows.Add(Connect(bpmnDto, sourceId, targetId));
        }

        return flows;
    }
    
    private List<SequenceFlowDto> Connect(BpmnDto bpmnDto, int[] sourceIds,
        int targetId)
    {
        var flows = new List<SequenceFlowDto>();
        foreach (int sourceId in sourceIds)
        {
            flows.Add(Connect(bpmnDto, sourceId, targetId));
        }

        return flows;
    }
    #endregion

    #region HttpUtils
    private async Task<HttpResponseMessage> CallBpmnToPetriNet(BpmnDto bpmnDto)
    {
        await using var application = new WebApplicationFactory<Program>();
        var client = application.CreateClient();
        string json = JsonConvert.SerializeObject(bpmnDto);
        HttpResponseMessage response = 
            await client.PostAsync("api/v1/Transformation/bpmn-to-petri-net", 
                new StringContent(json, Encoding.UTF8, "application/json"));
        return response;
    }

    private async Task<PetriNet> CallAndDeserializeBpmnToPetriNet(BpmnDto bpmnDto)
    {
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.True(response.IsSuccessStatusCode, response.ToString());
        PetriNet? petriNetDto = await response.Content.ReadFromJsonAsync<PetriNet>();
        Assert.NotNull(petriNetDto);
        return petriNetDto;
    }
    #endregion

    #region PositiveTests
    [Fact]
    public async Task SimplePositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents.First().Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        id = AssertTask(id);
        AssertEnd(id);
    }
    
    [Fact]
    public async Task ParallelPositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 3, 
            numParallelGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 3)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds, bpmnDto.ParallelGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[1].Id, bpmnDto.EndEvents.First().Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        int[] forkOutIds = AssertParallel(id, 3, out var forkTrans);
        id = AssertTask(forkOutIds[0]);
        AssertParallel(id, 1, out var joinTrans);
        id = AssertTask(forkOutIds[1]);
        AssertParallel(id, 1, out var joinTransTemp);
        Assert.Equal(joinTrans, joinTransTemp);
        id = AssertTask(forkOutIds[2]);
        id = AssertParallel(id, 1, out joinTransTemp).First();
        Assert.Equal(joinTrans, joinTransTemp);
        AssertEnd(id);
    }
    
    [Fact]
    public async Task ParallelEndBranchPositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 2, numTasks: 3, 
            numParallelGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 3)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds[0], bpmnDto.EndEvents[0].Id);
        Connect(bpmnDto, taskIds.Take(new Range(1, 3)).ToArray(), 
            bpmnDto.ParallelGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[1].Id, bpmnDto.EndEvents[1].Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        int[] forkOutIds = AssertParallel(id, 3, out var forkTrans);
        id = AssertTask(forkOutIds[0]);
        AssertEnd(id);
        id = AssertTask(forkOutIds[1]);
        AssertParallel(id, 1, out var joinTrans);
        id = AssertTask(forkOutIds[2]);
        id = AssertParallel(id, 1, out var joinTransTemp).First();
        Assert.Equal(joinTrans, joinTransTemp);
        AssertEnd(id);
    }
    
    [Fact]
    public async Task ExclusivePositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 3, 
            numExclusiveGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ExclusiveGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 3)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds, bpmnDto.ExclusiveGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[1].Id, bpmnDto.EndEvents.First().Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        int[] forkOutIds = AssertExclusive(id, 3, out var forkPlace);
        id = AssertTask(forkOutIds[0]);
        AssertExclusive(id, 1, out var joinPlace);
        id = AssertTask(forkOutIds[1]);
        AssertExclusive(id, 1, out var joinPlaceTemp);
        Assert.Equal(joinPlace, joinPlaceTemp);
        id = AssertTask(forkOutIds[2]);
        id = AssertExclusive(id, 1, out joinPlaceTemp).First();
        Assert.Equal(joinPlace, joinPlaceTemp);
        AssertEnd(id);
    }
    
    [Fact]
    public async Task LoopPositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 2, 
            numExclusiveGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ExclusiveGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.Tasks[1].Id);
        Connect(bpmnDto, bpmnDto.Tasks[1].Id, bpmnDto.ExclusiveGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[1].Id, 
            new[] { bpmnDto.ExclusiveGateways[0].Id, bpmnDto.EndEvents.First().Id });
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        id = AssertExclusive(id, 1, out var forkPlace).First();
        id = AssertTask(id);
        id = AssertTask(id);
        int[] joinOutIds = AssertExclusive(id, 2, out var joinPlace);
        AssertExclusive(joinOutIds[0], 1, out var forkPlaceTemp);
        Assert.Equal(forkPlace, forkPlaceTemp);
        AssertEnd(joinOutIds[1]);
    }
    
    [Fact]
    public async Task ExclusiveParallelPositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 2, 
            numParallelGateways: 2, numExclusiveGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ExclusiveGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.ParallelGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds, bpmnDto.ParallelGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[1].Id, bpmnDto.ExclusiveGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[1].Id, bpmnDto.EndEvents.First().Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        id = AssertExclusive(id, 1, out var xorForkPlace).First();
        int[] andForkOutIds = AssertParallel(id, 2, out var forkTrans);
        id = AssertTask(andForkOutIds[0]);
        AssertParallel(id, 1, out var andJoinTrans);
        id = AssertTask(andForkOutIds[1]);
        id = AssertParallel(id, 1, out var andJoinTransTemp).First();
        Assert.Equal(andJoinTrans, andJoinTransTemp);
        id = AssertExclusive(id, 1, out var xorJoinPlace).First();
        AssertEnd(id);
    }

    [Fact]
    public async Task ParallelExclusivePositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 2, numTasks: 1, 
            numParallelGateways: 2, numExclusiveGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        int[] splitIds = new[] { bpmnDto.EndEvents[0].Id, bpmnDto.ExclusiveGateways[0].Id };
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, splitIds);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.ExclusiveGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[1].Id, 
            new[] { bpmnDto.ParallelGateways[0].Id, bpmnDto.ParallelGateways[1].Id });
        Connect(bpmnDto, bpmnDto.ParallelGateways[1].Id, bpmnDto.EndEvents[1].Id);
        
        curPetriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(curPetriNetDto, out curIdToObject, out var startPlace);
        int id = AssertStart(startPlace.PlaceId);
        int[] andForkOutIds = AssertParallel(id, 2, out var andForkTrans);
        AssertEnd(andForkOutIds[0]);
        id = AssertExclusive(andForkOutIds[1], 1, out var xorForkPlace).First();
        id = AssertTask(id);
        int[] xorJoinOutIds = AssertExclusive(id, 2, out var xorJoinPlace);
        AssertParallel(xorJoinOutIds[0], 2, out var andForkTransTemp);
        Assert.Equal(andForkTrans, andForkTransTemp);
        id = AssertParallel(xorJoinOutIds[1], 1, out var andJoinTrans).First();
        AssertEnd(id);
    }
    #endregion
    
    #region NegativeTests
    [Fact]
    public async Task UnconnectedNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1);

        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task NoStartEventNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 1);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents.First().Id);

        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task NoEndEventNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 0, numTasks: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);

        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task EndEventOutgoingNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 2, numTasks: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents[0].Id);
        Connect(bpmnDto, bpmnDto.EndEvents[0].Id, bpmnDto.EndEvents[1].Id);

        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task SelfLoopNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numExclusiveGateways: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ExclusiveGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.ExclusiveGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.EndEvents.First().Id);

        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task MultiSourceNonGatewayNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 3, 
            numParallelGateways: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 2)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds, bpmnDto.Tasks[2].Id);
        Connect(bpmnDto, bpmnDto.Tasks[2].Id, bpmnDto.EndEvents.First().Id);
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task MultiTargetNonGatewayNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 3, 
            numParallelGateways: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[2].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 2)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.Tasks[2].Id, taskIds);
        Connect(bpmnDto, taskIds, bpmnDto.ParallelGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, bpmnDto.EndEvents.First().Id);
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task ParallelNoOutgoingNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 1, 
            numParallelGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, bpmnDto.ParallelGateways[1].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents[0].Id);
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task ParallelMissingEndNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 3, 
            numParallelGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ParallelGateways[0].Id);
        int[] taskIds = bpmnDto.Tasks.Take(new Range(0, 3)).Select(t => t.Id).ToArray();
        Connect(bpmnDto, bpmnDto.ParallelGateways[0].Id, taskIds);
        Connect(bpmnDto, taskIds.Take(new Range(1, 3)).ToArray(), 
            bpmnDto.ParallelGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ParallelGateways[1].Id, bpmnDto.EndEvents[0].Id);
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task DuplicateIdNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents.First().Id);
        bpmnDto.SequenceFlows[1].Id = bpmnDto.SequenceFlows[0].Id;
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task DuplicateNameNegativeTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 2);
        bpmnDto.Tasks[1].Name = bpmnDto.Tasks[0].Name;
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.Tasks[1].Id);
        Connect(bpmnDto, bpmnDto.Tasks[1].Id, bpmnDto.EndEvents.First().Id);
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task DuplicateSequenceFlowTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 2, 
            numExclusiveGateways: 2);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.ExclusiveGateways[0].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[0].Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.Tasks[1].Id);
        Connect(bpmnDto, bpmnDto.Tasks[1].Id, bpmnDto.ExclusiveGateways[1].Id);
        Connect(bpmnDto, bpmnDto.ExclusiveGateways[1].Id, 
            new[] { bpmnDto.ExclusiveGateways[0].Id, bpmnDto.ExclusiveGateways[0].Id,
                bpmnDto.EndEvents.First().Id });
        
        HttpResponseMessage response = await CallBpmnToPetriNet(bpmnDto);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion
}