using CompareHub.Backend.app.Core.Modules.ProductDiscovery.DTOs;

namespace CompareHub.Backend.app.Core.Modules.ProductDiscovery.Interfaces;

public interface IPublicProductSearchService
{
    IReadOnlyList<string> SourceWebsites { get; }
    IAsyncEnumerable<PublicSourceResultDto> SearchBySourceAsync(string query, CancellationToken cancellationToken);
    Task<ProductSearchResponseDto> SearchAsync(string query, CancellationToken cancellationToken);
}
