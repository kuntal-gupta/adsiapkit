using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AdsIAPKit
{
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "AdsIAPKit/Ads Config")]
    public class AdsConfig : ScriptableObject
    {
        [Header("General")]
        public bool enableTestMode = true;
        public bool enableAdsOnStart = true;
        public bool enableGDPRConsent = false;

        [Header("iOS Privacy")]
        public bool enableATT = true;
        [TextArea(2, 3)]
        public string attDescription = "This identifier will be used to deliver personalized ads to you.";

        [Header("Active Provider")]
        public AdsProviderType activeProvider = AdsProviderType.None;

        [Header("AdMob")]
        public AdMobSettings adMob = new AdMobSettings();

        [Header("LevelPlay")]
        [FormerlySerializedAs("ironSource")]
        public LevelPlaySettings levelPlay = new LevelPlaySettings();
    }

    // The numbers are stored in AdsConfig assets, so they never change. 2 (Unity Ads) and 4 (AppLovin MAX)
    // were removed in 2.0.0; a config that still holds one of them loads as no provider.
    public enum AdsProviderType
    {
        None      = 0,
        AdMob     = 1,
        LevelPlay = 3
    }

    // ─── AdMob ────────────────────────────────────────────────────────────────
    [Serializable]
    public class AdMobSettings
    {
        public bool enabled = false;

        [Header("App IDs")]
        public string androidAppId = "ca-app-pub-xxxxxxxxxxxxxxxx~xxxxxxxxxx";
        public string iosAppId     = "ca-app-pub-xxxxxxxxxxxxxxxx~xxxxxxxxxx";

        [Header("Banner")]
        public bool   bannerEnabled   = true;
        public string androidBannerId = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
        public string iosBannerId     = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
        public AdMobBannerPosition bannerPosition = AdMobBannerPosition.Bottom;
        public AdMobBannerSize     bannerSize     = AdMobBannerSize.Banner;
        [Tooltip("When an Adaptive or Smart banner fails to load, retry right away with the standard 320x50 banner.")]
        public bool adaptiveFallbackToStandard = true;

        [Header("Interstitial")]
        public bool   interstitialEnabled   = true;
        public string androidInterstitialId = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
        public string iosInterstitialId     = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";

        [Header("Rewarded")]
        public bool   rewardedEnabled   = true;
        public string androidRewardedId = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
        public string iosRewardedId     = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";

        [Header("Rewarded Interstitial")]
        public bool   rewardedInterstitialEnabled   = false;
        public string androidRewardedInterstitialId = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
        public string iosRewardedInterstitialId     = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";

        [Header("Test IDs (auto-used when Test Mode is on)")]
        public bool useTestIds = true;
    }

    public enum AdMobBannerPosition { Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight, Center }
    public enum AdMobBannerSize    { Banner, MediumRectangle, IABBanner, Leaderboard, SmartBanner, Adaptive }

    // ─── LevelPlay (formerly IronSource) ──────────────────────────────────────
    [Serializable]
    public class LevelPlaySettings
    {
        public bool enabled = false;

        [Header("App Keys")]
        public string androidAppKey = "YOUR_ANDROID_APP_KEY";
        public string iosAppKey     = "YOUR_IOS_APP_KEY";

        [Header("Banner")]
        public bool   bannerEnabled   = true;
        public string androidBannerId = "YOUR_ANDROID_BANNER_AD_UNIT_ID";
        public string iosBannerId     = "YOUR_IOS_BANNER_AD_UNIT_ID";
        public LevelPlayBannerAnchor     bannerPosition = LevelPlayBannerAnchor.Bottom;
        public LevelPlayBannerSizeOption bannerSize   = LevelPlayBannerSizeOption.Banner;

        [Header("Interstitial")]
        public bool   interstitialEnabled   = true;
        public string androidInterstitialId = "YOUR_ANDROID_INTERSTITIAL_AD_UNIT_ID";
        public string iosInterstitialId     = "YOUR_IOS_INTERSTITIAL_AD_UNIT_ID";

        [Header("Rewarded")]
        public bool   rewardedEnabled   = true;
        public string androidRewardedId = "YOUR_ANDROID_REWARDED_AD_UNIT_ID";
        public string iosRewardedId     = "YOUR_IOS_REWARDED_AD_UNIT_ID";

        [Header("Revenue & Testing")]
        [Tooltip("Raise AdsManager.OnImpressionRevenue for every LevelPlay impression.")]
        public bool useImpressionLevelRevenue = true;
        [Tooltip("Enable the LevelPlay Test Suite; open it with AdsManager.OpenAdDebugger(). Turn off for release.")]
        public bool enableTestSuite = false;
    }

    // Named apart from the SDK's own LevelPlayBannerPosition and LevelPlayAdSize types.
    public enum LevelPlayBannerAnchor { Top, Bottom }
    // 0 and 1 keep the meaning of the old IronSource values (Banner, Rectangle); Smart (2) became Adaptive.
    public enum LevelPlayBannerSizeOption { Banner = 0, MediumRectangle = 1, Adaptive = 2, Large = 3 }
}
