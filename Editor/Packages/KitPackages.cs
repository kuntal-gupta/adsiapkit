using System.Collections.Generic;

namespace AdsIAPKit.Editor
{
    /// <summary>A scoped registry a package is installed from. Null on a package means the Unity registry.</summary>
    internal sealed class KitRegistry
    {
        public readonly string   Name;
        public readonly string   Url;
        public readonly string[] Scopes;

        public KitRegistry(string name, string url, params string[] scopes)
        {
            Name   = name;
            Url    = url;
            Scopes = scopes;
        }
    }

    /// <summary>A UPM package the kit can install from the setup window.</summary>
    internal sealed class KitPackage
    {
        public readonly string        Id;
        public readonly string        DisplayName;
        public readonly KitRegistry[] Registries;
        public readonly string        DocsUrl;
        public string Note;

        /// <summary>Scripting define the integration also needs. No current package sets one.</summary>
        public string RequiredDefine;

        public KitPackage(string id, string displayName, string docsUrl, params KitRegistry[] registries)
        {
            Id          = id;
            DisplayName = displayName;
            DocsUrl     = docsUrl;
            Registries  = registries;
        }
    }

    /// <summary>One mediation network: the adapter package(s) to install for the active ad provider.</summary>
    internal sealed class KitAdapter
    {
        public readonly string   Network;
        public readonly string[] PackageIds;

        public KitAdapter(string network, params string[] packageIds)
        {
            Network    = network;
            PackageIds = packageIds;
        }
    }

    /// <summary>
    /// Every package the kit knows how to install, per feature. Package names were checked
    /// against the Unity and OpenUPM registries.
    /// </summary>
    internal static class KitPackages
    {
        // ── Registries ─────────────────────────────────────────────────────────
        public static readonly KitRegistry OpenUpmGoogle =
            new KitRegistry("OpenUPM", "https://package.openupm.com", "com.google");

        // ── SDKs ───────────────────────────────────────────────────────────────
        public static readonly KitPackage UnityIap = new KitPackage(
            "com.unity.purchasing", "Unity In-App Purchasing 5",
            "https://docs.unity.com/ugs/en-us/manual/iap/manual/overview")
        { Note = "Unity registry. Version 5.0 or newer." };

        public static readonly KitPackage GoogleMobileAds = new KitPackage(
            "com.google.ads.mobile", "Google Mobile Ads (AdMob)",
            "https://developers.google.com/admob/unity/quick-start", OpenUpmGoogle)
        { Note = "From OpenUPM. Brings External Dependency Manager with it." };

        public static readonly KitPackage LevelPlay = new KitPackage(
            "com.unity.services.levelplay", "Unity LevelPlay",
            "https://docs.unity.com/grow/levelplay/sdk/unity/package-integration/")
        { Note = "Unity registry. Version 9.0 or newer. Install mediated networks with its Network Manager." };

        public static readonly KitPackage IosSupport = new KitPackage(
            "com.unity.ads.ios-support", "iOS 14 Advertising Support (ATT)",
            "https://docs.unity.com/ads/en-us/manual/ATTCompliance")
        { Note = "Optional. Shows the App Tracking Transparency prompt on iOS." };

        /// <summary>The SDK package an ad provider needs, or null for None.</summary>
        public static KitPackage ForProvider(AdsProviderType provider)
        {
            switch (provider)
            {
                case AdsProviderType.AdMob:      return GoogleMobileAds;
                case AdsProviderType.LevelPlay:  return LevelPlay;
                default:                         return null;
            }
        }

        /// <summary>Everything the current settings need, required first.</summary>
        public static List<KitPackage> Required(AdsIAPSettings settings)
        {
            var list = new List<KitPackage>();
            if (settings == null) return list;

            var provider = ForProvider(settings.adsConfig != null ? settings.adsConfig.activeProvider : AdsProviderType.None);
            if (provider != null) list.Add(provider);
            if (settings.iapConfig != null && settings.iapConfig.enableIAP) list.Add(UnityIap);
            return list;
        }

        // ── Mediation adapters ─────────────────────────────────────────────────
        /// <summary>
        /// AdMob mediation adapters, published by Google on OpenUPM. LevelPlay installs its own
        /// adapters (AdMob, Meta Audience Network) from its Network Manager, so it has none here.
        /// </summary>
        public static readonly KitAdapter[] AdMobAdapters =
        {
            Google("LevelPlay (IronSource)", "ironsource"),
            Google("Meta Audience Network",  "metaaudiencenetwork"),
        };

        public static KitAdapter[] AdaptersFor(AdsProviderType provider) =>
            provider == AdsProviderType.AdMob ? AdMobAdapters : null;

        /// <summary>Registries an adapter of this provider is installed from.</summary>
        public static KitRegistry[] AdapterRegistries(AdsProviderType provider) => new[] { OpenUpmGoogle };

        private static KitAdapter Google(string network, string key) =>
            new KitAdapter(network, "com.google.ads.mobile.mediation." + key);
    }
}
