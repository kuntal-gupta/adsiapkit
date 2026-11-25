using System;
using UnityEngine;

#if LEVELPLAY_ENABLED
using Unity.Services.LevelPlay;
#endif

namespace AdsIAPKit
{
    /// <summary>
    /// Unity LevelPlay (formerly IronSource) on the LevelPlay 9 API. Mediated networks such as
    /// AdMob and Meta Audience Network are installed with the LevelPlay Network Manager.
    /// </summary>
    public class LevelPlayProvider : IAdsProvider
    {
        public string ProviderName  => "LevelPlay";
        public bool   IsInitialized { get; private set; }

        private LevelPlaySettings _settings;

        // Delay before retrying a failed ad load.
        private const float RetryDelay = 30f;

        // The rewarded close and reward callbacks arrive in either order; wait this long after a close for the reward.
        private const float RewardGraceSeconds = 1f;

        public void Initialize(AdsConfig config)
        {
            _settings = config.levelPlay;

#if LEVELPLAY_ENABLED
#if UNITY_IOS
            var appKey = _settings.iosAppKey;
#else
            var appKey = _settings.androidAppKey;
#endif
            if (string.IsNullOrEmpty(appKey) || appKey.StartsWith("YOUR_"))
                Debug.LogError($"[AdsIAPKit] LevelPlay app key is not configured: '{appKey}'.");

            if (_settings.enableTestSuite) LevelPlay.SetMetaData("is_test_suite", "enable");

            // Listeners go on before Init, as LevelPlay requires.
            LevelPlay.OnInitSuccess += _ => AdsManager.RunOnMainThread(OnInitSuccess);
            LevelPlay.OnInitFailed  += error => AdsManager.RunOnMainThread(() =>
                Debug.LogError($"[AdsIAPKit] LevelPlay initialization failed: {error}"));
#if !LEVELPLAY_9_6
            // Before 9.6 impressions come from one global event; 9.6 moved it onto each ad (see TrackRevenue).
            if (_settings.useImpressionLevelRevenue) LevelPlay.OnImpressionDataReady += OnImpression;
#endif

            LevelPlay.Init(appKey);
#else
            Debug.LogWarning("[AdsIAPKit] LevelPlay SDK not found. Install 'com.unity.services.levelplay' 9.0 or newer.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free rewards in a build
#endif
        }

#if LEVELPLAY_ENABLED
        private static void OnImpression(LevelPlayImpressionData data) =>
            AdsManager.RunOnMainThread(() => AdsManager.RaiseImpressionRevenue(data?.Revenue ?? 0d));
#endif

        private void OnInitSuccess()
        {
            IsInitialized = true;
            AdsIAPSettings.Log("LevelPlay initialized.");
            if (_settings.interstitialEnabled) LoadInterstitial();
            if (_settings.rewardedEnabled)     LoadRewarded();
        }

        /// <summary>Opens the LevelPlay Test Suite. Needs Enable Test Suite in the LevelPlay settings.</summary>
        public void LaunchTestSuite()
        {
#if LEVELPLAY_ENABLED
            if (!_settings.enableTestSuite)
            {
                Debug.LogWarning("[AdsIAPKit] Turn on Enable Test Suite in the LevelPlay settings to open the Test Suite.");
                return;
            }
            if (!IsInitialized) { AdsIAPSettings.Log("LevelPlay Test Suite needs the SDK to be initialized."); return; }
            LevelPlay.LaunchTestSuite();
#else
            AdsIAPSettings.Log("The Test Suite needs the LevelPlay package.");
#endif
        }

        private static string PlatformId(string androidId, string iosId, string format)
        {
#if UNITY_IOS
            var id = iosId;
#else
            var id = androidId;
#endif
            if (string.IsNullOrEmpty(id) || id.StartsWith("YOUR_"))
                Debug.LogError($"[AdsIAPKit] LevelPlay {format} ad unit ID is not configured: '{id}'.");
            return id;
        }

        // ── Banner ─────────────────────────────────────────────────────────────
#pragma warning disable CS0414 // read only when the LevelPlay SDK is installed
        private bool _bannerWanted;
        private bool _bannerLoaded;
#pragma warning restore CS0414

        public void ShowBanner()
        {
            if (!_settings.bannerEnabled) return;
            _bannerWanted = true;
            AdsIAPSettings.Log("[LevelPlay] ShowBanner");
#if LEVELPLAY_ENABLED
            if (_banner != null)
            {
                if (_bannerLoaded)
                {
                    _banner.ShowAd();
                    ReportBannerLayout();
                }
                return; // still loading: it is shown from OnAdLoaded
            }

            _bannerSize = ResolveBannerSize();
            var builder = new LevelPlayBannerAd.Config.Builder();
            builder.SetSize(_bannerSize);
            builder.SetPosition(_settings.bannerPosition == LevelPlayBannerAnchor.Top
                ? Unity.Services.LevelPlay.LevelPlayBannerPosition.TopCenter
                : Unity.Services.LevelPlay.LevelPlayBannerPosition.BottomCenter);
            builder.SetDisplayOnLoad(false); // shown below only while the game still wants it

            var banner = new LevelPlayBannerAd(PlatformId(_settings.androidBannerId, _settings.iosBannerId, "banner"),
                builder.Build());
            _banner = banner;
#if LEVELPLAY_9_6
            if (_settings.useImpressionLevelRevenue) banner.OnAdImpressionDataReady += OnImpression;
#endif

            // Auto-refresh raises OnAdLoaded again for every new creative.
            banner.OnAdLoaded += _ =>
            {
                if (banner != _banner) return;
                _bannerLoaded = true;
                if (!_bannerWanted) return;
                banner.ShowAd();
                ReportBannerLayout();
            };
            banner.OnAdLoadFailed += error =>
            {
                if (banner != _banner) return;
                // A failed refresh keeps the previous creative on screen; only a banner that never loaded is retried.
                if (_bannerLoaded) return;
                Debug.LogWarning($"[AdsIAPKit] LevelPlay banner failed to load: {error}");
                banner.DestroyAd();
                _banner = null;
                AdsManager.SetBannerLayout(false, 0f);
                AdsManager.RunDelayed(RetryDelay, () => { if (_bannerWanted && _banner == null) ShowBanner(); });
            };
            banner.LoadAd();
#else
            // No SDK: simulate a standard banner in the Editor so layouts can be checked.
            if (Application.isEditor) AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(50f));
#endif
        }

        public void HideBanner()
        {
            _bannerWanted = false;
            AdsIAPSettings.Log("[LevelPlay] HideBanner");
#if LEVELPLAY_ENABLED
            _banner?.HideAd();
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

        public void DestroyBanner()
        {
            _bannerWanted = false;
            _bannerLoaded = false;
            AdsIAPSettings.Log("[LevelPlay] DestroyBanner");
#if LEVELPLAY_ENABLED
            // A destroyed LevelPlay banner cannot be reused; the next ShowBanner creates a new one.
            _banner?.DestroyAd();
            _banner = null;
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

#if LEVELPLAY_ENABLED
        private LevelPlayBannerAd _banner;
        private LevelPlayAdSize   _bannerSize;

        private LevelPlayAdSize ResolveBannerSize()
        {
            switch (_settings.bannerSize)
            {
                case LevelPlayBannerSizeOption.Large:           return LevelPlayAdSize.LARGE;
                case LevelPlayBannerSizeOption.MediumRectangle: return LevelPlayAdSize.MEDIUM_RECTANGLE;
                case LevelPlayBannerSizeOption.Adaptive:        return LevelPlayAdSize.CreateAdaptiveAdSize();
                default:                                        return LevelPlayAdSize.BANNER;
            }
        }

        private float FallbackHeightDp()
        {
            switch (_settings.bannerSize)
            {
                case LevelPlayBannerSizeOption.Large:           return 90f;
                case LevelPlayBannerSizeOption.MediumRectangle: return 250f;
                default:                                        return 50f;
            }
        }

        private void ReportBannerLayout()
        {
            // Sizes are in dp (Android) or points (iOS).
            float heightDp = _bannerSize != null ? _bannerSize.Height : 0f;
            if (heightDp <= 0f) heightDp = FallbackHeightDp();
            AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(heightDp));
        }
#endif

        // ── Interstitial ───────────────────────────────────────────────────────
        private Action _onInterstitialClosed;
#if LEVELPLAY_ENABLED
        private LevelPlayInterstitialAd _interstitial;
        private bool                    _interstitialLoading;

        public bool IsInterstitialReady() => _interstitial != null && _interstitial.IsAdReady();
#else
        private bool _interstitialReady;

        public bool IsInterstitialReady() => _interstitialReady;
#endif

        public void LoadInterstitial()
        {
            if (!_settings.interstitialEnabled || !IsInitialized) return;
#if LEVELPLAY_ENABLED
            if (_interstitial == null)
            {
                _interstitial = new LevelPlayInterstitialAd(
                    PlatformId(_settings.androidInterstitialId, _settings.iosInterstitialId, "interstitial"));
#if LEVELPLAY_9_6
                if (_settings.useImpressionLevelRevenue) _interstitial.OnAdImpressionDataReady += OnImpression;
#endif
                _interstitial.OnAdLoaded     += _ => _interstitialLoading = false;
                _interstitial.OnAdLoadFailed += error =>
                {
                    _interstitialLoading = false;
                    Debug.LogWarning($"[AdsIAPKit] LevelPlay interstitial failed to load: {error}");
                    AdsManager.RunDelayed(RetryDelay, LoadInterstitial);
                };
                _interstitial.OnAdClosed        += _ => FinishInterstitial();
                _interstitial.OnAdDisplayFailed += (_, error) =>
                {
                    Debug.LogWarning($"[AdsIAPKit] LevelPlay interstitial failed to show: {error}");
                    FinishInterstitial();
                };
            }

            if (_interstitialLoading || _interstitial.IsAdReady()) return;
            _interstitialLoading = true;
            AdsIAPSettings.Log("[LevelPlay] LoadInterstitial");
            _interstitial.LoadAd();
#else
            AdsIAPSettings.Log("[LevelPlay] LoadInterstitial");
            _interstitialReady = true; // stub
#endif
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            AdsIAPSettings.Log("[LevelPlay] ShowInterstitial");
            if (!IsInterstitialReady())
            {
                onClosed?.Invoke();
                LoadInterstitial();
                return;
            }

            _onInterstitialClosed = onClosed;
#if LEVELPLAY_ENABLED
            _interstitial.ShowAd();
#else
            _interstitialReady = false;
            FinishInterstitial();
#endif
        }

        private void FinishInterstitial()
        {
            var callback = _onInterstitialClosed;
            _onInterstitialClosed = null;
            callback?.Invoke();
            LoadInterstitial();
        }

        // ── Rewarded ───────────────────────────────────────────────────────────
        private Action<bool> _onRewardedResult;
        private bool         _rewardedShowing;
        private bool         _rewardEarned;
#pragma warning disable CS0414 // read only when the LevelPlay SDK is installed
        private bool         _rewardedClosed;
        private int          _rewardedShowId; // ignores a late grace timer from an earlier show
#pragma warning restore CS0414
#if LEVELPLAY_ENABLED
        private LevelPlayRewardedAd _rewarded;
        private bool                _rewardedLoading;

        public bool IsRewardedReady() => _rewarded != null && _rewarded.IsAdReady();
#else
        private bool _rewardedReady;

        public bool IsRewardedReady() => _rewardedReady;
#endif

        public void LoadRewarded()
        {
            if (!_settings.rewardedEnabled || !IsInitialized) return;
#if LEVELPLAY_ENABLED
            if (_rewarded == null)
            {
                _rewarded = new LevelPlayRewardedAd(
                    PlatformId(_settings.androidRewardedId, _settings.iosRewardedId, "rewarded"));
#if LEVELPLAY_9_6
                if (_settings.useImpressionLevelRevenue) _rewarded.OnAdImpressionDataReady += OnImpression;
#endif
                _rewarded.OnAdLoaded     += _ => _rewardedLoading = false;
                _rewarded.OnAdLoadFailed += error =>
                {
                    _rewardedLoading = false;
                    Debug.LogWarning($"[AdsIAPKit] LevelPlay rewarded failed to load: {error}");
                    AdsManager.RunDelayed(RetryDelay, LoadRewarded);
                };
                _rewarded.OnAdRewarded += (_, __) =>
                {
                    _rewardEarned = true;
                    if (_rewardedClosed) FinishRewarded();
                };
                _rewarded.OnAdClosed += _ =>
                {
                    _rewardedClosed = true;
                    if (_rewardEarned) { FinishRewarded(); return; }
                    var showId = _rewardedShowId;
                    AdsManager.RunDelayed(RewardGraceSeconds, () => { if (showId == _rewardedShowId) FinishRewarded(); });
                };
                _rewarded.OnAdDisplayFailed += (_, error) =>
                {
                    Debug.LogWarning($"[AdsIAPKit] LevelPlay rewarded failed to show: {error}");
                    _rewardEarned = false;
                    FinishRewarded();
                };
            }

            if (_rewardedLoading || _rewarded.IsAdReady()) return;
            _rewardedLoading = true;
            AdsIAPSettings.Log("[LevelPlay] LoadRewarded");
            _rewarded.LoadAd();
#else
            AdsIAPSettings.Log("[LevelPlay] LoadRewarded");
            _rewardedReady = true; // stub
#endif
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            AdsIAPSettings.Log("[LevelPlay] ShowRewarded");
            if (!IsRewardedReady())
            {
                onResult?.Invoke(false);
                LoadRewarded();
                return;
            }

            _rewardedShowId++;
            _rewardedShowing = true;
            _onRewardedResult = onResult;
            _rewardEarned = false;
            _rewardedClosed = false;
#if LEVELPLAY_ENABLED
            // The result is reported once the ad has closed, so the game never resumes mid-ad.
            _rewarded.ShowAd();
#else
            _rewardedReady = false;
            _rewardEarned = true; // stub: always reward (Editor only, see Initialize)
            FinishRewarded();
#endif
        }

        // Runs once per show: the first of reward-after-close, close-after-reward, grace timeout or show failure.
        private void FinishRewarded()
        {
            if (!_rewardedShowing) return;
            _rewardedShowing = false;
            var callback = _onRewardedResult;
            var earned = _rewardEarned;
            _onRewardedResult = null;
            _rewardEarned = false;
            _rewardedClosed = false;
            _rewardedShowId++;
            callback?.Invoke(earned);
            LoadRewarded();
        }
    }
}
