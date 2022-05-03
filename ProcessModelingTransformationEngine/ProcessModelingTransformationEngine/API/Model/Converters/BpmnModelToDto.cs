using ProcessModelingTransformationEngine.API.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.BPMN;
using BpmnTask = ProcessModelingTransformationEngine.Domain.BPMN.Task;

namespace ProcessModelingTransformationEngine.API.Model.Converters;

public class BpmnModelToDto
{
    public BpmnDto ModelToDto(Bpmn bpmn)
    {
        var startEventDto = new StartEventDto() { Id = bpmn.StartEvent.Id };
        var endEventDto = new EndEventDto() { Id = bpmn.EndEvent.Id };
        var bpmnDto = new BpmnDto() 
        { 
            Id = bpmn.Id, Name = bpmn.Name,
            StartEvent = startEventDto, EndEvent = endEventDto,
            IntermediateEvents = new List<IntermediateEventDto>(),
            SequenceFlows = new List<SequenceFlowDto>(),
            Tasks = new List<TaskDto>(),
            AndGateways = new List<AndGatewayDto>(),
            XorGateways = new List<XorGatewayDto>()
        };
        foreach (var node in bpmn)
        {
            foreach (var flow in node.GetTargetFlows())
            {
                bpmnDto.SequenceFlows.Add(new SequenceFlowDto()
                {
                    Id = flow.Id, Name = flow.Name, 
                    SourceId = flow.Source.Id, TargetId = flow.Target.Id
                });
            }

            switch (node)
            {
            case StartEvent or EndEvent:
                continue;
            case IntermediateEvent intermediateEvent:
                bpmnDto.IntermediateEvents.Add(
                    new IntermediateEventDto() { Id = intermediateEvent.Id });
                break;
            case BpmnTask task:
                bpmnDto.Tasks.Add(new TaskDto() { Id = task.Id, Name = task.Name });
                break;
            case AndGateway andGateway:
                bpmnDto.AndGateways.Add(new AndGatewayDto() { Id = andGateway.Id });
                break;
            case XorGateway xorGateway:
                bpmnDto.XorGateways.Add(new XorGatewayDto() { Id = xorGateway.Id });
                break;
            default:
                throw new NotSupportedException($"Unknown node type {node.GetType()}");
            }
        }
        return bpmnDto;
    }
}