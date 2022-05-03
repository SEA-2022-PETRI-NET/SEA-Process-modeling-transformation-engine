using ProcessModelingTransformationEngine.API.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.BPMN;

namespace ProcessModelingTransformationEngine.Application;

public class ValidateBpmnService
{
    public void Validate(BpmnDto bpmnDto)
    {
        List<IBpmnElementDto> nodes = 
            new IBpmnElementDto[] { bpmnDto.StartEvent, bpmnDto.EndEvent }
            .Concat(bpmnDto.IntermediateEvents)
            .Concat(bpmnDto.Tasks)
            .Concat(bpmnDto.AndGateways)
            .Concat(bpmnDto.XorGateways)
            .ToList();

        List<int> nodeIds = nodes.Select(n => n.Id).ToList();
        List<int> allIds = nodeIds.Concat(new[] { bpmnDto.Id })
            .Concat(bpmnDto.SequenceFlows.Select(f => f.Id))
            .ToList();
        if (allIds.Distinct().Count() != allIds.Count)
        {
            throw new BadHttpRequestException("ids within a single BPMN should all be unique");
        }

        List<string> nodeNames = new[] { bpmnDto.Name }
            .Concat(bpmnDto.Tasks.Select(n => n.Name))
            .ToList();
        if (nodeNames.Distinct().Count(n => !string.IsNullOrEmpty(n)) !=
            nodeNames.Count(n => !string.IsNullOrEmpty(n)))
        {
            throw new BadHttpRequestException("Names within a single BPMN should all be unique");
        }

        if (bpmnDto.SequenceFlows.Exists(f => f.TargetId == bpmnDto.StartEvent.Id))
        {
            throw new BadHttpRequestException("Start event cannot have a source flow");
        }
        
        if (bpmnDto.SequenceFlows.Exists(f => f.SourceId == bpmnDto.EndEvent.Id))
        {
            throw new BadHttpRequestException("End event cannot have a target flow");
        }

        foreach (var flow in bpmnDto.SequenceFlows)
        {
            if (!nodeIds.Contains(flow.SourceId))
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " + 
                    $"has an invalid source node id '{flow.SourceId}'");
            }

            if (!nodeIds.Contains(flow.TargetId))
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " + 
                    $"has an invalid target node id '{flow.TargetId}'");
            }

            if (flow.SourceId == flow.TargetId)
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " +
                    "has identical source and target ids");
            }

            if (flow.TargetId == bpmnDto.StartEvent.Id)
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " +
                    "is a source flow of the start event");
            }
            
            if (flow.SourceId == bpmnDto.EndEvent.Id)
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " +
                    "is a target flow of the end event");
            }
        }
        
        // All node ids except start id
        List<int> nodeIdsExceptStart = nodeIds
            .Except(new[] { bpmnDto.StartEvent.Id })
            .ToList();
        List<int> checkTargetIds = bpmnDto.SequenceFlows.Select(f => f.TargetId)
            .Where(id => nodeIdsExceptStart.Contains(id))
            .ToList();
        if (checkTargetIds.Distinct().Count() != nodeIdsExceptStart.Count)
        {
            throw new BadHttpRequestException(
                "All nodes except the start node must have at least one source flow");
        }

        List<int> gatewayIds = bpmnDto.AndGateways.Select(n => n.Id)
            .Concat(bpmnDto.XorGateways.Select(n => n.Id))
            .ToList();
        List<int> allSingleTargetSourceIds = bpmnDto.SequenceFlows
            .Select(f => f.SourceId).Except(gatewayIds)
            .ToList();
        if (allSingleTargetSourceIds.Distinct().Count() != 
            nodeIds.Except(gatewayIds).Except(new[] { bpmnDto.EndEvent.Id }).Count())
        {
            throw new BadHttpRequestException(
                "Only gateways can have more than one target flow");
        }
        
        // All node ids except end id
        List<int> nodeIdsExceptEnd = nodeIds.Except(new[] { bpmnDto.EndEvent.Id }).ToList();
        List<int> checkSourceIds = bpmnDto.SequenceFlows.Select(f => f.SourceId)
            .Where(id => nodeIdsExceptEnd.Contains(id))
            .ToList();
        if (checkSourceIds.Distinct().Count() != nodeIdsExceptEnd.Count)
        {
            throw new BadHttpRequestException(
                "All nodes except the end node must have at least one target flow");
        }

        /*Dictionary<int, IBpmnElementDto> idToNode = nodes.ToDictionary(n => n.Id, n => n);
        var visited = new HashSet<IBpmnElementDto>();
        var stack = new Stack<IBpmnElementDto>();
        visited.Add(bpmnDto.StartEvent);
        stack.Push(bpmnDto.StartEvent);
        while (stack.Count > 0)
        {
            var curNode = stack.Pop();
            IEnumerable<SequenceFlowDto> targetFlows = 
                bpmnDto.SequenceFlows.Where(f => f.SourceId == curNode.Id);
            foreach (var targetFlow in targetFlows)
            {
                var target = idToNode[targetFlow.TargetId];
                bool isLoop = !visited.Add(target);
                if (isLoop && curNode is not XorGateway)
                {
                    throw new BadHttpRequestException("Only XOR gateways can loop back to previous part in the graph");
                }

                if (!isLoop)
                {
                    stack.Push(target);
                }
            }
        }*/
    }
}