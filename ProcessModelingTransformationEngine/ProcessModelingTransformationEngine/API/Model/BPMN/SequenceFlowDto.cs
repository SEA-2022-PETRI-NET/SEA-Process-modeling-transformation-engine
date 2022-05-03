namespace ProcessModelingTransformationEngine.API.Model.BPMN;

public class SequenceFlowDto : IBpmnElementDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public int SourceId { get; set; }
    public int TargetId { get; set; }
}