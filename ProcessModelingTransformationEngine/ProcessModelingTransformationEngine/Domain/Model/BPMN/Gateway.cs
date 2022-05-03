namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class Gateway : Node
{
    public override bool IsMultiTarget => true;
    
    public Gateway(int id) : base(id)
    { }
}