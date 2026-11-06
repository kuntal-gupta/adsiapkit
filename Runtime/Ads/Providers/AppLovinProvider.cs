using System;
using UnityEngine;

namespace AdsIAPKit
{
    public class AppLovinProvider : IAdsProvider
    {
        public string ProviderName  => "AppLovin MAX";
        public bool   IsInitialized { get; private set; }

        private AppLovinSettings _settings;
        private Action           _onInterstitialClosed;
        private Action<bool>     _onRewardedResult;

        private string BannerId       => Application.platform == RuntimePlatform.IPhonePlayer ? _settings.iosBannerId       : _settings.androidBannerId;
        private string InterstitialId => Application.platform == RuntimePlatform.IPhonePlayer ? _settings.iosInterstitialId : _settings.androidInterstitialId;
        private string RewardedId     => Application.platform == RuntimePlatform.IPhonePlayer ? _settings.iosRewardedId     : _settings.androidRewardedId;

        public void Initialize(AdsConfig config)
        {
            _settings = config.appLovin;

#if APPLOVIN_ENABLED
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfig =>
            {
                IsInitialized = true;
                AdsIAPSettings.Log("AppLovin MAX initialized.");
                PreloadAds();

                if (_settings.enableMediationDebugger)
                    MaxSdk.ShowMediationDebugger();
            };
            MaxSdk.SetSdkKey(_settings.sdkKey);
            MaxSdk.InitializeSdk();

            // Wire callbacks
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent           += (adUnitId, info) => { _onInterstitialClosed?.Invoke(); LoadInterstitial(); };
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent        += (adUnitId, reward, info) => _onRewardedResult?.Invoke(true);
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent                += (adUnitId, info) => LoadRewarded();
            MaxSdkCallbacks.Banner.OnAdLoadedEvent                  += (adUnitId, info) =>
            {
                _bannerLoaded = true;
                if (_bannerWanted) ReportBannerLayout();
            };
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent              += (adUnitId, error) =>
            {
                // MAX keeps showing the previous banner on a failed refresh; only clear if nothing ever loaded.
                if (!_bannerLoaded) AdsManager.SetBannerLayout(false, 0f);
            };
#else
            Debug.LogWarning("[AdsIAPKit] AppLovin MAX SDK not found. " +
                             "Import the AppLovin MAX Unity Plugin.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free rewards in a build
#endif
        }

        private void PreloadAds()
        {
            if (_settings.interstitialEnabled) LoadInterstitial();
            if (_settings.rewardedEnabled)     LoadRewarded();
        }

        /// <summary>Opens the MAX Mediation Debugger to check adapters and test ads on a device.</summary>
        public void ShowMediationDebugger()
        {
#if APPLOVIN_ENABLED
            MaxSdk.ShowMediationDebugger();
#else
            AdsIAPSettings.Log("Mediation Debugger needs the AppLovin MAX package.");
#endif
        }

        // ── Banner ─────────────────────────────────────────────────────────────
#pragma warning disable CS0414 // read only when the MAX SDK is installed
        private bool _bannerWanted;
        private bool _bannerLoaded;
        private bool _bannerCreated;
#pragma warning restore CS0414

        public void ShowBanner()
        {
            _bannerWanted = true;
            AdsIAPSettings.Log($"[AppLovin] ShowBanner ({BannerId})");
#if APPLOVIN_ENABLED
            if (!_bannerCreated)
            {
                MaxSdk.CreateBanner(BannerId, (MaxSdk.BannerPosition)(int)_settings.bannerPosition);
                _bannerCreated = true;
            }
            MaxSdk.ShowBanner(BannerId);
            if (_bannerLoaded) ReportBannerLayout();
#else
            if (Application.isEditor) AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(50f));
#endif
        }

#if APPLOVIN_ENABLED
        private void ReportBannerLayout()
        {
            // MAX uses adaptive banners by default; the height is in dp.
            var heightDp = MaxSdkUtils.GetAdaptiveBannerHeight();
            if (heightDp <= 0f) heightDp = MaxSdkUtils.IsTablet() ? 90f : 50f;
            AdsManager.SetBannerLayout(true, heightDp * MaxSdkUtils.GetScreenDensity());
        }
#endif

        public void HideBanner()
        {
            AdsIAPSettings.Log("[AppLovin] HideBanner");
#if APPLOVIN_ENABLED
            MaxSdk.HideBanner(BannerId);
#endif
            _bannerWanted = false;
            AdsManager.SetBannerLayout(false, 0f);
        }

        public void DestroyBanner()
        {
            AdsIAPSettings.Log("[AppLovin] DestroyBanner");
#if APPLOVIN_ENABLED
            MaxSdk.DestroyBanner(BannerId);
            _bannerCreated = false;
#endif
            _bannerWanted = false;
            _bannerLoaded = false;
            AdsManager.SetBannerLayout(false, 0f);
        }

        // ── Interstitial ───────────────────────────────────────────────────────
        public bool IsInterstitialReady()
        {
#if APPLOVIN_ENABLED
            return MaxSdk.IsInterstitialReady(InterstitialId);
#else
            return true;
#endif
        }

        public void LoadInterstitial()
        {
            AdsIAPSettings.Log($"[AppLovin] LoadInterstitial ({InterstitialId})");
#if APPLOVIN_ENABLED
            MaxSdk.LoadInterstitial(InterstitialId);
#endif
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            _onInterstitialClosed = onClosed;
            AdsIAPSettings.Log("[AppLovin] ShowInterstitial");
#if APPLOVIN_ENABLED
            MaxSdk.ShowInterstitial(InterstitialId);
#else
            onClosed?.Invoke();
#endif
        }

        // ── Rewarded ───────────────────────────────────────────────────────────
        public bool IsRewardedReady()
        {
#if APPLOVIN_ENABLED
            return MaxSdk.IsRewardedAdReady(RewardedId);
#else
            return true;
#endif
        }

        public void LoadRewarded()
        {
            AdsIAPSettings.Log($"[AppLovin] LoadRewarded ({RewardedId})");
#if APPLOVIN_ENABLED
            MaxSdk.LoadRewardedAd(RewardedId);
#endif
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            _onRewardedResult = onResult;
            AdsIAPSettings.Log("[AppLovin] ShowRewarded");
#if APPLOVIN_ENABLED
            MaxSdk.ShowRewardedAd(RewardedId);
#else
            onResult?.Invoke(true);
#endif
        }
    }
}
