using Microsoft.AspNetCore.Mvc;

namespace PaymentsService.Controllers
{
    [ApiExplorerSettings(IgnoreApi = false)]
    [ApiController]
    [Route("api/[controller]/swagger")]
    public class PaymentsSwaggerController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("PaymentsService Swagger is available at /swagger");
    }
} 