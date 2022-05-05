using ProcessModelingTransformationEngine.API.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.Model.BPMN;
using BpmnTask = ProcessModelingTransformationEngine.Domain.Model.BPMN.Task;

namespace ProcessModelingTransformationEngine.API.Model.Converters;

public class BpmnModelToDto
{
    public BpmnDto ModelToDto(Bpmn bpmn)
    {
        var startEventDto = new StartEventDto() { Id = bpmn.StartEvent.Id };
        var bpmnDto = new BpmnDto() 
        { 
            Id = bpmn.Id, Name = bpmn.Name,
            StartEvent = startEventDto,
            EndEvents = new List<EndEventDto>(),
            IntermediateEvents = new List<IntermediateEventDto>(),
            SequenceFlows = new List<SequenceFlowDto>(),
            Tasks = new List<TaskDto>(),
            AndGateways = new List<ParallelGatewayDto>(),
            XorGateways = new List<ExclusiveGatewayDto>()
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
            case StartEvent:
                continue;
            case EndEvent endEvent:
                bpmnDto.EndEvents.Add(
                    new EndEventDto() { Id = endEvent.Id });
                break;
            case IntermediateEvent intermediateEvent:
                bpmnDto.IntermediateEvents.Add(
                    new IntermediateEventDto() { Id = intermediateEvent.Id });
                break;
            case BpmnTask task:
                bpmnDto.Tasks.Add(new TaskDto() { Id = task.Id, Name = task.Name });
                break;
            case ParallelGateway andGateway:
                bpmnDto.AndGateways.Add(new ParallelGatewayDto() { Id = andGateway.Id });
                break;
            case ExclusiveGateway xorGateway:
                bpmnDto.XorGateways.Add(new ExclusiveGatewayDto() { Id = xorGateway.Id });
                break;
            default:
                throw new NotSupportedException($"Unknown node type {node.GetType()}");
            }
        }
        return bpmnDto;
    }
}