using System;
using UnityEngine;

#if ADMOB_ENABLED
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace AdsIAPKit
{
    public class AdMobProvider : IAdsProvider
    {
        public string ProviderName   => "AdMob";
        public bool   IsInitialized  { get; private set; }

        private AdMobSettings _settings;
        private bool          _testMode;

        // Test ad unit IDs (Google's official test IDs)
        private const string GoogleTestPublisher    = "ca-app-pub-3940256099942544";
        private const string TestBannerId           = "ca-app-pub-3940256099942544/6300978111";
        private const string TestBannerIdIOS        = "ca-app-pub-3940256099942544/2934735716";
        private const string TestAdaptiveBannerId   = "ca-app-pub-3940256099942544/9214589741";
        private const string TestAdaptiveBannerIdIOS = "ca-app-pub-3940256099942544/2435281174";
        private const string TestInterstitialId     = "ca-app-pub-3940256099942544/1033173712";
        private const string TestInterstitialIdIOS  = "ca-app-pub-3940256099942544/4411468910";
        private const string TestRewardedId         = "ca-app-pub-3940256099942544/5224354917";
        private const string TestRewardedIdIOS      = "ca-app-pub-3940256099942544/1712485313";

        // Delay before retrying a failed ad load
        private const float RetryDelay = 30f;

        public void Initialize(AdsConfig config)
        {
            _settings = config.adMob;
            _testMode = config.enableTestMode;

#if ADMOB_ENABLED
            if (config.enableGDPRConsent) GatherConsent();
            else StartSdk();
#else
            Debug.LogWarning("[AdsIAPKit] AdMob SDK not found. " +
                             "Import 'Google Mobile Ads Unity Plugin' and rebuild.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free rewards in a build
#endif
        }

#if ADMOB_ENABLED
        private bool _sdkStarted;

        // ── Consent (UMP) ──────────────────────────────────────────────────────
        // Google requires consent from EEA/UK users before ads are requested.
        private void GatherConsent()
        {
            // Consent from a previous session lets ads start without waiting for the form.
            if (ConsentInformation.CanRequestAds()) StartSdk();

            ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
                AdsManager.RunOnMainThread(() =>
                {
                    if (updateError != null)
                    {
                        Debug.LogWarning($"[AdsIAPKit] Consent info update failed: {updateError.Message}");
                        StartSdkIfConsented();
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                        AdsManager.RunOnMainThread(() =>
                        {
                            if (formError != null)
                                Debug.LogWarning($"[AdsIAPKit] Consent form failed: {formError.Message}");
                            StartSdkIfConsented();
                        }));
                }));
        }

        private void StartSdkIfConsented()
        {
            if (ConsentInformation.CanRequestAds()) StartSdk();
            else AdsIAPSettings.Log("AdMob not started: user consent does not allow ad requests.");
        }

        public bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <summary>Re-opens the consent form. Show a "Privacy settings" button when IsPrivacyOptionsRequired.</summary>
        public void ShowPrivacyOptions(Action onClosed = null)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
                AdsManager.RunOnMainThread(() =>
                {
                    if (error != null)
                        Debug.LogWarning($"[AdsIAPKit] Privacy options form failed: {error.Message}");
                    onClosed?.Invoke();
                }));
        }

        private void StartSdk()
        {
            if (_sdkStarted) return;
            _sdkStarted = true;

            // The callback may arrive on a background thread, so hop back before touching state.
            MobileAds.Initialize(status => AdsManager.RunOnMainThread(() =>
            {
                IsInitialized = true;
                Debug.Log("[AdsIAPKit] AdMob SDK initialized.");
                AdsIAPSettings.Log("AdMob initialized.");
                PreloadAds();
            }));
        }
        /// <summary>Opens Google's Ad Inspector to check mediation adapters and test ads on a device.</summary>
        public void OpenAdInspector()
        {
            MobileAds.OpenAdInspector(error =>
            {
                if (error != null)
                    AdsManager.RunOnMainThread(() =>
                        Debug.LogWarning($"[AdsIAPKit] Ad Inspector failed: {error.GetMessage()}"));
            });
        }
#else
        public bool IsPrivacyOptionsRequired => false;
        public void ShowPrivacyOptions(Action onClosed = null) => onClosed?.Invoke();
        public void OpenAdInspector() => AdsIAPSettings.Log("Ad Inspector needs the Google Mobile Ads package.");
#endif

        private void PreloadAds()
        {
            if (_settings.interstitialEnabled) LoadInterstitial();
            if (_settings.rewardedEnabled)     LoadRewarded();
        }

        // ── Banner ─────────────────────────────────────────────────────────────
        private bool _bannerWanted;

        public void ShowBanner()
        {
            if (!_settings.bannerEnabled) return;
            _bannerWanted = true;
#if UNITY_IOS
            string testBannerId = (_settings.bannerSize == AdMobBannerSize.Adaptive || _settings.bannerSize == AdMobBannerSize.SmartBanner)
                ? TestAdaptiveBannerIdIOS : TestBannerIdIOS;
#else
            string testBannerId = (_settings.bannerSize == AdMobBannerSize.Adaptive || _settings.bannerSize == AdMobBannerSize.SmartBanner)
                ? TestAdaptiveBannerId : TestBannerId;
#endif
            string id = ResolveId(testBannerId, _settings.androidBannerId, _settings.iosBannerId);

            Debug.Log($"[AdsIAPKit] Requesting AdMob banner ({id}).");
            AdsIAPSettings.Log($"[AdMob] ShowBanner ({id})");
#if ADMOB_ENABLED
            if (_bannerView != null)
            {
                _bannerView.Show();
                if (_bannerLoaded) ReportBannerLayout();
                return;
            }

            _bannerView = new BannerView(id, ResolveBannerSize(), ResolveBannerPosition());
            _bannerView.OnBannerAdLoaded += () => AdsManager.RunOnMainThread(() =>
            {
                Debug.Log("[AdsIAPKit] AdMob banner loaded.");
                AdsIAPSettings.Log("[AdMob] Banner loaded.");
                _bannerLoaded = _bannerView != null;
                if (!_bannerWanted) _bannerView?.Hide();
                else ReportBannerLayout();
            });
            _bannerView.OnBannerAdLoadFailed += error => AdsManager.RunOnMainThread(() =>
            {
                Debug.LogError($"[AdsIAPKit] AdMob banner failed to load: {error.GetCode()} - {error.GetMessage()}");
                _bannerView?.Destroy();
                _bannerView = null;
                _bannerLoaded = false;
                AdsManager.SetBannerLayout(false, 0f);
                AdsManager.RunDelayed(RetryDelay, () => { if (_bannerWanted) ShowBanner(); });
            });
            _bannerView.OnAdPaid += adValue => AdsManager.RunOnMainThread(() =>
                AdsManager.RaiseImpressionRevenue(adValue.Value / 1000000d));
            _bannerView.LoadAd(new AdRequest());
#else
            // No SDK: simulate a standard banner in the Editor so layouts can be checked.
            if (Application.isEditor) AdsManager.SetBannerLayout(true, AdsManager.DpToPixels(50f));
#endif
        }

        public void HideBanner()
        {
            _bannerWanted = false;
            AdsIAPSettings.Log("[AdMob] HideBanner");
#if ADMOB_ENABLED
            _bannerView?.Hide();
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

        public void DestroyBanner()
        {
            _bannerWanted = false;
            AdsIAPSettings.Log("[AdMob] DestroyBanner");
#if ADMOB_ENABLED
            _bannerView?.Destroy();
            _bannerView = null;
            _bannerLoaded = false;
#endif
            AdsManager.SetBannerLayout(false, 0f);
        }

#if ADMOB_ENABLED
        private BannerView _bannerView;
        private bool       _bannerLoaded;

        private void ReportBannerLayout()
        {
            if (_bannerView == null) return;
            var height = _bannerView.GetHeightInPixels();
            // Some SDK versions report 0 before the first layout pass; fall back to the requested size.
            if (height <= 0f) height = AdsManager.DpToPixels(ResolveBannerSize().Height);
            AdsManager.SetBannerLayout(true, height);
        }

        private AdSize ResolveBannerSize()
        {
            switch (_settings.bannerSize)
            {
                case AdMobBannerSize.MediumRectangle: return AdSize.MediumRectangle;
                case AdMobBannerSize.IABBanner: return AdSize.IABBanner;
                case AdMobBannerSize.Leaderboard: return AdSize.Leaderboard;
                case AdMobBannerSize.SmartBanner:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                case AdMobBannerSize.Adaptive:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                default: return AdSize.Banner;
            }
        }

        private AdPosition ResolveBannerPosition()
        {
            switch (_settings.bannerPosition)
            {
                case AdMobBannerPosition.Top: return AdPosition.Top;
                case AdMobBannerPosition.TopLeft: return AdPosition.TopLeft;
                case AdMobBannerPosition.TopRight: return AdPosition.TopRight;
                case AdMobBannerPosition.BottomLeft: return AdPosition.BottomLeft;
                case AdMobBannerPosition.BottomRight: return AdPosition.BottomRight;
                case AdMobBannerPosition.Center: return AdPosition.Center;
                default: return AdPosition.Bottom;
            }
        }
#endif

        // ── Interstitial ───────────────────────────────────────────────────────
        private Action _onInterstitialClosed;
#if ADMOB_ENABLED
        private InterstitialAd _interstitialAd;
        private bool           _interstitialLoading;

        public bool IsInterstitialReady() => _interstitialAd != null && _interstitialAd.CanShowAd();
#else
        private bool _interstitialReady;

        public bool IsInterstitialReady() => _interstitialReady;
#endif

        public void LoadInterstitial()
        {
            if (!_settings.interstitialEnabled) return;
#if UNITY_IOS
            string testId = TestInterstitialIdIOS;
#else
            string testId = TestInterstitialId;
#endif
            string id = ResolveId(testId, _settings.androidInterstitialId, _settings.iosInterstitialId);

#if ADMOB_ENABLED
            if (_interstitialLoading || IsInterstitialReady()) return;
            _interstitialLoading = true;
            AdsIAPSettings.Log($"[AdMob] LoadInterstitial ({id})");

            InterstitialAd.Load(id, new AdRequest(), (ad, error) => AdsManager.RunOnMainThread(() =>
            {
                _interstitialLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdsIAPKit] AdMob interstitial failed to load: {error?.GetMessage()}");
                    AdsManager.RunDelayed(RetryDelay, LoadInterstitial);
                    return;
                }

                _interstitialAd = ad;
                ad.OnAdFullScreenContentClosed += () => AdsManager.RunOnMainThread(FinishInterstitial);
                ad.OnAdFullScreenContentFailed += showError => AdsManager.RunOnMainThread(() =>
                {
                    Debug.LogWarning($"[AdsIAPKit] AdMob interstitial failed to show: {showError.GetMessage()}");
                    FinishInterstitial();
                });
                ad.OnAdPaid += adValue => AdsManager.RunOnMainThread(() =>
                    AdsManager.RaiseImpressionRevenue(adValue.Value / 1000000d));
                AdsIAPSettings.Log("[AdMob] Interstitial loaded.");
            }));
#else
            AdsIAPSettings.Log($"[AdMob] LoadInterstitial ({id})");
            _interstitialReady = true; // stub
#endif
        }

        public void ShowInterstitial(Action onClosed = null)
        {
            AdsIAPSettings.Log("[AdMob] ShowInterstitial");
            if (!IsInterstitialReady())
            {
                onClosed?.Invoke();
                LoadInterstitial();
                return;
            }

            _onInterstitialClosed = onClosed;
#if ADMOB_ENABLED
            _interstitialAd.Show();
#else
            _interstitialReady = false;
            FinishInterstitial();
#endif
        }

        private void FinishInterstitial()
        {
            var callback = _onInterstitialClosed;
            _onInterstitialClosed = null;
#if ADMOB_ENABLED
            _interstitialAd?.Destroy();
            _interstitialAd = null;
#endif
            callback?.Invoke();
            LoadInterstitial();
        }

        // ── Rewarded ───────────────────────────────────────────────────────────
        private Action<bool> _onRewardedResult;
        private bool         _rewardEarned;
#if ADMOB_ENABLED
        private RewardedAd _rewardedAd;
        private bool       _rewardedLoading;

        public bool IsRewardedReady() => _rewardedAd != null && _rewardedAd.CanShowAd();
#else
        private bool _rewardedReady;

        public bool IsRewardedReady() => _rewardedReady;
#endif

        public void LoadRewarded()
        {
            if (!_settings.rewardedEnabled) return;
#if UNITY_IOS
            string testId = TestRewardedIdIOS;
#else
            string testId = TestRewardedId;
#endif
            string id = ResolveId(testId, _settings.androidRewardedId, _settings.iosRewardedId);

#if ADMOB_ENABLED
            if (_rewardedLoading || IsRewardedReady()) return;
            _rewardedLoading = true;
            AdsIAPSettings.Log($"[AdMob] LoadRewarded ({id})");

            RewardedAd.Load(id, new AdRequest(), (ad, error) => AdsManager.RunOnMainThread(() =>
            {
                _rewardedLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdsIAPKit] AdMob rewarded failed to load: {error?.GetMessage()}");
                    AdsManager.RunDelayed(RetryDelay, LoadRewarded);
                    return;
                }

                _rewardedAd = ad;
                ad.OnAdFullScreenContentClosed += () => AdsManager.RunOnMainThread(FinishRewarded);
                ad.OnAdFullScreenContentFailed += showError => AdsManager.RunOnMainThread(() =>
                {
                    Debug.LogWarning($"[AdsIAPKit] AdMob rewarded failed to show: {showError.GetMessage()}");
                    _rewardEarned = false;
                    FinishRewarded();
                });
                ad.OnAdPaid += adValue => AdsManager.RunOnMainThread(() =>
                    AdsManager.RaiseImpressionRevenue(adValue.Value / 1000000d));
                AdsIAPSettings.Log("[AdMob] Rewarded loaded.");
            }));
#else
            AdsIAPSettings.Log($"[AdMob] LoadRewarded ({id})");
            _rewardedReady = true; // stub
#endif
        }

        public void ShowRewarded(Action<bool> onResult)
        {
            AdsIAPSettings.Log("[AdMob] ShowRewarded");
            if (!IsRewardedReady())
            {
                onResult?.Invoke(false);
                LoadRewarded();
                return;
            }

            _onRewardedResult = onResult;
            _rewardEarned = false;
#if ADMOB_ENABLED
            // The reward is granted when the ad closes, so the game never resumes mid-ad.
            _rewardedAd.Show(reward => _rewardEarned = true);
#else
            _rewardedReady = false;
            _rewardEarned = true; // stub: always reward (Editor only, see Initialize)
            FinishRewarded();
#endif
        }

        private void FinishRewarded()
        {
            var callback = _onRewardedResult;
            var earned = _rewardEarned;
            _onRewardedResult = null;
            _rewardEarned = false;
#if ADMOB_ENABLED
            _rewardedAd?.Destroy();
            _rewardedAd = null;
#endif
            callback?.Invoke(earned);
            LoadRewarded();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private string ResolveId(string testId, string androidId, string iosId)
        {
            if (_testMode && _settings.useTestIds) return testId;
#if UNITY_IOS
            string id = iosId;
#else
            string id = androidId;
#endif
            if (string.IsNullOrEmpty(id) || id.Contains("xxxx"))
                Debug.LogError($"[AdsIAPKit] AdMob ad unit ID is not configured: '{id}'.");
            else if (!_testMode && id.StartsWith(GoogleTestPublisher))
                Debug.LogWarning($"[AdsIAPKit] Test Mode is off but ad unit '{id}' is a Google test ID. Set your real ad unit IDs.");
            return id;
        }
    }
}
