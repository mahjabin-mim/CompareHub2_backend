using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;
using CompareHub.Backend.app.Core.Shared.Common;

namespace CompareHub.Backend.app.Host.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private const string KoreanMartBaseUrl = "https://koreanmartbd.com/";
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProductDiscoveryService _productDiscoveryService;
    private readonly IProductScraperService _productScraperService;
    private readonly IPublicProductSearchService _publicProductSearchService;

    public ProductsController(
        IProductDiscoveryService productDiscoveryService,
        IProductScraperService productScraperService,
        IPublicProductSearchService publicProductSearchService)
    {
        _productDiscoveryService = productDiscoveryService;
        _productScraperService = productScraperService;
        _publicProductSearchService = publicProductSearchService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        var result = await _productDiscoveryService.SearchProductsAsync(query, cancellationToken);
        return Ok(ApiResponse<ProductSearchResponseDto>.Ok(result));
    }

    [AllowAnonymous]
    [HttpGet("search/koreanmart")]
    public async Task<IActionResult> SearchKoreanMart([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<string>.Fail("Search query is required."));
        }

        var results = await _productScraperService.SearchProductsAsync(KoreanMartBaseUrl, query.Trim(), cancellationToken);
        return Ok(ApiResponse<ProductSearchResponseDto>.Ok(new ProductSearchResponseDto(query.Trim(), results)));
    }

    [AllowAnonymous]
    [EnableRateLimiting("search")]
    [HttpGet("search/public")]
    public async Task<IActionResult> SearchPublicSources([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<string>.Fail("Search query is required."));
        }

        var result = await _publicProductSearchService.SearchAsync(query, cancellationToken);
        return Ok(ApiResponse<ProductSearchResponseDto>.Ok(result));
    }

    // Server-sent events: "start" lists the stores, then one "source" event per store as it finishes, then "done".
    [AllowAnonymous]
    [EnableRateLimiting("search")]
    [HttpGet("search/public/stream")]
    public async Task StreamPublicSources([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(ApiResponse<string>.Fail("Search query is required."), cancellationToken);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        // Stops reverse proxies from holding events back until the response ends.
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await WriteEventAsync("start", new { Query = query.Trim(), Sources = _publicProductSearchService.SourceWebsites }, cancellationToken);
            await foreach (var sourceResult in _publicProductSearchService.SearchBySourceAsync(query, cancellationToken))
            {
                await WriteEventAsync("source", sourceResult, cancellationToken);
            }

            await WriteEventAsync("done", new { }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The visitor left or started a new search; the response has already started, so just stop.
        }
    }

    private async Task WriteEventAsync<T>(string eventName, T data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, EventJsonOptions);
        await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
