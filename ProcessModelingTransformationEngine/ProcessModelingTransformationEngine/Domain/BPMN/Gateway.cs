using System.Diagnostics;

namespace ProcessModelingTransformationEngine.Domain.BPMN;

public class Gateway : Node
{
    public override bool IsMultiTarget => true;
    
    public Gateway(int id) : base(id)
    { }
}