using System;
using UnityEngine;

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

        [Header("Unity Ads")]
        public UnityAdsSettings unityAds = new UnityAdsSettings();

        [Header("IronSource")]
        public IronSourceSettings ironSource = new IronSourceSettings();

        [Header("AppLovin MAX")]
        public AppLovinSettings appLovin = new AppLovinSettings();
    }

    public enum AdsProviderType
    {
        None,
        AdMob,
        UnityAds,
        IronSource,
        AppLovin
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

    // ─── Unity Ads ────────────────────────────────────────────────────────────
    [Serializable]
    public class UnityAdsSettings
    {
        public bool enabled = false;

        [Header("Game IDs")]
        public string androidGameId = "YOUR_ANDROID_GAME_ID";
        public string iosGameId     = "YOUR_IOS_GAME_ID";

        [Header("Banner")]
        public bool   bannerEnabled  = true;
        public string bannerAdUnitId = "Banner_Android";
        public string iosBannerAdUnitId = "Banner_iOS";
        public UnityAdsBannerPosition bannerPosition = UnityAdsBannerPosition.BottomCenter;

        [Header("Interstitial")]
        public bool   interstitialEnabled     = true;
        public string interstitialAdUnitId    = "Interstitial_Android";
        public string iosInterstitialAdUnitId = "Interstitial_iOS";

        [Header("Rewarded")]
        public bool   rewardedEnabled     = true;
        public string rewardedAdUnitId    = "Rewarded_Android";
        public string iosRewardedAdUnitId = "Rewarded_iOS";
    }

    public enum UnityAdsBannerPosition { TopCenter, TopLeft, TopRight, BottomCenter, BottomLeft, BottomRight, Center }

    // ─── IronSource ───────────────────────────────────────────────────────────
    [Serializable]
    public class IronSourceSettings
    {
        public bool enabled = false;

        [Header("App Keys")]
        public string androidAppKey = "YOUR_ANDROID_APP_KEY";
        public string iosAppKey     = "YOUR_IOS_APP_KEY";

        [Header("Ad Units")]
        public bool bannerEnabled       = true;
        public bool interstitialEnabled = true;
        public bool rewardedEnabled     = true;
        public bool offerwallEnabled    = false;

        [Header("Banner")]
        public IronSourceBannerPosition bannerPosition = IronSourceBannerPosition.Bottom;
        public IronSourceBannerSize     bannerSize     = IronSourceBannerSize.Banner;

        [Header("Segments & Impressions")]
        public bool useImpressionLevelRevenue = true;
    }

    public enum IronSourceBannerPosition { Top, Bottom }
    public enum IronSourceBannerSize    { Banner, Rectangle, Smart }

    // ─── AppLovin MAX ─────────────────────────────────────────────────────────
    [Serializable]
    public class AppLovinSettings
    {
        public bool enabled = false;

        [Header("SDK Key")]
        public string sdkKey = "YOUR_APPLOVIN_SDK_KEY";

        [Header("Ad Unit IDs — Android")]
        public string androidBannerId       = "ANDROID_BANNER_AD_UNIT_ID";
        public string androidInterstitialId = "ANDROID_INTER_AD_UNIT_ID";
        public string androidRewardedId     = "ANDROID_REWARDED_AD_UNIT_ID";
        public string androidMrecId         = "ANDROID_MREC_AD_UNIT_ID";

        [Header("Ad Unit IDs — iOS")]
        public string iosBannerId       = "IOS_BANNER_AD_UNIT_ID";
        public string iosInterstitialId = "IOS_INTER_AD_UNIT_ID";
        public string iosRewardedId     = "IOS_REWARDED_AD_UNIT_ID";
        public string iosMrecId         = "IOS_MREC_AD_UNIT_ID";

        [Header("Features")]
        public bool bannerEnabled       = true;
        public bool interstitialEnabled = true;
        public bool rewardedEnabled     = true;
        public bool mrecEnabled         = false;
        public bool enableCreativeDebugger = false;
        public bool enableMediationDebugger = false;

        [Header("Banner")]
        public AppLovinBannerPosition bannerPosition = AppLovinBannerPosition.BottomCenter;
    }

    public enum AppLovinBannerPosition { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Centered }
}
