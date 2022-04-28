using Microsoft.AspNetCore.Mvc;
using ProcessModelingTransformationEngine.Application;

namespace ProcessModelingTransformationEngine.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransformationController : Controller
{
    
    private readonly PetriNetToDCRTransformerService _petriNetToDcrTransformerService;

    public TransformationController(PetriNetToDCRTransformerService petriNetToDcrTransformerService)
    {
        _petriNetToDcrTransformerService = petriNetToDcrTransformerService;
    }

    
    [HttpPost(Name = "TransformPetriNetToDCR")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public void TransformPetriNetToDCR()
    {
    }
}