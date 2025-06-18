using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class OrdersServiceTests
{
    [Fact]
    public async Task CanCreateAndGetOrder()
    {
        var client = new HttpClient();
        var order = new { userId = "testuser", amount = 100, description = "test" };
        var response = await client.PostAsJsonAsync("http://localhost:8080/api/orders", order);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadAsAsync<dynamic>();
        var getResponse = await client.GetAsync($"http://localhost:8080/api/orders/{created.id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadAsAsync<dynamic>();
        Assert.Equal(order.userId, (string)fetched.userId);
        Assert.Equal(order.amount, (decimal)fetched.amount);
    }
} 