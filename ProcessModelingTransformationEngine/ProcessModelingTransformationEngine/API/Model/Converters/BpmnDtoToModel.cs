using ProcessModelingTransformationEngine.API.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.BPMN;
using BpmnTask = ProcessModelingTransformationEngine.Domain.BPMN.Task;

namespace ProcessModelingTransformationEngine.API.Model.Converters;

public class BpmnDtoToModel
{
    public Bpmn DtoToModel(BpmnDto bpmnDto)
    {
        var startEvent = new StartEvent(bpmnDto.StartEvent.Id);
        var endEvent = new EndEvent(bpmnDto.EndEvent.Id);
        var bpmn = new Bpmn(bpmnDto.Id, bpmnDto.Name, startEvent, endEvent);

        var idToNode = new Dictionary<int, Node>();
        foreach (var intermediateEventDto in bpmnDto.IntermediateEvents)
        {
            idToNode.Add(intermediateEventDto.Id, 
                new IntermediateEvent(intermediateEventDto.Id));
        }
        foreach (var taskDto in bpmnDto.Tasks)
        {
            idToNode.Add(taskDto.Id, new BpmnTask(taskDto.Id, taskDto.Name));
        }
        foreach (var andGateWayDto in bpmnDto.AndGateways)
        {
            idToNode.Add(andGateWayDto.Id, new AndGateway(andGateWayDto.Id));
        }
        foreach (var xorGateWayDto in bpmnDto.XorGateways)
        {
            idToNode.Add(xorGateWayDto.Id, new XorGateway(xorGateWayDto.Id));
        }

        foreach (var flowDto in bpmnDto.SequenceFlows)
        {
            bpmn.AddFlow(new SequenceFlow(flowDto.Id, flowDto.Name, 
                idToNode[flowDto.SourceId], idToNode[flowDto.TargetId]));
        }

        return bpmn;
    }
}