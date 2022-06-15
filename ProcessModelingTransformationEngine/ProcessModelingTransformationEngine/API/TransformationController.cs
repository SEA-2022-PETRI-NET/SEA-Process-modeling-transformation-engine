using Microsoft.AspNetCore.Mvc;
using ProcessModelingTransformationEngine.API.Model.BPMN;
using SEA_Models.Domain.Model.BPMN;
using ProcessModelingTransformationEngine.Application;
using SEA_Models.PetriNet;

namespace ProcessModelingTransformationEngine.API;

[ApiController]
[Route("api/v1/[controller]")]
public class TransformationController : Controller
{
    private readonly ValidateBpmnService _validateBpmnService;
    private readonly DcrToPetriNetTransformerService _dcrToPetriNetTransformerService;
    private readonly BpmnToPetriNetTransformerService _bpmnToPetriNetTransformerService;

    public TransformationController(ValidateBpmnService validateBpmnService, 
        DcrToPetriNetTransformerService dcrToPetriNetTransformerService,
        BpmnToPetriNetTransformerService bpmnToPetriNetTransformerService)
    {
        _validateBpmnService = validateBpmnService;
        _dcrToPetriNetTransformerService = dcrToPetriNetTransformerService;
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
    
    [HttpPost("dcr-to-petri-net", Name = "TransformDcrToPetriNet")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TransformDcrToPetriNet()
    {
        throw new NotImplementedException();
    }
}