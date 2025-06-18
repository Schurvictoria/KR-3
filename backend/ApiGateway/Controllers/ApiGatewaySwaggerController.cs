using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiExplorerSettings(IgnoreApi = false)]
    [ApiController]
    [Route("api/[controller]/swagger")]
    public class ApiGatewaySwaggerController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("API Gateway Swagger is available at /swagger");
    }
} 