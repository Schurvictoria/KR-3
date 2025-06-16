using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public GatewayController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] object request)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync("http://orders-service/api/orders", request);
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
