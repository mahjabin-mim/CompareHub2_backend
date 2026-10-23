using System.Net;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public static class AlertMessages
{
    public static string Price(decimal price) => $"৳{price:N0}";

    public static string Title(IReadOnlyList<PriceDropNotice> drops) =>
        drops.Count == 1 ? "Price drop on your wishlist" : $"{drops.Count} price drops on your wishlist";

    public static string Summary(PriceDropNotice drop) =>
        $"{drop.ItemName}: {Price(drop.OldPrice)} → {Price(drop.NewPrice)} at {StoreNames.Display(drop.StoreKey)}";

    public static string PushBody(IReadOnlyList<PriceDropNotice> drops) =>
        drops.Count == 1 ? Summary(drops[0]) : $"{Summary(drops[0])} and {drops.Count - 1} more";

    public static string EmailSubject(IReadOnlyList<PriceDropNotice> drops) =>
        drops.Count == 1
            ? $"Price drop: {drops[0].ItemName} is now {Price(drops[0].NewPrice)} at {StoreNames.Display(drops[0].StoreKey)}"
            : $"{drops.Count} price drops on your CompareHub wishlist";

    public static string EmailHtml(string userName, IReadOnlyList<PriceDropNotice> drops, string wishlistUrl)
    {
        string Html(string text) => WebUtility.HtmlEncode(text);

        var rows = string.Concat(drops.Select(drop => $"""
            <tr><td style="padding:12px 0;border-bottom:1px solid #F0D8C4">
              <div style="font-weight:700;color:#3A2417">{Html(drop.ItemName)}</div>
              <div style="margin-top:4px;color:#3A2417">{Html(StoreNames.Display(drop.StoreKey))}:
                <span style="text-decoration:line-through;color:#8a7466">{Price(drop.OldPrice)}</span>
                <b style="color:#9C4623">{Price(drop.NewPrice)}</b>
                <span style="color:#1f7a4d">(save {Price(drop.OldPrice - drop.NewPrice)})</span></div>
              <a href="{Html(drop.ProductUrl)}" style="color:#C1592E;font-weight:600">View at the store →</a>
            </td></tr>
            """));

        return $"""
            <div style="font-family:-apple-system,Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px;background:#FEFAF7;color:#3A2417">
              <h2 style="margin:0 0 4px;color:#C1592E">CompareHub</h2>
              <p style="margin:0 0 16px">Hi {Html(userName)}, prices dropped on products in your wishlist:</p>
              <table style="width:100%;border-collapse:collapse">{rows}</table>
              <p style="margin:20px 0"><a href="{Html(wishlistUrl)}" style="background:#C1592E;color:#fff;padding:10px 18px;border-radius:8px;text-decoration:none;font-weight:600">Open your wishlist</a></p>
              <p style="font-size:12px;color:#8a7466">You get this because these products are in your wishlist. You can turn email alerts off in your wishlist settings.</p>
            </div>
            """;
    }
}
