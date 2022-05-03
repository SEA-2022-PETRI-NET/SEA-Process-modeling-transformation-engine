using System.Diagnostics;

namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class SequenceFlow : IBpmnElement
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public Node Source { get; set; }
    public Node Target { get; set; }

    public SequenceFlow(int id, string name, Node source, Node target)
    {
        Debug.Assert(source != target);
        this.Id = id;
        this.Name = name;
        this.Source = source;
        this.Target = target;
    }
}