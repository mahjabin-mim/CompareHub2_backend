using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Services.Scrapers;

namespace CompareHub.Backend.app.Core.Modules.ProductDiscovery.Services;

public class PublicProductSearchService : IPublicProductSearchService
{
    private const int MaxResultsPerSource = 30;
    private const int MaxCombinedResults = 60;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private static readonly string[] PublicSourceUrls =
    [
        "https://kireibd.com/",
        "https://koreanmartbd.com/",
        "https://shop.shajgoj.com/",
        "https://groomlybd.com/",
        "https://skincareshop.com.bd/",
        "https://beautybooth.com.bd/",
        "https://www.emartwayskincare.com.bd/",
        "https://tekka.com.bd/",
        "https://dhalishop.com/",
        "https://thealamsbd.com/",
        "https://www.thelilium.shop/",
        "https://klassy.com.bd/",
        "https://pixielabellabangladesh.com/",
        "https://www.skinnora.com/",
        "https://makeupchari.com/",
        "https://perfectobd.com/",
        "https://www.arogga.com/"
    ];

    private readonly IProductScraperService _productScraperService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublicProductSearchService> _logger;

    public PublicProductSearchService(
        IProductScraperService productScraperService,
        IMemoryCache cache,
        ILogger<PublicProductSearchService> logger)
    {
        _productScraperService = productScraperService;
        _cache = cache;
        _logger = logger;
    }

    public IReadOnlyList<string> SourceWebsites { get; } = PublicSourceUrls.Select(GetSourceWebsiteName).ToList();

    public async IAsyncEnumerable<PublicSourceResultDto> SearchBySourceAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();
        var cacheKey = $"public-search:{trimmedQuery.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out List<PublicSourceResultDto>? cached) && cached is not null)
        {
            foreach (var sourceResult in cached)
            {
                yield return sourceResult;
            }

            yield break;
        }

        var searchTasks = PublicSourceUrls
            .Select(sourceUrl => SearchSourceAsync(sourceUrl, trimmedQuery, cancellationToken))
            .ToList();
        var completed = new List<PublicSourceResultDto>(searchTasks.Count);

        // Hand back each store as soon as it finishes instead of waiting for the slowest one.
        await foreach (var searchTask in Task.WhenEach(searchTasks).WithCancellation(cancellationToken))
        {
            var sourceResult = await searchTask;
            completed.Add(sourceResult);
            yield return sourceResult;
        }

        // A cancelled search ends with empty stores, so only cache searches that ran to completion.
        if (!cancellationToken.IsCancellationRequested)
        {
            _cache.Set(cacheKey, completed, CacheDuration);
        }
    }

    public async Task<ProductSearchResponseDto> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();
        var sourceResults = new List<PublicSourceResultDto>();
        await foreach (var sourceResult in SearchBySourceAsync(trimmedQuery, cancellationToken))
        {
            sourceResults.Add(sourceResult);
        }

        var results = ProductScraperHelpers.NormalizeResults(
            sourceResults.SelectMany(x => x.Results),
            trimmedQuery,
            MaxCombinedResults);
        var sourceChecks = sourceResults
            .Select(x => new ProductSourceCheckDto(x.SourceWebsite, true, x.Results.Count > 0, x.Results.Count))
            .ToList();

        return new ProductSearchResponseDto(trimmedQuery, results, sourceChecks);
    }

    private async Task<PublicSourceResultDto> SearchSourceAsync(string sourceUrl, string query, CancellationToken cancellationToken)
    {
        var sourceWebsite = GetSourceWebsiteName(sourceUrl);
        try
        {
            var results = await _productScraperService.SearchProductsAsync(sourceUrl, query, cancellationToken);
            // Stores are shown side by side, so drop a store's fallback results that don't match the query.
            return new PublicSourceResultDto(
                sourceWebsite,
                ProductScraperHelpers.NormalizeResults(results, query, MaxResultsPerSource, relevantOnly: true));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Public source search failed for {SourceUrl}.", sourceUrl);
            return new PublicSourceResultDto(sourceWebsite, []);
        }
    }

    private static string GetSourceWebsiteName(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return sourceUrl;
        }

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
    }
}
