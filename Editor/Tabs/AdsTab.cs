using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    public class AdsTab : BaseTab
    {
        public override string TabName => "Ads";
        public override string TabIcon => "📺";

        private AdsConfig _cfg;

        // Foldout states
        private bool _admobFold    = true;
        private bool _unityFold    = false;
        private bool _ironFold     = false;
        private bool _appLovinFold = false;

        private Vector2 _scroll;

        public override void OnEnable(AdsIAPSettings settings)
        {
            base.OnEnable(settings);
            _cfg = settings?.adsConfig;
        }

        public override void Draw()
        {
            if (_cfg == null)
            {
                EditorGUILayout.HelpBox("No AdsConfig assigned. Create one via Assets > Create > AdsIAPKit > Ads Config, then drag it into the master Settings.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawGeneralSettings();
            DrawSeparator(2);

            DrawAdMobSection();
            DrawSeparator();
            DrawUnityAdsSection();
            DrawSeparator();
            DrawIronSourceSection();
            DrawSeparator();
            DrawAppLovinSection();

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        // ── General ────────────────────────────────────────────────────────────
        private void DrawGeneralSettings()
        {
            GUILayout.Label("⚙️  General Settings", SectionStyle());
            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            _cfg.activeProvider  = (AdsProviderType)EditorGUILayout.EnumPopup(
                new GUIContent("Active Provider", "Which ad SDK is active at runtime"), _cfg.activeProvider);
            _cfg.enableTestMode  = EditorGUILayout.Toggle(
                new GUIContent("Test Mode", "Use test/sample ad unit IDs (never ship with this ON)"), _cfg.enableTestMode);
            _cfg.enableAdsOnStart = EditorGUILayout.Toggle(
                new GUIContent("Initialize On Start", "Create AdsManager and start the ad SDK when the first scene loads"),
                _cfg.enableAdsOnStart);
            _cfg.enableGDPRConsent = EditorGUILayout.Toggle(
                new GUIContent("GDPR Consent (UMP)", "Show Google's consent form when required (EEA/UK) before ads are requested. AdMob only."),
                _cfg.enableGDPRConsent);

            if (EditorGUI.EndChangeCheck()) MarkDirty();

            if (_cfg.enableTestMode)
                EditorGUILayout.HelpBox("⚠  Test Mode ON — use test ad IDs only. Disable before publishing.", MessageType.Warning);

            if (_cfg.activeProvider == AdsProviderType.None)
                EditorGUILayout.HelpBox("No active provider selected. Pick one above.", MessageType.Info);
        }

        // ── AdMob ──────────────────────────────────────────────────────────────
        private void DrawAdMobSection()
        {
            var cfg = _cfg.adMob;
            _admobFold = EditorGUILayout.Foldout(_admobFold, "", true);
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 300, lastRect.height),
                      $"🟠  AdMob (Google){(_cfg.activeProvider == AdsProviderType.AdMob ? " ★ ACTIVE" : "")}", HeaderStyle());

            if (!_admobFold) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            cfg.enabled = EditorGUILayout.Toggle("Enable AdMob", cfg.enabled);

            if (cfg.enabled)
            {
                GUILayout.Space(4);
                GUILayout.Label("App IDs", SectionStyle());
                cfg.androidAppId = EditorGUILayout.TextField("Android App ID", cfg.androidAppId);
                cfg.iosAppId     = EditorGUILayout.TextField("iOS App ID",     cfg.iosAppId);
                cfg.useTestIds   = EditorGUILayout.Toggle(
                    new GUIContent("Use Google Test IDs", "Forces Google's official test unit IDs when test mode is on"),
                    cfg.useTestIds);

                DrawSeparator(0.5f, 4f);
                // Banner
                GUILayout.Label("Banner", SectionStyle());
                cfg.bannerEnabled  = EditorGUILayout.Toggle("Banner Enabled", cfg.bannerEnabled);
                if (cfg.bannerEnabled)
                {
                    cfg.androidBannerId = EditorGUILayout.TextField("Android Unit ID", cfg.androidBannerId);
                    cfg.iosBannerId     = EditorGUILayout.TextField("iOS Unit ID",     cfg.iosBannerId);
                    cfg.bannerPosition  = (AdMobBannerPosition)EditorGUILayout.EnumPopup("Position", cfg.bannerPosition);
                    cfg.bannerSize      = (AdMobBannerSize)EditorGUILayout.EnumPopup("Size", cfg.bannerSize);
                }

                DrawSeparator(0.5f, 4f);
                // Interstitial
                GUILayout.Label("Interstitial", SectionStyle());
                cfg.interstitialEnabled   = EditorGUILayout.Toggle("Interstitial Enabled", cfg.interstitialEnabled);
                if (cfg.interstitialEnabled)
                {
                    cfg.androidInterstitialId = EditorGUILayout.TextField("Android Unit ID", cfg.androidInterstitialId);
                    cfg.iosInterstitialId     = EditorGUILayout.TextField("iOS Unit ID",     cfg.iosInterstitialId);
                }

                DrawSeparator(0.5f, 4f);
                // Rewarded
                GUILayout.Label("Rewarded", SectionStyle());
                cfg.rewardedEnabled   = EditorGUILayout.Toggle("Rewarded Enabled", cfg.rewardedEnabled);
                if (cfg.rewardedEnabled)
                {
                    cfg.androidRewardedId = EditorGUILayout.TextField("Android Unit ID", cfg.androidRewardedId);
                    cfg.iosRewardedId     = EditorGUILayout.TextField("iOS Unit ID",     cfg.iosRewardedId);
                }

                DrawSeparator(0.5f, 4f);
                // Rewarded Interstitial
                GUILayout.Label("Rewarded Interstitial", SectionStyle());
                cfg.rewardedInterstitialEnabled = EditorGUILayout.Toggle("Enabled", cfg.rewardedInterstitialEnabled);
                if (cfg.rewardedInterstitialEnabled)
                {
                    cfg.androidRewardedInterstitialId = EditorGUILayout.TextField("Android Unit ID", cfg.androidRewardedInterstitialId);
                    cfg.iosRewardedInterstitialId     = EditorGUILayout.TextField("iOS Unit ID",     cfg.iosRewardedInterstitialId);
                }

                if (_cfg.activeProvider != AdsProviderType.AdMob)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Set as Active Provider", GUILayout.Height(24)))
                    {
                        _cfg.activeProvider = AdsProviderType.AdMob;
                        MarkDirty();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
        }

        // ── Unity Ads ──────────────────────────────────────────────────────────
        private void DrawUnityAdsSection()
        {
            var cfg = _cfg.unityAds;
            _unityFold = EditorGUILayout.Foldout(_unityFold, "", true);
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 320, lastRect.height),
                      $"⬛  Unity Ads{(_cfg.activeProvider == AdsProviderType.UnityAds ? " ★ ACTIVE" : "")}", HeaderStyle());

            if (!_unityFold) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            cfg.enabled = EditorGUILayout.Toggle("Enable Unity Ads", cfg.enabled);
            if (cfg.enabled)
            {
                GUILayout.Label("Game IDs", SectionStyle());
                cfg.androidGameId = EditorGUILayout.TextField("Android Game ID", cfg.androidGameId);
                cfg.iosGameId     = EditorGUILayout.TextField("iOS Game ID",     cfg.iosGameId);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Banner", SectionStyle());
                cfg.bannerEnabled = EditorGUILayout.Toggle("Banner Enabled", cfg.bannerEnabled);
                if (cfg.bannerEnabled)
                {
                    cfg.bannerAdUnitId    = EditorGUILayout.TextField("Android Ad Unit", cfg.bannerAdUnitId);
                    cfg.iosBannerAdUnitId = EditorGUILayout.TextField("iOS Ad Unit",     cfg.iosBannerAdUnitId);
                    cfg.bannerPosition    = (UnityAdsBannerPosition)EditorGUILayout.EnumPopup("Position", cfg.bannerPosition);
                }

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Interstitial", SectionStyle());
                cfg.interstitialEnabled     = EditorGUILayout.Toggle("Interstitial Enabled", cfg.interstitialEnabled);
                cfg.interstitialAdUnitId    = EditorGUILayout.TextField("Android Ad Unit",   cfg.interstitialAdUnitId);
                cfg.iosInterstitialAdUnitId = EditorGUILayout.TextField("iOS Ad Unit",       cfg.iosInterstitialAdUnitId);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Rewarded", SectionStyle());
                cfg.rewardedEnabled     = EditorGUILayout.Toggle("Rewarded Enabled", cfg.rewardedEnabled);
                cfg.rewardedAdUnitId    = EditorGUILayout.TextField("Android Ad Unit", cfg.rewardedAdUnitId);
                cfg.iosRewardedAdUnitId = EditorGUILayout.TextField("iOS Ad Unit",     cfg.iosRewardedAdUnitId);

                if (_cfg.activeProvider != AdsProviderType.UnityAds)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Set as Active Provider", GUILayout.Height(24)))
                    {
                        _cfg.activeProvider = AdsProviderType.UnityAds;
                        MarkDirty();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
        }

        // ── IronSource ─────────────────────────────────────────────────────────
        private void DrawIronSourceSection()
        {
            var cfg = _cfg.ironSource;
            _ironFold = EditorGUILayout.Foldout(_ironFold, "", true);
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 320, lastRect.height),
                      $"🟣  IronSource (LevelPlay){(_cfg.activeProvider == AdsProviderType.IronSource ? " ★ ACTIVE" : "")}", HeaderStyle());

            if (!_ironFold) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            cfg.enabled = EditorGUILayout.Toggle("Enable IronSource", cfg.enabled);
            if (cfg.enabled)
            {
                GUILayout.Label("App Keys", SectionStyle());
                cfg.androidAppKey = EditorGUILayout.TextField("Android App Key", cfg.androidAppKey);
                cfg.iosAppKey     = EditorGUILayout.TextField("iOS App Key",     cfg.iosAppKey);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Ad Units", SectionStyle());
                cfg.bannerEnabled       = EditorGUILayout.Toggle("Banner",             cfg.bannerEnabled);
                cfg.interstitialEnabled = EditorGUILayout.Toggle("Interstitial",       cfg.interstitialEnabled);
                cfg.rewardedEnabled     = EditorGUILayout.Toggle("Rewarded Video",     cfg.rewardedEnabled);
                cfg.offerwallEnabled    = EditorGUILayout.Toggle("Offerwall",          cfg.offerwallEnabled);

                if (cfg.bannerEnabled)
                {
                    DrawSeparator(0.5f, 4f);
                    GUILayout.Label("Banner Settings", SectionStyle());
                    cfg.bannerPosition = (IronSourceBannerPosition)EditorGUILayout.EnumPopup("Position", cfg.bannerPosition);
                    cfg.bannerSize     = (IronSourceBannerSize)EditorGUILayout.EnumPopup("Size", cfg.bannerSize);
                }

                DrawSeparator(0.5f, 4f);
                cfg.useImpressionLevelRevenue = EditorGUILayout.Toggle(
                    new GUIContent("Impression Level Revenue", "Fire ILRD events for each impression"), cfg.useImpressionLevelRevenue);

                if (_cfg.activeProvider != AdsProviderType.IronSource)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Set as Active Provider", GUILayout.Height(24)))
                    {
                        _cfg.activeProvider = AdsProviderType.IronSource;
                        MarkDirty();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
        }

        // ── AppLovin ───────────────────────────────────────────────────────────
        private void DrawAppLovinSection()
        {
            var cfg = _cfg.appLovin;
            _appLovinFold = EditorGUILayout.Foldout(_appLovinFold, "", true);
            var lastRect  = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 320, lastRect.height),
                      $"🔵  AppLovin MAX{(_cfg.activeProvider == AdsProviderType.AppLovin ? " ★ ACTIVE" : "")}", HeaderStyle());

            if (!_appLovinFold) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            cfg.enabled = EditorGUILayout.Toggle("Enable AppLovin MAX", cfg.enabled);
            if (cfg.enabled)
            {
                GUILayout.Label("SDK Key", SectionStyle());
                cfg.sdkKey = EditorGUILayout.TextField("SDK Key", cfg.sdkKey);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Android Ad Unit IDs", SectionStyle());
                cfg.bannerEnabled       = EditorGUILayout.Toggle("Banner", cfg.bannerEnabled);
                if (cfg.bannerEnabled)  cfg.androidBannerId       = EditorGUILayout.TextField("  Banner ID",       cfg.androidBannerId);
                cfg.interstitialEnabled = EditorGUILayout.Toggle("Interstitial", cfg.interstitialEnabled);
                if (cfg.interstitialEnabled) cfg.androidInterstitialId = EditorGUILayout.TextField("  Interstitial ID", cfg.androidInterstitialId);
                cfg.rewardedEnabled     = EditorGUILayout.Toggle("Rewarded", cfg.rewardedEnabled);
                if (cfg.rewardedEnabled) cfg.androidRewardedId    = EditorGUILayout.TextField("  Rewarded ID",     cfg.androidRewardedId);
                cfg.mrecEnabled         = EditorGUILayout.Toggle("MREC Banner", cfg.mrecEnabled);
                if (cfg.mrecEnabled)     cfg.androidMrecId        = EditorGUILayout.TextField("  MREC ID",         cfg.androidMrecId);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("iOS Ad Unit IDs", SectionStyle());
                if (cfg.bannerEnabled)        cfg.iosBannerId       = EditorGUILayout.TextField("  Banner ID",       cfg.iosBannerId);
                if (cfg.interstitialEnabled)  cfg.iosInterstitialId = EditorGUILayout.TextField("  Interstitial ID", cfg.iosInterstitialId);
                if (cfg.rewardedEnabled)      cfg.iosRewardedId     = EditorGUILayout.TextField("  Rewarded ID",     cfg.iosRewardedId);
                if (cfg.mrecEnabled)          cfg.iosMrecId         = EditorGUILayout.TextField("  MREC ID",         cfg.iosMrecId);

                DrawSeparator(0.5f, 4f);
                if (cfg.bannerEnabled)
                    cfg.bannerPosition = (AppLovinBannerPosition)EditorGUILayout.EnumPopup("Banner Position", cfg.bannerPosition);

                GUILayout.Label("Debug", SectionStyle());
                cfg.enableCreativeDebugger  = EditorGUILayout.Toggle("Creative Debugger",  cfg.enableCreativeDebugger);
                cfg.enableMediationDebugger = EditorGUILayout.Toggle("Mediation Debugger", cfg.enableMediationDebugger);

                if (_cfg.activeProvider != AdsProviderType.AppLovin)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Set as Active Provider", GUILayout.Height(24)))
                    {
                        _cfg.activeProvider = AdsProviderType.AppLovin;
                        MarkDirty();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
        }
    }
}
