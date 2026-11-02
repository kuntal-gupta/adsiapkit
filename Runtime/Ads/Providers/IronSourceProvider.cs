using System;
using UnityEngine;

namespace AdsIAPKit
{
    public class IronSourceProvider : IAdsProvider
    {
        public string ProviderName  => "IronSource";
        public bool   IsInitialized { get; private set; }

        private IronSourceSettings _settings;
        private Action             _onInterstitialClosed;
        private Action<bool>       _onRewardedResult;

        public void Initialize(AdsConfig config)
        {
            _settings = config.ironSource;

            string appKey =
#if UNITY_ANDROID
                _settings.androidAppKey;
#elif UNITY_IOS
                _settings.iosAppKey;
#else
                _settings.androidAppKey;
#endif

#if IRONSOURCE_ENABLED
            IronSource.Agent.init(appKey);
            if (_settings.bannerEnabled)       IronSource.Agent.initBanner();
            if (_settings.interstitialEnabled) IronSource.Agent.initInterstitial();
            if (_settings.rewardedEnabled)     IronSource.Agent.initRewardedVideo();

            IronSourceEvents.onInterstitialAdClosedEvent     += OnInterstitialClosed;
            IronSourceEvents.onRewardedVideoAdRewardedEvent  += OnRewarded;
            IronSourceEvents.onRewardedVideoAdShowFailedEvent += OnRewardFailed;

            IsInitialized = true;
            AdsIAPSettings.Log("IronSource initialized.");
#else
            Debug.LogWarning("[AdsIAPKit] IronSource SDK not found. " +
                             "Import the IronSource Unity Plugin.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free rewards in a build
#endif
        }

        // ── Banner ─────────────────────────────────────────────────────────────
        public void ShowBanner()
        {
            AdsIAPSettings.Log("[IronSource] ShowBanner");
#if IRONSOURCE_ENABLED
            IronSource.Agent.displayBanner();
#endif
        }

        public void HideBanner()
        {
            AdsIAPSettings.Log("[IronSource] HideBanner");
#if IRONSOURCE_ENABLED
            IronSource.Agent.hideBanner();
#endif
        }

        public void DestroyBanner()
        {
            AdsIAPSettings.Log("[IronSource] DestroyBanner");
#if IRONSOURCE_ENABLED
            IronSource.Agent.destroyBanner();
#endif
        }

        // ── Interstitial ───────────────────────────────────────────────────────
        public bool IsInterstitialReady()
        {
#if IRONSOURCE_ENABLED
            return IronSource.Agent.isInterstitialReady();
#else
            return true;
#endif
        }

        public void LoadInterstitial()
        {
            AdsIAPSettings.Log("[IronSource] LoadInterstitial");
#if IRONSOURCE_ENABLED
            IronSource.Agent.loadInterstitial();
#endif
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            _onInterstitialClosed = onClosed;
            AdsIAPSettings.Log("[IronSource] ShowInterstitial");
#if IRONSOURCE_ENABLED
            IronSource.Agent.showInterstitial();
#else
            onClosed?.Invoke();
#endif
        }

#if IRONSOURCE_ENABLED
        private void OnInterstitialClosed() => _onInterstitialClosed?.Invoke();
#endif

        // ── Rewarded ───────────────────────────────────────────────────────────
        public bool IsRewardedReady()
        {
#if IRONSOURCE_ENABLED
            return IronSource.Agent.isRewardedVideoAvailable();
#else
            return true;
#endif
        }

        public void LoadRewarded()
        {
            // IronSource rewarded video is auto-loaded after init
            AdsIAPSettings.Log("[IronSource] LoadRewarded (auto-managed by SDK)");
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            _onRewardedResult = onResult;
            AdsIAPSettings.Log("[IronSource] ShowRewarded");
#if IRONSOURCE_ENABLED
            IronSource.Agent.showRewardedVideo();
#else
            onResult?.Invoke(true);
#endif
        }

#if IRONSOURCE_ENABLED
        private void OnRewarded(IronSourcePlacement _) => _onRewardedResult?.Invoke(true);
        private void OnRewardFailed(IronSourceError _) => _onRewardedResult?.Invoke(false);
#endif
    }
}
