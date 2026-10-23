namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public static class PriceRules
{
    private const decimal MinimumDrop = 10m;
    private const decimal MinimumDropFraction = 0.03m;
    // A rechecked price outside this band is far more likely a scraping glitch (a sachet, a bundle,
    // a missing digit) than a real change, so it is ignored rather than recorded or alerted on.
    private const decimal LowestPlausibleFraction = 0.2m;
    private const decimal HighestPlausibleMultiple = 5m;

    public static bool IsPlausibleChange(decimal oldPrice, decimal newPrice) =>
        newPrice > 0 && oldPrice > 0 && newPrice >= oldPrice * LowestPlausibleFraction && newPrice <= oldPrice * HighestPlausibleMultiple;

    // Ignores pennies and tiny wobbles: at least ৳10 and at least 3%.
    public static bool IsMeaningfulDrop(decimal oldPrice, decimal newPrice) =>
        newPrice < oldPrice && oldPrice - newPrice >= Math.Max(MinimumDrop, oldPrice * MinimumDropFraction);

    // Two links point at the same listing when host and path match; the query only matters for
    // sites that identify products by it (?p=123), i.e. when the path is just "/".
    public static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            return (url ?? string.Empty).Trim().ToLowerInvariant();
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath).TrimEnd('/').ToLowerInvariant();
        return path.Length == 0 ? $"{host}/{uri.Query.ToLowerInvariant()}" : $"{host}{path}";
    }

    public static bool SameListing(string a, string b) => NormalizeUrl(a) == NormalizeUrl(b);
}
