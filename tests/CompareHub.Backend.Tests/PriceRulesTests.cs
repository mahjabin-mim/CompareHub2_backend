using CompareHub.Backend.app.Core.Modules.Alerts.Services;

namespace CompareHub.Backend.Tests;

public class PriceRulesTests
{
    [Theory]
    [InlineData(1000, 900, true)]    // 10% off
    [InlineData(1000, 970, true)]    // exactly 3%
    [InlineData(1000, 980, false)]   // 2% is just noise
    [InlineData(200, 195, false)]    // under ৳10
    [InlineData(100, 90, true)]      // exactly ৳10 and 10%
    [InlineData(1000, 1100, false)]  // increases never alert
    [InlineData(1000, 1000, false)]
    public void IsMeaningfulDrop_NeedsAtLeastTenTakaAndThreePercent(decimal oldPrice, decimal newPrice, bool expected)
    {
        Assert.Equal(expected, PriceRules.IsMeaningfulDrop(oldPrice, newPrice));
    }

    [Theory]
    [InlineData(1000, 800, true)]
    [InlineData(1000, 1500, true)]
    [InlineData(1000, 150, false)]   // 85% off is far more likely a sachet or a parse error
    [InlineData(1000, 6000, false)]
    [InlineData(1000, 0, false)]
    public void IsPlausibleChange_RejectsScrapingGlitches(decimal oldPrice, decimal newPrice, bool expected)
    {
        Assert.Equal(expected, PriceRules.IsPlausibleChange(oldPrice, newPrice));
    }

    [Theory]
    [InlineData("https://www.kireibd.com/product/serum/", "https://kireibd.com/product/serum")]
    [InlineData("https://kireibd.com/product/serum?utm_source=x", "http://KIREIBD.com/Product/Serum")]
    [InlineData("https://shop.example.com/", "https://shop.example.com")]
    public void SameListing_IgnoresSchemeWwwCaseTrailingSlashAndTrackingQuery(string a, string b)
    {
        Assert.True(PriceRules.SameListing(a, b));
    }

    [Fact]
    public void SameListing_KeepsTheQueryWhenThePathIsJustTheRoot()
    {
        Assert.False(PriceRules.SameListing("https://shop.example.com/?p=1", "https://shop.example.com/?p=2"));
        Assert.False(PriceRules.SameListing("https://kireibd.com/product/a", "https://kireibd.com/product/b"));
    }
}
