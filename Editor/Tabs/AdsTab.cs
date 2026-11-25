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
        private bool _admobFold     = true;
        private bool _levelPlayFold = false;
        private bool _mediationFold = true;

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
            DrawProviderSetup();
            DrawPrivacySettings();
            DrawSeparator(2);

            DrawAdMobSection();
            DrawSeparator();
            DrawLevelPlaySection();

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

        // ── Packages & mediation for the active provider ───────────────────────
        private void DrawProviderSetup()
        {
            var package = KitPackages.ForProvider(_cfg.activeProvider);
            if (package == null) return;

            GUILayout.Space(6);
            KitPackageGUI.DrawPanel($"📦  Required for {package.DisplayName}", new[] { package });

            GUILayout.Space(4);
            _mediationFold = EditorGUILayout.Foldout(_mediationFold, "🔀  Mediation", true);
            if (!_mediationFold) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                switch (_cfg.activeProvider)
                {
                    case AdsProviderType.AdMob:
                        DrawAdapterMediation(package,
                            "Other networks compete for your AdMob ad requests. Install an adapter for each network, then add the " +
                            "network to a mediation group in the AdMob console (Mediation → Create mediation group). " +
                            "No code changes are needed.",
                            "https://developers.google.com/admob/unity/mediate");
                        break;

                    case AdsProviderType.LevelPlay:
                        GUILayout.Label("LevelPlay installs its own adapters. Open its Network Manager and install the " +
                                        "Google AdMob and Meta Audience Network adapters, then turn the networks on for your " +
                                        "ad units in the LevelPlay dashboard. No code changes are needed.",
                                        EditorStyles.wordWrappedMiniLabel);
                        if (GUILayout.Button("Open LevelPlay Network Manager", GUILayout.Height(22)))
                            OpenMenuOrDocs(package.DocsUrl, "Ads Mediation/Network Manager", "LevelPlay/Network Manager",
                                           "IronSource/Network Manager");
                        GUILayout.Label("Check which networks load on a device with AdsManager.Instance.OpenAdDebugger() " +
                                        "(turn on Enable Test Suite first).", EditorStyles.wordWrappedMiniLabel);
                        break;
                }
            }
        }

        private void DrawAdapterMediation(KitPackage sdk, string help, string docsUrl)
        {
            GUILayout.Label(help, EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(4);

            if (!KitPackageInstaller.IsInstalled(sdk.Id))
            {
                EditorGUILayout.HelpBox($"Install {sdk.DisplayName} first.", MessageType.Info);
                return;
            }

            KitPackageGUI.DrawAdapters(KitPackages.AdaptersFor(_cfg.activeProvider),
                                       KitPackages.AdapterRegistries(_cfg.activeProvider));
            GUILayout.Space(4);
            GUILayout.Label("Check which networks load on a device with AdsManager.Instance.OpenAdDebugger().",
                EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("Mediation Docs", GUILayout.Height(20)))
                Application.OpenURL(docsUrl);
        }

        private static void OpenMenuOrDocs(string docsUrl, params string[] menuPaths)
        {
            foreach (var path in menuPaths)
                if (MenuExists(path) && EditorApplication.ExecuteMenuItem(path)) return;
            Application.OpenURL(docsUrl);
        }

        private static bool MenuExists(string path)
        {
            var root = path.Substring(0, path.IndexOf('/'));
            return System.Array.Exists(Unsupported.GetSubmenus(root), m => m == path);
        }

        // ── Privacy ────────────────────────────────────────────────────────────
        private void DrawPrivacySettings()
        {
            DrawSeparator(1);
            GUILayout.Label("🔒  iOS App Tracking Transparency", SectionStyle());

            EditorGUI.BeginChangeCheck();
            _cfg.enableATT = EditorGUILayout.Toggle(
                new GUIContent("Request ATT", "Ask for tracking permission on iOS before ads load"), _cfg.enableATT);
            if (_cfg.enableATT)
            {
                EditorGUILayout.LabelField("Usage Description", EditorStyles.miniBoldLabel);
                _cfg.attDescription = EditorGUILayout.TextArea(_cfg.attDescription, GUILayout.Height(36));
            }
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            if (_cfg.enableATT)
                KitPackageGUI.DrawPanel("📦  Needed for the ATT prompt", new[] { KitPackages.IosSupport });
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
                    if (AdMobProvider.IsAdaptiveSize(cfg.bannerSize))
                        cfg.adaptiveFallbackToStandard = EditorGUILayout.Toggle(
                            new GUIContent("Fall Back To Standard", "If the adaptive banner fails to load, retry right away with the standard 320x50 banner"),
                            cfg.adaptiveFallbackToStandard);
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

        // ── LevelPlay ──────────────────────────────────────────────────────────
        private void DrawLevelPlaySection()
        {
            var cfg = _cfg.levelPlay;
            _levelPlayFold = EditorGUILayout.Foldout(_levelPlayFold, "", true);
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 320, lastRect.height),
                      $"🟣  LevelPlay (IronSource){(_cfg.activeProvider == AdsProviderType.LevelPlay ? " ★ ACTIVE" : "")}", HeaderStyle());

            if (!_levelPlayFold) return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            cfg.enabled = EditorGUILayout.Toggle("Enable LevelPlay", cfg.enabled);
            if (cfg.enabled)
            {
                GUILayout.Label("App Keys", SectionStyle());
                cfg.androidAppKey = EditorGUILayout.TextField("Android App Key", cfg.androidAppKey);
                cfg.iosAppKey     = EditorGUILayout.TextField("iOS App Key",     cfg.iosAppKey);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Banner", SectionStyle());
                cfg.bannerEnabled = EditorGUILayout.Toggle("Banner Enabled", cfg.bannerEnabled);
                if (cfg.bannerEnabled)
                {
                    cfg.androidBannerId = EditorGUILayout.TextField("Android Ad Unit", cfg.androidBannerId);
                    cfg.iosBannerId     = EditorGUILayout.TextField("iOS Ad Unit",     cfg.iosBannerId);
                    cfg.bannerPosition  = (LevelPlayBannerAnchor)EditorGUILayout.EnumPopup("Position", cfg.bannerPosition);
                    cfg.bannerSize      = (LevelPlayBannerSizeOption)EditorGUILayout.EnumPopup("Size", cfg.bannerSize);
                }

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Interstitial", SectionStyle());
                cfg.interstitialEnabled = EditorGUILayout.Toggle("Interstitial Enabled", cfg.interstitialEnabled);
                if (cfg.interstitialEnabled)
                {
                    cfg.androidInterstitialId = EditorGUILayout.TextField("Android Ad Unit", cfg.androidInterstitialId);
                    cfg.iosInterstitialId     = EditorGUILayout.TextField("iOS Ad Unit",     cfg.iosInterstitialId);
                }

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Rewarded", SectionStyle());
                cfg.rewardedEnabled = EditorGUILayout.Toggle("Rewarded Enabled", cfg.rewardedEnabled);
                if (cfg.rewardedEnabled)
                {
                    cfg.androidRewardedId = EditorGUILayout.TextField("Android Ad Unit", cfg.androidRewardedId);
                    cfg.iosRewardedId     = EditorGUILayout.TextField("iOS Ad Unit",     cfg.iosRewardedId);
                }

                DrawSeparator(0.5f, 4f);
                cfg.useImpressionLevelRevenue = EditorGUILayout.Toggle(
                    new GUIContent("Impression Level Revenue", "Raise AdsManager.OnImpressionRevenue for each impression"),
                    cfg.useImpressionLevelRevenue);
                cfg.enableTestSuite = EditorGUILayout.Toggle(
                    new GUIContent("Enable Test Suite", "Lets AdsManager.OpenAdDebugger() open the LevelPlay Test Suite. Turn off for release."),
                    cfg.enableTestSuite);
                if (cfg.enableTestSuite)
                    EditorGUILayout.HelpBox("Test Suite is on. Turn it off before publishing.", MessageType.Warning);

                if (_cfg.activeProvider != AdsProviderType.LevelPlay)
                {
                    GUILayout.Space(4);
                    if (GUILayout.Button("Set as Active Provider", GUILayout.Height(24)))
                    {
                        _cfg.activeProvider = AdsProviderType.LevelPlay;
                        MarkDirty();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
        }
    }
}
