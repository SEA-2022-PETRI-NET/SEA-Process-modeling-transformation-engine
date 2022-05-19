using System.Diagnostics;

namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class Node : IBpmnElement
{
    public int Id { get; set; }
    public virtual bool IsMultiSource => false;
    public virtual bool IsMultiTarget => false;

    protected List<SequenceFlow> sourceFlows, targetFlows;

    public Node(int id)
    {
        this.Id = id;
        this.sourceFlows = new List<SequenceFlow>(1);
        this.targetFlows = new List<SequenceFlow>(1);
    }

    public SequenceFlow? GetFirstSourceFlow()
    {
        return sourceFlows.FirstOrDefault();
    }
    
    public SequenceFlow? GetFirstTargetFlow()
    {
        return targetFlows.FirstOrDefault();
    }
    
    public virtual void AddSourceFlow(SequenceFlow flow)
    {
        Debug.Assert(IsMultiSource || sourceFlows.Count == 0);
        Debug.Assert(flow.Target == this);
        Debug.Assert(sourceFlows.All(f => f.Source != flow.Source));
        sourceFlows.Add(flow);
    }

    public virtual void AddTargetFlow(SequenceFlow flow)
    {
        Debug.Assert(IsMultiTarget || targetFlows.Count == 0);
        Debug.Assert(flow.Source == this);
        Debug.Assert(targetFlows.All(f => f.Target != flow.Target));
        targetFlows.Add(flow);
    }

    public bool RemoveSourceFlow(SequenceFlow flow)
    {
        return sourceFlows.Remove(flow);
    }

    public bool RemoveTargetFlow(SequenceFlow flow)
    {
        return targetFlows.Remove(flow);
    }

    public IEnumerable<SequenceFlow> GetSourceFlows()
    {
        return sourceFlows.AsEnumerable();
    }

    public IEnumerable<SequenceFlow> GetTargetFlows()
    {
        return targetFlows.AsEnumerable();
    }
}
