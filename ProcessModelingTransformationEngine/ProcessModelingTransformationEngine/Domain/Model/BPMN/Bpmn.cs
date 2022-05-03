using System.Collections;

namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class Bpmn : IEnumerable<Node>
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public StartEvent StartEvent { get; set; }
    public EndEvent EndEvent { get; set; }

    public Bpmn(int id, string name, StartEvent startEvent, EndEvent endEvent)
    {
        this.Id = id;
        this.Name = name;
        this.StartEvent = startEvent;
        this.EndEvent = endEvent;
    }

    public void AddFlow(SequenceFlow flow)
    {
        flow.Source.AddTargetFlow(flow);
        flow.Target.AddSourceFlow(flow);
    }

    public IEnumerator<Node> GetEnumerator()
    {
        var visited = new HashSet<Node>();
        var stack = new Stack<Node>();
        visited.Add(StartEvent);
        stack.Push(StartEvent);
        while (stack.Count > 0)
        {
            var curNode = stack.Pop();
            yield return curNode;
            foreach (var targetFlow in curNode.GetTargetFlows())
            {
                if (visited.Add(targetFlow.Target))
                {
                    stack.Push(targetFlow.Target);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}