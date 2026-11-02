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
        public void ShowBanner()
        {
            AdsIAPSettings.Log($"[AppLovin] ShowBanner ({BannerId})");
#if APPLOVIN_ENABLED
            MaxSdk.CreateBanner(BannerId, (MaxSdk.BannerPosition)(int)_settings.bannerPosition);
            MaxSdk.ShowBanner(BannerId);
#endif
        }

        public void HideBanner()
        {
            AdsIAPSettings.Log("[AppLovin] HideBanner");
#if APPLOVIN_ENABLED
            MaxSdk.HideBanner(BannerId);
#endif
        }

        public void DestroyBanner()
        {
            AdsIAPSettings.Log("[AppLovin] DestroyBanner");
#if APPLOVIN_ENABLED
            MaxSdk.DestroyBanner(BannerId);
#endif
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
