using Microsoft.AspNetCore.Mvc;
using SEA_Models.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.Application;
using SEA_Models.Domain.Model.PetriNet;

namespace ProcessModelingTransformationEngine.API;

[ApiController]
[Route("api/v1/[controller]")]
public class TransformationController : Controller
{
    private readonly ValidateBpmnService _validateBpmnService;
    private readonly PetriNetToDcrTransformerService _petriNetToDcrTransformerService;
    private readonly BpmnToPetriNetTransformerService _bpmnToPetriNetTransformerService;

    public TransformationController(ValidateBpmnService validateBpmnService, 
        PetriNetToDcrTransformerService petriNetToDcrTransformerService,
        BpmnToPetriNetTransformerService bpmnToPetriNetTransformerService)
    {
        _validateBpmnService = validateBpmnService;
        _petriNetToDcrTransformerService = petriNetToDcrTransformerService;
        _bpmnToPetriNetTransformerService = bpmnToPetriNetTransformerService;
    }
    
    [HttpPost("bpmn-to-petri-net", Name = "TransformBpmnToPetriNet")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PetriNet), StatusCodes.Status200OK)]
    public IActionResult TransformBpmnToPetriNet(BpmnDto bpmnDto)
    {
        _validateBpmnService.Validate(bpmnDto);
        var petriNet = _bpmnToPetriNetTransformerService.Transform(bpmnDto);
        return Ok(petriNet);
    }
    
    [HttpPost("petri-net-to-dcr", Name = "TransformPetriNetToDcr")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TransformPetriNetToDcr()
    {
        throw new NotImplementedException();
    }
}