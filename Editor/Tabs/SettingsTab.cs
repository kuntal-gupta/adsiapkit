using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    public class SettingsTab : BaseTab
    {
        public override string TabName => "Settings";
        public override string TabIcon => "⚙️";

        private Vector2 _scroll;

        public override void Draw()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawAboutSection();
            DrawSeparator(2);
            DrawDebugSection();
            DrawSeparator(2);
            DrawSDKLinksSection();
            DrawSeparator(2);
            DrawDangerZone();

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        private void DrawAboutSection()
        {
            GUILayout.Label("ℹ️  About AdsIAPKit", SectionStyle());
            GUILayout.Space(6);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            GUILayout.Label("AdsIAPKit v1.0.0 — Unified Ads & IAP manager for Unity.", infoStyle);
            GUILayout.Label("Supports AdMob · Unity Ads · IronSource · AppLovin MAX · Unity IAP.", infoStyle);
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            if (Settings != null)
            {
                GUILayout.Label("Settings Asset:");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(Settings, typeof(AdsIAPSettings), false);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            if (Settings != null)
                Settings.enableVerboseLogs = EditorGUILayout.Toggle("Verbose Logging", Settings.enableVerboseLogs);
            if (EditorGUI.EndChangeCheck()) MarkDirty();
        }

        private void DrawDebugSection()
        {
            GUILayout.Label("🐛  Debug", SectionStyle());
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Settings Asset", GUILayout.Height(26)))
            {
                if (Settings != null) EditorGUIUtility.PingObject(Settings);
            }
            if (GUILayout.Button("Ping Ads Config", GUILayout.Height(26)))
            {
                if (Settings?.adsConfig != null) EditorGUIUtility.PingObject(Settings.adsConfig);
            }
            if (GUILayout.Button("Ping IAP Config", GUILayout.Height(26)))
            {
                if (Settings?.iapConfig != null) EditorGUIUtility.PingObject(Settings.iapConfig);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("Open Package Manager", GUILayout.Height(26)))
                EditorApplication.ExecuteMenuItem("Window/Package Manager");

            GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Defined symbols detected:\n" +
                $"  UNITY_IAP_ENABLED : {IsSymbolDefined("UNITY_IAP_ENABLED")}\n" +
                $"  ADMOB_ENABLED     : {IsSymbolDefined("ADMOB_ENABLED")}\n" +
                $"  UNITY_ADS_ENABLED : {IsSymbolDefined("UNITY_ADS_ENABLED")}\n" +
                $"  IRONSOURCE_ENABLED: {IsSymbolDefined("IRONSOURCE_ENABLED")}\n" +
                $"  APPLOVIN_ENABLED  : {IsSymbolDefined("APPLOVIN_ENABLED")}",
                MessageType.None);
        }

        private void DrawSDKLinksSection()
        {
            GUILayout.Label("📦  SDK Installation Guides", SectionStyle());
            GUILayout.Space(4);

            DrawSDKRow("AdMob (Google Mobile Ads)",   "https://developers.google.com/admob/unity/quick-start");
            DrawSDKRow("Unity Ads",                   "https://docs.unity.com/ads/en-us/manual/UnityAdsHome");
            DrawSDKRow("IronSource (LevelPlay)",      "https://developers.is.com/ironsource-mobile/unity/");
            DrawSDKRow("AppLovin MAX",                "https://developers.applovin.com/en/unity/overview/integration");
            DrawSDKRow("Unity IAP",                   "https://docs.unity3d.com/Manual/UnityIAP.html");
        }

        private static void DrawSDKRow(string name, string url)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(220));
            if (GUILayout.Button("Open Docs ↗", GUILayout.Width(90), GUILayout.Height(20)))
                Application.OpenURL(url);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDangerZone()
        {
            GUILayout.Label("⚠️  Danger Zone", SectionStyle());
            GUILayout.Space(4);

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 0.5f);
            if (GUILayout.Button("Reset All Settings to Default", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings",
                    "This will reset ALL ads and IAP configuration to defaults. This cannot be undone.",
                    "Reset", "Cancel"))
                {
                    if (Settings?.adsConfig != null)
                    {
                        Settings.adsConfig.activeProvider = AdsProviderType.None;
                        Settings.adsConfig.enableTestMode = true;
                    }
                    MarkDirty();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        // Most SDK symbols come from the package's version defines, which this assembly shares,
        // so they are checked at compile time; IronSource still needs a Scripting Define Symbol.
        private static string IsSymbolDefined(string symbol)
        {
            bool defined;
            switch (symbol)
            {
#if UNITY_IAP_ENABLED
                case "UNITY_IAP_ENABLED": defined = true; break;
#endif
#if ADMOB_ENABLED
                case "ADMOB_ENABLED": defined = true; break;
#endif
#if UNITY_ADS_ENABLED
                case "UNITY_ADS_ENABLED": defined = true; break;
#endif
#if APPLOVIN_ENABLED
                case "APPLOVIN_ENABLED": defined = true; break;
#endif
                default: defined = AdsIAPKitSdkChecker.IsScriptingSymbolDefined(symbol); break;
            }
            return defined ? "✓ YES" : "✗ No";
        }
    }
}
