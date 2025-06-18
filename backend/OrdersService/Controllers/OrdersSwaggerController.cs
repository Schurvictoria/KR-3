using Microsoft.AspNetCore.Mvc;

namespace OrdersService.Controllers
{
    [ApiExplorerSettings(IgnoreApi = false)]
    [ApiController]
    [Route("api/[controller]/swagger")]
    public class OrdersSwaggerController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("OrdersService Swagger is available at /swagger");
    }
} 