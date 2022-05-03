using System.Text.Json.Serialization;

namespace ProcessModelingTransformationEngine.Domain.Model.PetriNet;

public class Transition
{
    public int Id { get; set; }
    
    public int TransitionId { get; set; }
    
    public string? Name { get; set; }
    
    [JsonIgnore]
    public int? PetriNetId { get; set; }
    [JsonIgnore]
    public PetriNet? PetriNet { get; set; }
}