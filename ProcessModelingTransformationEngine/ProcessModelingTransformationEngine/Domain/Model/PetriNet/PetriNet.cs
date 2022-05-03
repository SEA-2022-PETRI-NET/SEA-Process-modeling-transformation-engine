namespace ProcessModelingTransformationEngine.Domain.Model.PetriNet;

public class PetriNet
{
    public int Id { get; set; }

    public string Name { get; set; }
    
    public List<Arc> Arcs { get; set; }
    public List<Place> Places { get; set; }
    public List<Transition> Transitions { get; set; }
}