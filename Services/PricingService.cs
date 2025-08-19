// Services/PricingService.cs
using System;
using System.Collections.Generic;
using System.Linq;

public class PricingService
{
    public static readonly ISet<string> ValidServices = new HashSet<string>
    {
        "Web Services","ERP Systems","Mobile Applications","E-Commerce","Custom Software","SaaS Application"
    };
    public static readonly ISet<string> ValidTiers = new HashSet<string> { "basic", "pro", "premium" };

    private readonly Dictionary<string, ServiceDef> _defs;

    public PricingService()
    {
        _defs = PricingCatalog.Build();
    }

    public List<string> FilterFeatures(string service, string type, IEnumerable<string>? requested)
    {
        if (!_defs.TryGetValue(service, out var def)) return new();
        var allowed = def.Features.ToDictionary(f => f.Value, f => f);
        var result = new List<string>();
        foreach (var key in requested ?? Enumerable.Empty<string>())
        {
            if (!allowed.TryGetValue(key, out var feat)) continue;
            if (feat.Types is { Count: > 0 } && !feat.Types.Contains(type)) continue;
            result.Add(key);
        }
        return result;
    }

    public int ComputePrice(string service, string tier, string type, int pages, IEnumerable<string> features)
    {
        if (!_defs.TryGetValue(service, out var def)) throw new ArgumentException("Invalid service");
        if (!def.Base.TryGetValue(tier, out var basePrice)) throw new ArgumentException("Invalid tier");

        var total = basePrice;
        if (def.PageCost.HasValue)
        {
            var extra = Math.Max(0, pages - 1);
            total += extra * def.PageCost.Value;
        }

        var featMap = def.Features.ToDictionary(f => f.Value, f => f);
        foreach (var key in features)
        {
            if (!featMap.TryGetValue(key, out var f)) continue;
            if (f.Types is { Count: > 0 } && !f.Types.Contains(type)) continue;
            total += f.Price;
        }
        return Math.Max(0, total);
    }

    public int ComputeHours(string service, string tier, int pages, IEnumerable<string> features)
    {
        if (!_defs.TryGetValue(service, out var def)) return 0;
        var hours = def.EstimatedHours.TryGetValue(tier, out var h) ? h : 0;

        if (def.PageCost.HasValue)
        {
            const int HOURLY = 20; // matches your FE baseline
            hours += Math.Max(0, pages - 1) * (def.PageCost.Value / HOURLY);
        }

        if (def.FeatureHours is not null)
            foreach (var key in features)
                if (def.FeatureHours.TryGetValue(key, out var fh)) hours += fh;

        return Math.Max(0, hours);
    }

    // --- Models ---
    public class ServiceDef
    {
        public Dictionary<string, int> Base { get; set; } = new();
        public Dictionary<string, int> EstimatedHours { get; set; } = new();
        public int? PageCost { get; set; }
        public List<FeatureDef> Features { get; set; } = new();
        public Dictionary<string, int>? FeatureHours { get; set; }
    }

    public class FeatureDef
    {
        public string Value { get; set; } = "";
        public int Price { get; set; }
        public List<string>? Types { get; set; }
    }

    private static class PricingCatalog
    {
        public static Dictionary<string, ServiceDef> Build()
        {
            // WEB
            var web = new ServiceDef
            {
                Base = new() { ["basic"] = 160, ["pro"] = 400, ["premium"] = 800 },
                EstimatedHours = new() { ["basic"] = 8, ["pro"] = 20, ["premium"] = 40 },
                PageCost = 200,
                Features = new()
                {
                    new(){ Value="seo", Price=50 },
                    new(){ Value="schema", Price=35 },
                    new(){ Value="speed", Price=40 },
                    new(){ Value="analytics", Price=30 },
                    new(){ Value="contact_form", Price=20 },
                    new(){ Value="map", Price=20 },
                    new(){ Value="faq", Price=25 },
                    new(){ Value="search", Price=40 },
                    new(){ Value="blog", Price=40 },
                    new(){ Value="gallery", Price=30 },
                    new(){ Value="video_section", Price=30 },
                    new(){ Value="newsletter", Price=25 },
                    new(){ Value="popup", Price=20 },
                    new(){ Value="social", Price=20 },
                    new(){ Value="instagram_feed", Price=25 },
                    new(){ Value="testimonials", Price=25 },
                    new(){ Value="multi_language", Price=60 },
                    new(){ Value="cms", Price=60 },
                    new(){ Value="protected_pages", Price=25 },
                    new(){ Value="booking", Price=80, Types = new(){ "salon","spa","clinic","gym","photography","tailor","auto","club","restaurant","car_rental","driving_school","education","lawyer","trainer","hotel" } },
                    new(){ Value="order_online", Price=120, Types = new(){ "bakery","cafe","restaurant","florist","grocery","pharmacy","printshop","foodtruck","butcher" } },
                    new(){ Value="product_catalog", Price=60, Types = new(){ "retail","florist","furniture","bookstore","mobile","pet","hardware","optic","kids","grocery","car_rental","printshop" } },
                    new(){ Value="cart", Price=120, Types = new(){ "retail","bakery","florist","bookstore","mobile","kids","grocery","printshop","butcher" } },
                    new(){ Value="gdpr", Price=35 },
                    new(){ Value="accessibility", Price=40 },
                    new(){ Value="security", Price=35 },
                    new(){ Value="backup", Price=20 }
                },
                FeatureHours = new()
                {
                    ["booking"] = 4,
                    ["order_online"] = 6,
                    ["cart"] = 6,
                    ["multi_language"] = 2
                }
            };

            // ERP
            var erp = new ServiceDef
            {
                Base = new() { ["basic"] = 500, ["pro"] = 1000, ["premium"] = 1800 },
                EstimatedHours = new() { ["basic"] = 25, ["pro"] = 50, ["premium"] = 90 },
                Features = new()
                {
                    new(){ Value="pos", Price=100, Types = new(){ "retail","bakery","cafe","restaurant","florist","bookstore","mobile","hardware","kids","furniture","pharmacy","butcher" } },
                    new(){ Value="appointments", Price=70, Types = new(){ "salon","spa","clinic","gym","tailor","club" } },
                    new(){ Value="inventory", Price=80 },
                    new(){ Value="crm", Price=50 },
                    new(){ Value="sales_reports", Price=60 },
                    new(){ Value="staff", Price=60 },
                    new(){ Value="project_mgmt", Price=70 }
                },
                FeatureHours = new() { ["pos"] = 6, ["project_mgmt"] = 6 }
            };

            // MOBILE
            var mobile = new ServiceDef
            {
                Base = new() { ["basic"] = 600, ["pro"] = 1000, ["premium"] = 1800 },
                EstimatedHours = new() { ["basic"] = 30, ["pro"] = 50, ["premium"] = 90 },
                Features = new()
                {
                    new(){ Value="push", Price=50 },
                    new(){ Value="auth", Price=60 },
                    new(){ Value="sms_verification", Price=50 },
                    new(){ Value="analytics", Price=40 },
                    new(){ Value="payments", Price=100, Types = new(){ "shop_app","booking_app","delivery_app","event_app","restaurant_app","fitness_app","kids_app","photo_app","taxi_app","news_app","inventory_app","finance_app","volunteer_app","custom" } },
                    new(){ Value="chat", Price=80, Types = new(){ "community_app","event_app","fitness_app","photo_app","volunteer_app","custom" } },
                    new(){ Value="gps_tracking", Price=70, Types = new(){ "delivery_app","taxi_app","fitness_app","custom" } },
                },
                FeatureHours = new() { ["payments"] = 5, ["chat"] = 4, ["gps_tracking"] = 5 }
            };

            // E-COM
            var ecommerce = new ServiceDef
            {
                Base = new() { ["basic"] = 800, ["pro"] = 1400, ["premium"] = 2400 },
                EstimatedHours = new() { ["basic"] = 40, ["pro"] = 70, ["premium"] = 120 },
                Features = new()
                {
                    new(){ Value="seo", Price=50 },
                    new(){ Value="analytics", Price=40 },
                    new(){ Value="product_filters", Price=50 },
                    new(){ Value="reviews", Price=40 },
                    new(){ Value="wishlist", Price=35 },
                    new(){ Value="subscription", Price=70 },
                    new(){ Value="multi_vendor", Price=120, Types = new(){ "marketplace" } }
                },
                FeatureHours = new() { ["multi_vendor"] = 6, ["subscription"] = 4 }
            };

            // CUSTOM
            var custom = new ServiceDef
            {
                Base = new() { ["basic"] = 400, ["pro"] = 900, ["premium"] = 1600 },
                EstimatedHours = new() { ["basic"] = 20, ["pro"] = 45, ["premium"] = 80 },
                Features = new()
                {
                    new(){ Value="auth", Price=120 },
                    new(){ Value="docs", Price=100 },
                    new(){ Value="integration_feature", Price=100, Types = new(){ "integration","api","saas","erp" } },
                    new(){ Value="custom_ui", Price=120 }
                },
                FeatureHours = new() { ["integration_feature"] = 6, ["custom_ui"] = 4 }
            };

            // SAAS
            var saas = new ServiceDef
            {
                Base = new() { ["basic"] = 400, ["pro"] = 900, ["premium"] = 1600 },
                EstimatedHours = new() { ["basic"] = 20, ["pro"] = 45, ["premium"] = 80 },
                Features = new()
                {
                    new(){ Value="billing", Price=90 },
                    new(){ Value="sso", Price=65 },
                    new(){ Value="audit_log", Price=40 },
                    new(){ Value="custom_dashboard", Price=50 }
                },
                FeatureHours = new() { ["billing"] = 4, ["sso"] = 4 }
            };

            return new Dictionary<string, ServiceDef>
            {
                ["Web Services"] = web,
                ["ERP Systems"] = erp,
                ["Mobile Applications"] = mobile,
                ["E-Commerce"] = ecommerce,
                ["Custom Software"] = custom,
                ["SaaS Application"] = saas
            };
        }
    }
}
