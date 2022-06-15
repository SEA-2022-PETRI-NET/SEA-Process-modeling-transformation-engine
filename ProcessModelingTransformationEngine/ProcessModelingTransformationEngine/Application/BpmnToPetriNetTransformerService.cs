using System.Diagnostics;
using SEA_Models.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.API.Model.Converters;
using SEA_Models.BPMN;
using SEA_Models.PetriNet;

using BpmnTask = SEA_Models.BPMN.Task;

namespace ProcessModelingTransformationEngine.Application;

public class BpmnToPetriNetTransformerService
{
    private int idCounter;
    private BpmnDtoToModel _bpmnDtoToModel = new BpmnDtoToModel();

    private int GenId()
    {
        return idCounter++;
    }
    
    public PetriNet Transform(BpmnDto bpmnDto)
    {
        return Transform(_bpmnDtoToModel.DtoToModel(bpmnDto));
    }

    private Arc Connect(PetriNet petriNet, int sourceId, int targetId)
    {
        var arc = new Arc() { Id = GenId(), 
            SourceNode = sourceId, TargetNode = targetId };
        petriNet.Arcs.Add(arc);
        return arc;
    }

    private Place AddNewPlace(PetriNet petriNet, int id)
    {
        var place = new Place() { Id = id, Name = "p" + id, NumberOfTokens = 0 };
        petriNet.Places.Add(place);
        return place;
    }

    private Transition AddNewTransition(PetriNet petriNet, int id)
    {
        var transition = new Transition() { Id = id, Name = "t" + id };
        petriNet.Transitions.Add(transition);
        return transition;
    }
    
    public PetriNet Transform(Bpmn bpmn)
    {
        idCounter = 0;
        var petriNet = new PetriNet() 
        { 
            Id = GenId(), 
            Name = bpmn.Name + "_From-BPMN-To-Petri-Net",
            Places = new List<Place>(),
            Arcs = new List<Arc>(),
            Transitions = new List<Transition>()
        };
        // Explore BPMN via DFS
        // Each "choice"/"action" in the BPMN is going to be a transition in the petri net
        // with one input and output place that can contain 0 or 1 tokens to denote
        // what choices there are currently (which transitions can fire)
        // In the initial configuration (before a simulation)
        // only the start transition can be fired (1 token in its input place)
        
        // Maps old id of an element to the id of its new place/transition
        var oldIdToNewId = new Dictionary<int, int>(); 
        var frontier = new Stack<Node>();
        oldIdToNewId.Add(bpmn.StartEvent.Id, GenId());
        frontier.Push(bpmn.StartEvent);
        while (frontier.Count > 0)
        {
            Node curNode = frontier.Pop();
            int curId = oldIdToNewId[curNode.Id];
            Place place = null;
            if (curNode is not ParallelGateway)
            {
                place = AddNewPlace(petriNet, curId);
            }

            bool isFork = curNode is ExclusiveGateway;
            Transition transition = null;
            if (isFork)
            {
                place.Name = "exclusive" + place.Id;
            }
            else
            {
                // After non-fork we can continue to all outgoing nodes
                transition = AddNewTransition(petriNet, 
                    place == null ? curId : GenId());
                if (place != null)
                {
                    Connect(petriNet, curId, transition.Id);   
                }

                if (curNode is ParallelGateway)
                {
                    transition.Name = "parallel" + transition.Id;
                }
            }
            
            if (curNode is StartEvent)
            {
                place.Name = "start";
                place.NumberOfTokens = 1;
            }
            else if (curNode is EndEvent)
            {
                transition.Name = "end";
                // End event has no outgoing flows
                //var place2 = AddNewPlace(petriNet, GenId());
                //Connect(petriNet, transition.Id, place2.Id);
            }
            else if (curNode is BpmnTask task)
            {
                transition.Name = task.Name;
            }
            
            foreach (var targetFlow in curNode.GetTargetFlows())
            {
                int newTargetId;
                // Else means target has already been visited (more than 1 input)
                // so reference the existing id instead of creating new one
                if (!oldIdToNewId.TryGetValue(targetFlow.Target.Id, out newTargetId))
                {
                    newTargetId = GenId();
                    oldIdToNewId.Add(targetFlow.Target.Id, newTargetId);
                    frontier.Push(targetFlow.Target);
                } 
                // Fork (exclusive) adds a transition for each possible choice
                // can only fire one of them
                if (isFork)
                {
                    transition = AddNewTransition(petriNet, GenId());
                    Connect(petriNet, curId, transition.Id);
                }
                if (targetFlow.Target is ParallelGateway)
                {
                    var outPlace = AddNewPlace(petriNet, GenId());
                    Connect(petriNet, transition.Id, outPlace.Id);
                    Connect(petriNet, outPlace.Id, newTargetId);
                } 
                else
                {
                    Connect(petriNet, transition.Id, newTargetId);
                }
            }
        }
        return petriNet;
    }
}