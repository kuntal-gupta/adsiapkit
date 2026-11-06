using System;
using UnityEngine;

namespace AdsIAPKit
{
    public class UnityAdsProvider : IAdsProvider
    {
        public string ProviderName  => "Unity Ads";
        public bool   IsInitialized { get; private set; }

        private UnityAdsSettings _settings;
        private bool             _testMode;
        private string           _gameId;

        public void Initialize(AdsConfig config)
        {
            _settings = config.unityAds;
            _testMode = config.enableTestMode;

#if UNITY_ANDROID
            _gameId = _settings.androidGameId;
#elif UNITY_IOS
            _gameId = _settings.iosGameId;
#else
            _gameId = _settings.androidGameId;
#endif

#if UNITY_ADS_ENABLED
            Advertisement.Initialize(_gameId, _testMode, new UnityAdsInitializationListener(
                () => { IsInitialized = true; AdsIAPSettings.Log("Unity Ads initialized."); },
                err  => Debug.LogError($"[AdsIAPKit] Unity Ads init error: {err}")
            ));
#else
            Debug.LogWarning("[AdsIAPKit] Unity Ads package not found. " +
                             "Install 'com.unity.ads' from Package Manager.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free rewards in a build
#endif
        }

        private string BannerUnitId       => Application.platform == RuntimePlatform.IPhonePlayer
                                                ? _settings.iosBannerAdUnitId : _settings.bannerAdUnitId;
        private string InterstitialUnitId => Application.platform == RuntimePlatform.IPhonePlayer
                                                ? _settings.iosInterstitialAdUnitId : _settings.interstitialAdUnitId;
        private string RewardedUnitId     => Application.platform == RuntimePlatform.IPhonePlayer
                                                ? _settings.iosRewardedAdUnitId : _settings.rewardedAdUnitId;

        // ── Banner ─────────────────────────────────────────────────────────────
        public void ShowBanner()
        {
            if (!_settings.bannerEnabled) return;
            AdsIAPSettings.Log($"[UnityAds] ShowBanner ({BannerUnitId})");
#if UNITY_ADS_ENABLED
            BannerLoadOptions options = new BannerLoadOptions
            {
                loadCallback  = () =>
                {
                    Advertisement.Banner.Show(BannerUnitId);
                    // Unity Ads banners are 320x50 dp.
                    AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(50f));
                },
                errorCallback = err =>
                {
                    Debug.LogError($"Banner error: {err}");
                    AdsManager.SetBannerLayout(false, 0f);
                }
            };
            Advertisement.Banner.SetPosition((BannerPosition)(int)_settings.bannerPosition);
            Advertisement.Banner.Load(BannerUnitId, options);
#else
            if (Application.isEditor) AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(50f));
#endif
        }

        public void HideBanner()
        {
            AdsIAPSettings.Log("[UnityAds] HideBanner");
#if UNITY_ADS_ENABLED
            Advertisement.Banner.Hide();
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

        public void DestroyBanner()
        {
            AdsIAPSettings.Log("[UnityAds] DestroyBanner");
#if UNITY_ADS_ENABLED
            Advertisement.Banner.Hide(true);
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

        // ── Interstitial ───────────────────────────────────────────────────────
        private bool   _interstitialReady;
        private Action _onInterstitialClosed;

        public bool IsInterstitialReady() => _interstitialReady;

        public void LoadInterstitial()
        {
            AdsIAPSettings.Log($"[UnityAds] LoadInterstitial ({InterstitialUnitId})");
#if UNITY_ADS_ENABLED
            Advertisement.Load(InterstitialUnitId, new UnityAdsLoadListener(
                unitId => _interstitialReady = true,
                (unitId, err, msg) => Debug.LogError($"[UnityAds] Load error: {msg}")
            ));
#else
            _interstitialReady = true;
#endif
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            _onInterstitialClosed = onClosed;
            AdsIAPSettings.Log("[UnityAds] ShowInterstitial");
#if UNITY_ADS_ENABLED
            Advertisement.Show(InterstitialUnitId, new UnityAdsShowListener(
                (unitId, result) =>
                {
                    _interstitialReady = false;
                    onClosed?.Invoke();
                    if (result != ShowResult.Finished)
                        Debug.LogWarning($"[UnityAds] Interstitial show result: {result}");
                }
            ));
#else
            _interstitialReady = false;
            onClosed?.Invoke();
#endif
        }

        // ── Rewarded ───────────────────────────────────────────────────────────
        private bool         _rewardedReady;
        private Action<bool> _onRewardedResult;

        public bool IsRewardedReady() => _rewardedReady;

        public void LoadRewarded()
        {
            AdsIAPSettings.Log($"[UnityAds] LoadRewarded ({RewardedUnitId})");
#if UNITY_ADS_ENABLED
            Advertisement.Load(RewardedUnitId, new UnityAdsLoadListener(
                unitId => _rewardedReady = true,
                (unitId, err, msg) => Debug.LogError($"[UnityAds] Rewarded load error: {msg}")
            ));
#else
            _rewardedReady = true;
#endif
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            _onRewardedResult = onResult;
            AdsIAPSettings.Log("[UnityAds] ShowRewarded");
#if UNITY_ADS_ENABLED
            Advertisement.Show(RewardedUnitId, new UnityAdsShowListener(
                (unitId, result) =>
                {
                    _rewardedReady = false;
                    bool rewarded  = result == ShowResult.Finished;
                    onResult?.Invoke(rewarded);
                }
            ));
#else
            _rewardedReady = false;
            onResult?.Invoke(true);
#endif
        }
    }
}
