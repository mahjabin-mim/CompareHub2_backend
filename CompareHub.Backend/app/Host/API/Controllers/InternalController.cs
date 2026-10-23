using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Shared.Common;

namespace CompareHub.Backend.app.Host.API.Controllers;

// Called by a scheduler (GitHub Actions) because the free host sleeps and can't run its own timer.
[ApiController]
[Route("api/v1/internal")]
public class InternalController : ControllerBase
{
    private readonly IPriceCheckService _priceCheckService;
    private readonly IConfiguration _configuration;

    public InternalController(IPriceCheckService priceCheckService, IConfiguration configuration)
    {
        _priceCheckService = priceCheckService;
        _configuration = configuration;
    }

    [HttpPost("price-check")]
    public async Task<IActionResult> PriceCheck(CancellationToken cancellationToken)
    {
        var expected = _configuration["Cron:Key"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            return NotFound();
        }

        var provided = Request.Headers["X-Cron-Key"].ToString();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid key."));
        }

        var summary = await _priceCheckService.RunAsync(cancellationToken);
        return Ok(ApiResponse<PriceCheckSummary>.Ok(summary));
    }
}
