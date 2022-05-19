using System.Collections;

namespace ProcessModelingTransformationEngine.Domain.Model.BPMN;

public class Bpmn : IEnumerable<Node>
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public StartEvent StartEvent { get; set; }

    public Bpmn(int id, string name, StartEvent startEvent)
    {
        this.Id = id;
        this.Name = name;
        this.StartEvent = startEvent;
    }

    public void AddFlow(SequenceFlow flow)
    {
        flow.Source.AddTargetFlow(flow);
        flow.Target.AddSourceFlow(flow);
    }

    public IEnumerator<Node> GetEnumerator()
    {
        var visited = new HashSet<Node>();
        var frontier = new Stack<Node>();
        visited.Add(StartEvent);
        frontier.Push(StartEvent);
        while (frontier.Count > 0)
        {
            var curNode = frontier.Pop();
            yield return curNode;
            foreach (var targetFlow in curNode.GetTargetFlows())
            {
                if (visited.Add(targetFlow.Target))
                {
                    frontier.Push(targetFlow.Target);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}