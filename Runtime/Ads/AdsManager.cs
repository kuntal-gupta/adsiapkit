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
        private bool          _bannerRequested;

        public bool  IsInitialized => _provider?.IsInitialized ?? false;
        public string ProviderName  => _provider?.ProviderName ?? "None";

        // ── Events ─────────────────────────────────────────────────────────────
        public static event Action              OnAdsInitialized;
        public static event Action              OnInterstitialClosed;
        public static event Action<bool>        OnRewardedResult;   // true = rewarded
        public static event Action<double>      OnImpressionRevenue; // for ILRD

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
            if (mode != LoadSceneMode.Single || !_bannerRequested || !IsInitialized) return;
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
            if (_bannerRequested)
                _provider.ShowBanner();
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
            _bannerRequested = true;
            if (!CheckReady("ShowBanner")) return;
            _provider.ShowBanner();
        }

        public void HideBanner()
        {
            _bannerRequested = false;
            _provider?.HideBanner();
        }

        public void DestroyBanner()
        {
            _bannerRequested = false;
            _provider?.DestroyBanner();
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
