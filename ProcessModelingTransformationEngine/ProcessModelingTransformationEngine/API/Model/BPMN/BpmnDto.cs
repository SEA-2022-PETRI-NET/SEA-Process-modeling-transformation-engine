namespace SEA_Models.Domain.Model.BPMN;

public class BpmnDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public StartEventDto StartEvent { get; set; }
    public List<EndEventDto> EndEvents { get; set; }
    public List<IntermediateEventDto> IntermediateEvents { get; set; }
    public List<SequenceFlowDto> SequenceFlows { get; set; }
    public List<TaskDto> Tasks { get; set; }
    public List<ParallelGatewayDto> ParallelGateways { get; set; }
    public List<ExclusiveGatewayDto> ExclusiveGateways { get; set; }
}