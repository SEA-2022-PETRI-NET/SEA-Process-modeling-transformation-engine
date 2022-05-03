using Microsoft.AspNetCore.Mvc;
using ProcessModelingTransformationEngine.API.Model.BPMN;
using ProcessModelingTransformationEngine.Application;

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
    
    [HttpPost("bpmn/petri", Name = "TransformBpmnToPetriNet")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TransformBpmnToPetriNet(BpmnDto bpmnDto)
    {
        _validateBpmnService.Validate(bpmnDto);
        return Ok();
    }
    
    [HttpPost("petri/dcr", Name = "TransformPetriNetToDcr")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TransformPetriNetToDcr()
    {
        throw new NotImplementedException();
    }
}