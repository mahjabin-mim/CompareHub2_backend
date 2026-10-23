using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;
using CompareHub.Backend.app.Core.Modules.Alerts.Services;

namespace CompareHub.Backend.Tests;

public class AlertMessagesTests
{
    private static PriceDropNotice Drop(string name = "Nineless B-Boost Serum 30ml", decimal oldPrice = 1450, decimal newPrice = 1190) =>
        new(name, "skinnora.com", oldPrice, newPrice, "https://skinnora.com/p/serum");

    [Fact]
    public void SingleDrop_NamesTheProductStoreAndPrices()
    {
        var drops = new[] { Drop() };

        Assert.Equal("Price drop: Nineless B-Boost Serum 30ml is now ৳1,190 at Skinnora", AlertMessages.EmailSubject(drops));
        Assert.Equal("Nineless B-Boost Serum 30ml: ৳1,450 → ৳1,190 at Skinnora", AlertMessages.PushBody(drops));
    }

    [Fact]
    public void SeveralDrops_AreSummarisedInOneMessage()
    {
        var drops = new[] { Drop(), Drop("Anua Toner 250ml", 1300, 1100), Drop("Cosrx Essence", 700, 600) };

        Assert.Equal("3 price drops on your wishlist", AlertMessages.Title(drops));
        Assert.EndsWith("and 2 more", AlertMessages.PushBody(drops));
    }

    [Fact]
    public void EmailHtml_EscapesProductNamesAndShowsTheSaving()
    {
        var html = AlertMessages.EmailHtml("Mim <b>", new[] { Drop("<script>alert(1)</script> Serum") }, "https://comparehub2.vercel.app/wishlist");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("Mim &lt;b&gt;", html);
        Assert.Contains("save ৳260", html);
        Assert.Contains("https://comparehub2.vercel.app/wishlist", html);
    }
}
