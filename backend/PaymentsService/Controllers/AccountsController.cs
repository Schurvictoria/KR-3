using Microsoft.AspNetCore.Mvc;
using PaymentsService.Models;
using PaymentsService.Data;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentsDbContext _db;

        public AccountsController(PaymentsDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            if (_db.Accounts.Any(a => a.UserId == request.UserId))
                return BadRequest("Account already exists");

            var account = new Account
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Balance = 0
            };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpPost("{userId}/topup")]
        public async Task<IActionResult> TopUp(string userId, [FromBody] TopUpRequest request)
        {
            var account = _db.Accounts.FirstOrDefault(a => a.UserId == userId);
            if (account == null)
                return NotFound();

            account.Balance += request.Amount;
            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpGet("{userId}/balance")]
        public IActionResult GetBalance(string userId)
        {
            var account = _db.Accounts.FirstOrDefault(a => a.UserId == userId);
            if (account == null)
                return NotFound();

            return Ok(account.Balance);
        }
    }
}
