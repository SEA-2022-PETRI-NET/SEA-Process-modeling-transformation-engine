using System.Text.Json.Serialization;

namespace ProcessModelingTransformationEngine.Domain.Model.PetriNet;

public class Arc
{
    [JsonIgnore]
    public int Id { get; set; }
    
    public int SourceNode { get; set; }
    
    public int TargetNode { get; set; }
    
    [JsonIgnore]
    public int? PetriNetId { get; set; }
    [JsonIgnore]
    public PetriNet? PetriNet { get; set; }
}