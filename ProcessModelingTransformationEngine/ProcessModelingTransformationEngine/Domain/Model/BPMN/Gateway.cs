namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class Gateway : Node
{
    public override bool IsMultiSource => true;
    public override bool IsMultiTarget => true;
    
    public Gateway(int id) : base(id)
    { }
}