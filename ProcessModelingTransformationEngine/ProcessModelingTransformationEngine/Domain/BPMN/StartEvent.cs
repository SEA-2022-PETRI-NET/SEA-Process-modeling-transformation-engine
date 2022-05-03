namespace ProcessModelingTransformationEngine.Domain.BPMN;

public class StartEvent : Event
{
    public override bool IsMultiSource => false;
    
    public StartEvent(int id) : base(id)
    { }

    public override void AddSourceFlow(SequenceFlow flow)
    {
        throw new InvalidOperationException("Start event cannot have a source flow");
    }
}
