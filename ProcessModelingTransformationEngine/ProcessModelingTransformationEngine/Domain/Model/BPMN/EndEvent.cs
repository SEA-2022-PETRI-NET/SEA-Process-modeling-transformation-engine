namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class EndEvent : Event
{
    public EndEvent(int id) : base(id)
    { }

    public override void AddTargetFlow(SequenceFlow flow)
    {
        throw new InvalidOperationException("End event cannot have a target flow");
    }
}
