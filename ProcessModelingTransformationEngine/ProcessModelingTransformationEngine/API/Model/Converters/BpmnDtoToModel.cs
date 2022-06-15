using SEA_Models.BPMN;
using SEA_Models.Domain.Model.BPMN;
using BpmnTask = SEA_Models.BPMN.Task;

namespace ProcessModelingTransformationEngine.API.Model.Converters;

public class BpmnDtoToModel
{
    public Bpmn DtoToModel(BpmnDto bpmnDto)
    {
        var startEvent = new StartEvent(bpmnDto.StartEvent.Id);
        var bpmn = new Bpmn(bpmnDto.Id, bpmnDto.Name, startEvent);

        var idToNode = new Dictionary<int, Node>();
        idToNode.Add(startEvent.Id, startEvent);
        foreach (var endEventDto in bpmnDto.EndEvents)
        {
            idToNode.Add(endEventDto.Id, 
                new EndEvent(endEventDto.Id));
        }
        foreach (var intermediateEventDto in bpmnDto.IntermediateEvents)
        {
            idToNode.Add(intermediateEventDto.Id, 
                new IntermediateEvent(intermediateEventDto.Id));
        }
        foreach (var taskDto in bpmnDto.Tasks)
        {
            idToNode.Add(taskDto.Id, new BpmnTask(taskDto.Id, taskDto.Name));
        }
        foreach (var andGateWayDto in bpmnDto.ParallelGateways)
        {
            idToNode.Add(andGateWayDto.Id, new ParallelGateway(andGateWayDto.Id));
        }
        foreach (var xorGateWayDto in bpmnDto.ExclusiveGateways)
        {
            idToNode.Add(xorGateWayDto.Id, new ExclusiveGateway(xorGateWayDto.Id));
        }

        foreach (var flowDto in bpmnDto.SequenceFlows)
        {
            bpmn.AddFlow(new SequenceFlow(flowDto.Id, flowDto.Name, 
                idToNode[flowDto.SourceId], idToNode[flowDto.TargetId]));
        }

        return bpmn;
    }
}