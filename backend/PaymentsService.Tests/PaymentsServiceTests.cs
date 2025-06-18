using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class PaymentsServiceTests
{
    [Fact]
    public async Task CanCreateTopUpAndGetBalance()
    {
        var client = new HttpClient();
        var userId = "testuser";
        var createResp = await client.PostAsJsonAsync("http://localhost:5001/api/accounts", new { userId });
        createResp.EnsureSuccessStatusCode();
        var topupResp = await client.PostAsJsonAsync($"http://localhost:5001/api/accounts/{userId}/topup", new { amount = 100 });
        topupResp.EnsureSuccessStatusCode();
        var balanceResp = await client.GetAsync($"http://localhost:5001/api/accounts/{userId}/balance");
        balanceResp.EnsureSuccessStatusCode();
        var balance = await balanceResp.Content.ReadAsAsync<decimal>();
        Assert.Equal(100, balance);
    }
} 