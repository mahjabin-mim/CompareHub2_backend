namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public static class StoreNames
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kireibd.com"] = "Kirei",
        ["koreanmartbd.com"] = "Korean Mart",
        ["shop.shajgoj.com"] = "Shajgoj",
        ["groomlybd.com"] = "Groomly",
        ["skincareshop.com.bd"] = "Skincare Shop",
        ["beautybooth.com.bd"] = "Beauty Booth",
        ["emartwayskincare.com.bd"] = "Emartway",
        ["tekka.com.bd"] = "Tekka",
        ["dhalishop.com"] = "Dhali Shop",
        ["thealamsbd.com"] = "The Alams",
        ["thelilium.shop"] = "The Lilium",
        ["klassy.com.bd"] = "Klassy",
        ["pixielabellabangladesh.com"] = "Pixie Labella",
        ["skinnora.com"] = "Skinnora",
        ["makeupchari.com"] = "Makeup Chari",
        ["perfectobd.com"] = "Perfecto",
        ["arogga.com"] = "Arogga"
    };

    public static string Display(string storeKey) => Names.GetValueOrDefault(storeKey, storeKey);
}
