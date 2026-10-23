using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;
using CompareHub.Backend.app.Core.Modules.ProductDiscovery.Services.Scrapers;

namespace CompareHub.Backend.app.Core.Modules.ProductDiscovery.Services;

public class WebsiteProductScraperService : IProductScraperService
{
    private static readonly TimeSpan StoreDeadline = TimeSpan.FromSeconds(10);

    private readonly IEnumerable<IProductSourceScraper> _sourceScrapers;
    private readonly ILogger<WebsiteProductScraperService> _logger;

    public WebsiteProductScraperService(
        IEnumerable<IProductSourceScraper> sourceScrapers,
        ILogger<WebsiteProductScraperService> logger)
    {
        _sourceScrapers = sourceScrapers;
        _logger = logger;
    }

    public async Task<List<ProductResultDto>> SearchProductsAsync(string sourceUrl, string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var scraper = _sourceScrapers.FirstOrDefault(x => x.CanHandle(sourceUrl));
        if (scraper is null)
        {
            _logger.LogWarning("No fixed product scraper is registered for {SourceUrl}.", sourceUrl);
            return [];
        }

        // Caps a whole store, including chained requests, so one slow site can't hold up the search.
        using var storeDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        storeDeadline.CancelAfter(StoreDeadline);

        try
        {
            var results = await scraper.SearchAsync(query.Trim(), storeDeadline.Token);
            return ProductScraperHelpers.NormalizeResults(results, query, 30);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(ex, "Product search timed out for {SourceName}.", scraper.SourceName);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Product search failed for {SourceName}.", scraper.SourceName);
            return [];
        }
    }
}
