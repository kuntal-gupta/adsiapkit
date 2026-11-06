using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdsIAPKit
{
    /// <summary>
    /// Drop this prefab in your first scene, or call AdsManager.Instance from anywhere.
    /// It auto-picks the provider that is active in AdsConfig.
    /// </summary>
    /// <summary>The screen edge a banner is anchored to.</summary>
    public enum BannerEdge { Bottom, Top, None }

    public class AdsManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        private static AdsManager _instance;
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[AdsManager]");
                    _instance = go.AddComponent<AdsManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ── State ──────────────────────────────────────────────────────────────
        private IAdsProvider _provider;
        private AdsConfig    _config;
        private bool          _adsInitializedEventRaised;

        public bool  IsInitialized => _provider?.IsInitialized ?? false;
        public string ProviderName  => _provider?.ProviderName ?? "None";

        // ── Events ─────────────────────────────────────────────────────────────
        public static event Action              OnAdsInitialized;
        public static event Action              OnInterstitialClosed;
        public static event Action<bool>        OnRewardedResult;   // true = rewarded
        public static event Action<double>      OnImpressionRevenue; // for ILRD

        /// <summary>
        /// Raised on the main thread whenever the banner is requested, loads, hides, or changes height.
        /// Read IsBannerLoaded, BannerHeightPixels and BannerEdge in the handler.
        /// </summary>
        public static event Action              OnBannerLayoutChanged;

        // ── Banner layout ──────────────────────────────────────────────────────
        /// <summary>True when the game has asked for a banner (ShowBanner called, not hidden or destroyed).</summary>
        public static bool       IsBannerRequested  { get; private set; }

        /// <summary>True when a banner has loaded and is on screen.</summary>
        public static bool       IsBannerLoaded     { get; private set; }

        /// <summary>Height of the banner on screen, in screen pixels. 0 when no banner is showing.</summary>
        public static float      BannerHeightPixels { get; private set; }

        /// <summary>The screen edge the banner is anchored to, from the active provider's settings.</summary>
        public static BannerEdge BannerEdge         { get; private set; } = BannerEdge.Bottom;

        /// <summary>Banner height in the units of the given canvas (screen pixels / canvas scale factor).</summary>
        public static float GetBannerHeight(Canvas canvas)
        {
            if (canvas == null) return BannerHeightPixels;
            var scale = canvas.rootCanvas.scaleFactor;
            return scale > 0f ? BannerHeightPixels / scale : BannerHeightPixels;
        }

        /// <summary>Converts density-independent pixels (dp / points) to screen pixels.</summary>
        public static float DpToPixels(float dp)
        {
            var dpi = Screen.dpi;
            if (dpi <= 0f) dpi = 160f;
            return dp * dpi / 160f;
        }

        // Providers report here when their banner loads, shows, hides or is destroyed.
        internal static void SetBannerLayout(bool loaded, float heightPixels)
        {
            RunOnMainThread(() =>
            {
                heightPixels = loaded ? Mathf.Max(0f, heightPixels) : 0f;
                if (IsBannerLoaded == loaded && Mathf.Approximately(BannerHeightPixels, heightPixels)) return;
                IsBannerLoaded = loaded;
                BannerHeightPixels = heightPixels;
                AdsIAPSettings.Log($"Banner layout: loaded={loaded}, height={heightPixels:0}px, edge={BannerEdge}");
                OnBannerLayoutChanged?.Invoke();
            });
        }

        private static void SetBannerRequested(bool requested)
        {
            if (IsBannerRequested == requested) return;
            IsBannerRequested = requested;
            OnBannerLayoutChanged?.Invoke();
        }

        // ── Unity lifecycle ────────────────────────────────────────────────────
        // Supports Enter Play Mode without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            OnAdsInitialized = null;
            OnInterstitialClosed = null;
            OnRewardedResult = null;
            OnImpressionRevenue = null;
            OnBannerLayoutChanged = null;
            IsBannerRequested = false;
            IsBannerLoaded = false;
            BannerHeightPixels = 0f;
            BannerEdge = BannerEdge.Bottom;
            lock (MainThreadQueue) MainThreadQueue.Clear();
        }

        // "Initialize On Start" in the AdsIAPKit window: start ads without any scene setup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            var settings = AdsIAPSettings.Instance;
            if (settings != null && settings.adsConfig != null && settings.adsConfig.enableAdsOnStart)
                _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
            SceneManager.sceneLoaded += OnSceneLoaded;
#endif
            Initialize();
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // In the Editor the SDK's placeholder banner is a scene object that a scene load destroys,
        // while the native banner on a device survives. Recreate it so the Editor matches the device.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || !IsBannerRequested || !IsInitialized) return;
            try { _provider.DestroyBanner(); }
            catch (Exception exception) { Debug.LogException(exception); }
            _provider.ShowBanner();
        }
#endif

        // ── Initialization ─────────────────────────────────────────────────────
        private void Initialize()
        {
            var settings = AdsIAPSettings.Instance;
            if (settings == null || settings.adsConfig == null) return;

            _config = settings.adsConfig;
            BannerEdge = ResolveBannerEdge(_config);
            AttTracking.RequestIfNeeded(_config);
            _provider = CreateProvider(_config.activeProvider);

            if (_provider == null)
            {
                AdsIAPSettings.Log($"No ads provider set or provider is None.");
                return;
            }

            _provider.Initialize(_config);
            AdsIAPSettings.Log($"Initialized provider: {_provider.ProviderName}");
            if (_provider.IsInitialized)
                NotifyAdsInitialized();
            else
                StartCoroutine(WaitForProviderInitialization());
        }

        private IEnumerator WaitForProviderInitialization()
        {
            // No hard timeout: the consent form can keep the provider waiting on the user.
            var warnAt = Time.unscaledTime + 30f;
            var warned = false;
            while (_provider != null && !_provider.IsInitialized)
            {
                if (!warned && Time.unscaledTime > warnAt)
                {
                    warned = true;
                    Debug.LogWarning("[AdsIAPKit] Ads provider is still initializing after 30s.");
                }
                yield return null;
            }

            if (_provider != null)
                NotifyAdsInitialized();
        }

        private void NotifyAdsInitialized()
        {
            if (_adsInitializedEventRaised) return;
            _adsInitializedEventRaised = true;
            OnAdsInitialized?.Invoke();
            if (IsBannerRequested)
                _provider.ShowBanner();
        }

        private static BannerEdge ResolveBannerEdge(AdsConfig config)
        {
            switch (config.activeProvider)
            {
                case AdsProviderType.AdMob:
                    switch (config.adMob.bannerPosition)
                    {
                        case AdMobBannerPosition.Top:
                        case AdMobBannerPosition.TopLeft:
                        case AdMobBannerPosition.TopRight: return BannerEdge.Top;
                        case AdMobBannerPosition.Center:   return BannerEdge.None;
                        default:                           return BannerEdge.Bottom;
                    }
                case AdsProviderType.UnityAds:
                    switch (config.unityAds.bannerPosition)
                    {
                        case UnityAdsBannerPosition.TopCenter:
                        case UnityAdsBannerPosition.TopLeft:
                        case UnityAdsBannerPosition.TopRight: return BannerEdge.Top;
                        case UnityAdsBannerPosition.Center:   return BannerEdge.None;
                        default:                              return BannerEdge.Bottom;
                    }
                case AdsProviderType.IronSource:
                    return config.ironSource.bannerPosition == IronSourceBannerPosition.Top ? BannerEdge.Top : BannerEdge.Bottom;
                case AdsProviderType.AppLovin:
                    switch (config.appLovin.bannerPosition)
                    {
                        case AppLovinBannerPosition.TopLeft:
                        case AppLovinBannerPosition.TopCenter:
                        case AppLovinBannerPosition.TopRight: return BannerEdge.Top;
                        case AppLovinBannerPosition.Centered: return BannerEdge.None;
                        default:                              return BannerEdge.Bottom;
                    }
                default:
                    return BannerEdge.Bottom;
            }
        }

        internal static void RaiseImpressionRevenue(double revenue)
        {
            OnImpressionRevenue?.Invoke(revenue);
        }

        // ── Main-thread dispatch ───────────────────────────────────────────────
        // Ad SDK callbacks can arrive on a background thread; providers queue work here.
        private static readonly Queue<Action> MainThreadQueue = new Queue<Action>();

        internal static void RunOnMainThread(Action action)
        {
            if (action == null) return;
            lock (MainThreadQueue) MainThreadQueue.Enqueue(action);
        }

        internal static void RunDelayed(float seconds, Action action)
        {
            RunOnMainThread(() => Instance.StartCoroutine(DelayRoutine(seconds, action)));
        }

        private static IEnumerator DelayRoutine(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (MainThreadQueue)
                {
                    if (MainThreadQueue.Count == 0) return;
                    action = MainThreadQueue.Dequeue();
                }

                try { action(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        private IAdsProvider CreateProvider(AdsProviderType type)
        {
            return type switch
            {
                AdsProviderType.AdMob      => new AdMobProvider(),
                AdsProviderType.UnityAds   => new UnityAdsProvider(),
                AdsProviderType.IronSource => new IronSourceProvider(),
                AdsProviderType.AppLovin   => new AppLovinProvider(),
                _                          => null
            };
        }

        // ── Banner ─────────────────────────────────────────────────────────────
        public void ShowBanner()
        {
            SetBannerRequested(true);
            if (!CheckReady("ShowBanner")) return;
            _provider.ShowBanner();
        }

        public void HideBanner()
        {
            SetBannerRequested(false);
            _provider?.HideBanner();
            SetBannerLayout(false, 0f);
        }

        public void DestroyBanner()
        {
            SetBannerRequested(false);
            _provider?.DestroyBanner();
            SetBannerLayout(false, 0f);
        }

        // ── Interstitial ───────────────────────────────────────────────────────
        public void ShowInterstitial(Action onClosed = null)
        {
            if (!CheckReady("ShowInterstitial")) { onClosed?.Invoke(); return; }

            if (!_provider.IsInterstitialReady())
            {
                AdsIAPSettings.Log("Interstitial not ready, loading…");
                _provider.LoadInterstitial();
                onClosed?.Invoke();
                return;
            }

            _provider.ShowInterstitial(() =>
            {
                onClosed?.Invoke();
                OnInterstitialClosed?.Invoke();
            });
        }

        // ── Rewarded ───────────────────────────────────────────────────────────
        public void ShowRewarded(Action<bool> onResult)
        {
            if (!CheckReady("ShowRewarded")) { onResult?.Invoke(false); return; }

            if (!_provider.IsRewardedReady())
            {
                AdsIAPSettings.Log("Rewarded not ready, loading…");
                _provider.LoadRewarded();
                onResult?.Invoke(false);
                return;
            }

            _provider.ShowRewarded(rewarded =>
            {
                onResult?.Invoke(rewarded);
                OnRewardedResult?.Invoke(rewarded);
            });
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        public bool IsInterstitialReady() => _provider?.IsInterstitialReady() ?? false;
        public bool IsRewardedReady()     => _provider?.IsRewardedReady()     ?? false;

        // ── Privacy (UMP consent) ──────────────────────────────────────────────
        /// <summary>True when the user must be offered a way to change consent (e.g. a Settings button).</summary>
        public bool IsPrivacyOptionsRequired => (_provider as AdMobProvider)?.IsPrivacyOptionsRequired ?? false;

        public void ShowPrivacyOptions(Action onClosed = null)
        {
            if (_provider is AdMobProvider adMob) adMob.ShowPrivacyOptions(onClosed);
            else onClosed?.Invoke();
        }

        // ── Debugging ──────────────────────────────────────────────────────────
        /// <summary>
        /// Opens the network's mediation debugger on a device: the Ad Inspector for AdMob or the
        /// Mediation Debugger for AppLovin MAX. Use it in test builds to check that adapters load.
        /// </summary>
        public void OpenAdDebugger()
        {
            switch (_provider)
            {
                case AdMobProvider adMob:       adMob.OpenAdInspector(); break;
                case AppLovinProvider appLovin: appLovin.ShowMediationDebugger(); break;
                default: AdsIAPSettings.Log($"{ProviderName} has no ad debugger."); break;
            }
        }

        private bool CheckReady(string caller)
        {
            if (_provider == null || !_provider.IsInitialized)
            {
                AdsIAPSettings.Log($"[{caller}] Ads not initialized.");
                return false;
            }
            return true;
        }
    }
}
