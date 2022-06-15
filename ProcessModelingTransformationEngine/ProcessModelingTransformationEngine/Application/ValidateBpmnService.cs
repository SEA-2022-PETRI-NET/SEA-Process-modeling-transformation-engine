using SEA_Models.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.Domain.Utils;

namespace ProcessModelingTransformationEngine.Application;

public class ValidateBpmnService
{
    public void Validate(BpmnDto bpmnDto)
    {
        if (bpmnDto.EndEvents.Count == 0)
        {
            throw new BadHttpRequestException("BPMN must have at least one end event");
        }
        
        List<IBpmnElementDto> nodes = new IBpmnElementDto[] { bpmnDto.StartEvent }
            .Concat(bpmnDto.EndEvents)    
            .Concat(bpmnDto.IntermediateEvents)
            .Concat(bpmnDto.Tasks)
            .Concat(bpmnDto.ParallelGateways)
            .Concat(bpmnDto.ExclusiveGateways)
            .ToList();

        List<int> nodeIds = nodes.Select(n => n.Id).ToList();
        List<int> allIds = nodeIds.Concat(new[] { bpmnDto.Id })
            .Concat(bpmnDto.SequenceFlows.Select(f => f.Id))
            .ToList();
        if (allIds.Distinct().Count() != allIds.Count)
        {
            throw new BadHttpRequestException("ids within a single BPMN should all be unique");
        }

        List<int> endIds = bpmnDto.EndEvents.Select(e => e.Id).ToList();

        List<string> nodeNames = new[] { bpmnDto.Name }
            .Concat(bpmnDto.Tasks.Select(n => n.Name))
            .ToList();
        if (nodeNames.Distinct().Count(n => !string.IsNullOrEmpty(n)) !=
            nodeNames.Count(n => !string.IsNullOrEmpty(n)))
        {
            throw new BadHttpRequestException("Names within a single BPMN should all be unique");
        }

        List<SequenceFlowDto> uniqueFlows = bpmnDto.SequenceFlows
                .DistinctBy(f => new { f.SourceId, f.TargetId })
                .ToList();
        if (uniqueFlows.Count != bpmnDto.SequenceFlows.Count)
        {
            throw new BadHttpRequestException(
                "BPMN cannot contain two sequence flows with same source and target");
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
            
            if (endIds.Contains(flow.SourceId))
            {
                throw new BadHttpRequestException(
                    $"SequenceFlow with id '{flow.Id}' " +
                    "is a target flow of an end event");
            }
        }
        
        // All node ids except start id
        List<int> nodeIdsExceptStart = nodeIds
            .Where(id => id != bpmnDto.StartEvent.Id)
            .ToList();
        List<int> checkTargetIds = bpmnDto.SequenceFlows.Select(f => f.TargetId)
            .Where(id => id != bpmnDto.StartEvent.Id)
            .ToList();
        if (checkTargetIds.Distinct().Count() != nodeIdsExceptStart.Count)
        {
            throw new BadHttpRequestException(
                "All nodes except the start node must have at least one source flow");
        }
        
        List<int> checkSourceIds = bpmnDto.SequenceFlows.Select(f => f.SourceId)
            .ExceptAll(endIds).ToList();
        if (checkSourceIds.Distinct().Count() != nodeIds.Except(endIds).Count())
        {
            throw new BadHttpRequestException(
                "All nodes except the end node must have at least one target flow");
        }

        List<int> gatewayIds = bpmnDto.ParallelGateways.Select(n => n.Id)
            .Concat(bpmnDto.ExclusiveGateways.Select(n => n.Id))
            .ToList();
        List<int> singleSourceIds = nodeIdsExceptStart.ExceptAll(gatewayIds)
            .ToList();
        List<int> allSingleSourceTargetIds = bpmnDto.SequenceFlows
            .Select(f => f.TargetId).ExceptAll(gatewayIds)
            .ToList();
        if (allSingleSourceTargetIds.Count != 
            singleSourceIds.Count)
        {
            throw new BadHttpRequestException(
                "Only gateways can have more than one source flow");
        }

        List<int> singleTargetIds = nodeIds.ExceptAll(gatewayIds)
            .ExceptAll(endIds).ToList();
        List<int> allSingleTargetSourceIds = bpmnDto.SequenceFlows
            .Select(f => f.SourceId).ExceptAll(gatewayIds)
            .ToList();
        if (allSingleTargetSourceIds.Count != 
            singleTargetIds.Count)
        {
            throw new BadHttpRequestException(
                "Only gateways can have more than one target flow");
        }
    }
}