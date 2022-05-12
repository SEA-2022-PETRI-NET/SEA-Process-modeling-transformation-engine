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
using ProcessModelingTransformationEngine.Application;
using ProcessModelingTransformationEngine.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.Model.PetriNet;
using Xunit;

using Task = System.Threading.Tasks.Task;
using BpmnTask = ProcessModelingTransformationEngine.Domain.Model.BPMN.Task;

namespace ProcessModelingTransformationEngine.Tests;

public class BpmnToPetriNetTest
{
    private int curId = 0;
    
    private int GenId()
    {
        return curId++;
    }

    private Dictionary<int, object> IdToNode(PetriNet petriNetDto)
    {
        var idToNode = new Dictionary<int, object>();
        foreach (var place in petriNetDto.Places)
        {
            idToNode[place.Id] = place;
        }

        foreach (var transition in petriNetDto.Transitions)
        {
            idToNode[transition.Id] = transition;
        }

        return idToNode;
    }

    private T AssertCast<T>(object obj)
    {
        Assert.IsType<T>(obj);
        return (T)obj;
    }

    private Transition AssertTransition(object node)
    {
        return AssertCast<Transition>(node);
    }

    private Place AssertPlace(object node)
    {
        var place = AssertCast<Place>(node);
        Assert.Equal(place.Name == "start" ? 1 : 0, place.NumberOfTokens);
        return place;
    }

    // Assert a chain of ...Transition - Place - Transition... chainlength
    // TStart is type of start node and TEnd is type of final node
    private TEnd AssertNodeChain<TStart, TEnd>(PetriNet petriNetDto, 
        Dictionary<int, object> idToNode, int fromId, int chainLength)
    {
        var nodeType = typeof(TStart);
        object outgoingNode = idToNode[fromId];
        for (int i = 0; i < chainLength; i++)
        {
            if (nodeType == typeof(Place))
            {
                fromId = AssertPlace(outgoingNode).Id;
                nodeType = typeof(Transition);
            }
            else if (nodeType == typeof(Transition))
            {
                fromId = AssertTransition(outgoingNode).Id;
                nodeType = typeof(Place);
            } 
            else
            {
                throw new ArgumentException("Invalid petri net node type " + nodeType);
            }
            if (i != chainLength - 1)
            {
                outgoingNode = 
                    OutgoingNodes(petriNetDto, idToNode, fromId, 1).First();
            }
        }

        return (TEnd)outgoingNode;
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

    private void GetPetriNetInfo(PetriNet petriNetDto, 
        out Dictionary<int, object> idToNode, out Place startPlace)
    {
        idToNode = IdToNode(petriNetDto);
        startPlace = FindStartPlace(petriNetDto);
        Assert.Equal(1, startPlace.NumberOfTokens);
    }
    
    [Fact]
    public async Task SimplePositiveTest()
    {
        var bpmnDto = CreateBpmnDto(numEndEvents: 1, numTasks: 1);
        Connect(bpmnDto, bpmnDto.StartEvent.Id, bpmnDto.Tasks[0].Id);
        Connect(bpmnDto, bpmnDto.Tasks[0].Id, bpmnDto.EndEvents.First().Id);
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var endTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, startPlace.Id, 2 + 2 + 2);
        Assert.StartsWith("end", endTrans.Name);
        OutgoingIds(petriNetDto, endTrans.Id, 0);
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
        
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var forkTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, startPlace.Id, 2 + 2);
        var forkOutgoingIds = OutgoingIds(petriNetDto, forkTrans.Id, 3);
        var joinTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[0], 4);
        var joinTransTemp = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[1], 4);
        Assert.Equal(joinTrans, joinTransTemp);
        joinTransTemp = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[2], 4);
        Assert.Equal(joinTrans, joinTransTemp);
        var endTrans = AssertNodeChain<Transition, Transition>(petriNetDto,
            idToNode, joinTrans.Id, 1 + 2);
        Assert.StartsWith("end", endTrans.Name);
        OutgoingIds(petriNetDto, endTrans.Id, 0);
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
        
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var forkTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, startPlace.Id, 2 + 2);
        var forkOutgoingIds = OutgoingIds(petriNetDto, forkTrans.Id, 3);
        var endTrans1 = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[0], 4);
        Assert.StartsWith("end", endTrans1.Name);
        OutgoingIds(petriNetDto, endTrans1.Id, 0);
        var joinTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[1], 4);
        var joinTransTemp = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, forkOutgoingIds[2], 4);
        Assert.Equal(joinTrans, joinTransTemp);
        var endTrans2 = AssertNodeChain<Transition, Transition>(petriNetDto,
            idToNode, joinTrans.Id, 1 + 2);
        Assert.StartsWith("end", endTrans2.Name);
        OutgoingIds(petriNetDto, endTrans2.Id, 0);
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
        
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var forkPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, startPlace.Id, 2 + 1);
        var forkOutgoingIds = OutgoingIds(petriNetDto, forkPlace.Id, 3);
        var joinPlace = AssertNodeChain<Transition, Place>(
            petriNetDto, idToNode, forkOutgoingIds[0], 4);
        var joinPlaceTemp = AssertNodeChain<Transition, Place>(
            petriNetDto, idToNode, forkOutgoingIds[1], 4);
        Assert.Equal(joinPlace, joinPlaceTemp);
        joinPlaceTemp = AssertNodeChain<Transition, Place>(
            petriNetDto, idToNode, forkOutgoingIds[2], 4);
        Assert.Equal(joinPlace, joinPlaceTemp);
        var endTrans = AssertNodeChain<Place, Transition>(petriNetDto,
            idToNode, joinPlace.Id, 2 + 2);
        Assert.StartsWith("end", endTrans.Name);
        OutgoingIds(petriNetDto, endTrans.Id, 0);
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
        
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var forkPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, startPlace.Id, 2 + 1);
        var joinPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, forkPlace.Id, 2 + 2 + 2 + 1);
        var joinOutgoingIds = OutgoingIds(petriNetDto, joinPlace.Id, 2);
        var forkPlaceTemp = AssertNodeChain<Transition, Place>(
            petriNetDto, idToNode, joinOutgoingIds[0], 2);
        Assert.Equal(forkPlace, forkPlaceTemp);
        var endTrans = AssertNodeChain<Transition, Transition>(petriNetDto,
            idToNode, joinOutgoingIds[1], 1 + 2);
        Assert.StartsWith("end", endTrans.Name);
        OutgoingIds(petriNetDto, endTrans.Id, 0);
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

        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var xorForkPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, startPlace.Id, 2 + 1);
        var andForkTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, xorForkPlace.Id, 2 + 2);
        var andForkOutgoingIds = OutgoingIds(petriNetDto, andForkTrans.Id, 2);
        var andJoinTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, andForkOutgoingIds[0], 4);
        var andJoinTransTemp = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, andForkOutgoingIds[1], 4);
        Assert.Equal(andJoinTrans, andJoinTransTemp);
        var xorJoinPlace = AssertNodeChain<Transition, Place>(
            petriNetDto, idToNode, andJoinTrans.Id, 1 + 1);
        var endTrans = AssertNodeChain<Place, Transition>(petriNetDto,
            idToNode, xorJoinPlace.Id, 2 + 2);
        Assert.StartsWith("end", endTrans.Name);
        OutgoingIds(petriNetDto, endTrans.Id, 0);
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
        
        PetriNet petriNetDto = await CallAndDeserializeBpmnToPetriNet(bpmnDto);
        GetPetriNetInfo(petriNetDto, out var idToNode, out var startPlace);
        var andForkTrans = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, startPlace.Id, 2 + 2);
        var andForkOutgoingIds = OutgoingIds(petriNetDto, andForkTrans.Id, 2);
        var endTrans1 = AssertNodeChain<Place, Transition>(
            petriNetDto, idToNode, andForkOutgoingIds[0], 2);
        Assert.StartsWith("end", endTrans1.Name);
        OutgoingIds(petriNetDto, endTrans1.Id, 0);
        var xorForkPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, andForkOutgoingIds[1], 1);
        Assert.StartsWith("exclusive", xorForkPlace.Name);
        var xorJoinPlace = AssertNodeChain<Place, Place>(
            petriNetDto, idToNode, xorForkPlace.Id, 2 + 2 + 1);
        Assert.StartsWith("exclusive", xorJoinPlace.Name);
        var xorJoinOutgoingIds = OutgoingIds(petriNetDto, xorJoinPlace.Id, 2);
        var andForkTransTemp = AssertNodeChain<Transition, Transition>(
            petriNetDto, idToNode, xorJoinOutgoingIds[0], 3);
        Assert.Equal(andForkTrans, andForkTransTemp);
        var andJoinTrans = AssertNodeChain<Transition, Transition>(
            petriNetDto, idToNode, xorJoinOutgoingIds[1], 1 + 2);
        Assert.StartsWith("parallel", andJoinTrans.Name);
        var endTrans2 = AssertNodeChain<Transition, Transition>(petriNetDto,
            idToNode, andJoinTrans.Id, 1 + 2);
        Assert.StartsWith("end", endTrans2.Name);
        OutgoingIds(petriNetDto, endTrans2.Id, 0);
    }
}