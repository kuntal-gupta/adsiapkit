using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Main editor window.  Open via Tools > AdsIAPKit > Setup Window.
    /// </summary>
    public class AdsIAPKitWindow : EditorWindow
    {
        // ── Menu ───────────────────────────────────────────────────────────────
        [MenuItem("Tools/AdsIAPKit/Setup Window", priority = 1)]
        public static void Open()
        {
            var win = GetWindow<AdsIAPKitWindow>("AdsIAPKit");
            win.minSize = new Vector2(480, 560);
            win.Show();
        }

        [MenuItem("Tools/AdsIAPKit/Quick Select Settings", priority = 2)]
        public static void SelectSettings()
        {
            var s = LoadOrCreateSettings();
            if (s != null) Selection.activeObject = s;
        }

        // ── Constants ──────────────────────────────────────────────────────────
        private const string SettingsPath    = "Assets/Resources/AdsIAPKit/AdsIAPSettings.asset";
        private const string AdsConfigPath   = "Assets/Resources/AdsIAPKit/AdsConfig.asset";
        private const string IAPConfigPath   = "Assets/Resources/AdsIAPKit/IAPConfig.asset";

        internal const string GuideUrl = "https://github.com/kuntal-gupta/adsiapkit/blob/main/Documentation~/SetupGuide.md";

        // ── State ──────────────────────────────────────────────────────────────
        private AdsIAPSettings _settings;
        private int            _tabIndex;
        private BaseTab[]      _tabs;
        private GUIStyle       _tabButtonStyle;
        private bool           _stylesInit;

        private readonly string[] _tabLabels = { "📺  Ads", "💳  IAP", "⚙️  Settings" };

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void OnEnable()
        {
            _tabs = new BaseTab[]
            {
                new AdsTab(),
                new IAPTab(),
                new SettingsTab()
            };

            _settings = LoadOrCreateSettings();
            foreach (var tab in _tabs)
                tab.OnEnable(_settings);
        }

        // Keeps the install progress and package status current while the window is open.
        private void OnInspectorUpdate()
        {
            if (KitPackageInstaller.IsBusy) Repaint();
        }

        private void OnFocus()
        {
            // Re-sync in case assets were changed externally
            _settings = LoadOrCreateSettings();
            foreach (var tab in _tabs)
                tab.OnEnable(_settings);
        }

        // ── Drawing ────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();

            DrawTopBar();
            DrawTabBar();

            if (_settings == null)
            {
                DrawNoSettings();
                return;
            }

            DrawSDKStatus();

            GUILayout.Space(6);

            // Tab content
            using (new EditorGUILayout.VerticalScope())
            {
                _tabs[_tabIndex].Draw();
            }

            // Auto-save
            if (GUI.changed)
            {
                if (_settings != null)           EditorUtility.SetDirty(_settings);
                if (_settings?.adsConfig != null) EditorUtility.SetDirty(_settings.adsConfig);
                if (_settings?.iapConfig != null) EditorUtility.SetDirty(_settings.iapConfig);
                AssetDatabase.SaveAssets();
            }
        }

        // Summary of the packages the current settings need; each tab has the full list.
        private void DrawSDKStatus()
        {
            var result = AdsIAPKitSdkChecker.Check(_settings);
            if (result.IsReady)
            {
                if (result.Installed.Count > 0)
                    EditorGUILayout.HelpBox("All required packages are installed.", MessageType.Info);
                return;
            }

            var names = new List<string>();
            foreach (var package in result.Missing) names.Add(package.DisplayName);
            EditorGUILayout.HelpBox("Your settings need packages that are not installed or enabled:\n• " +
                                    string.Join("\n• ", names), MessageType.Warning);

            var installable = result.Missing.FindAll(p => !KitPackageInstaller.IsInstalled(p.Id));
            if (installable.Count > 0)
            {
                using (new EditorGUI.DisabledScope(KitPackageInstaller.IsBusy))
                    if (GUILayout.Button("Install Missing Packages", GUILayout.Height(26)))
                        KitPackageInstaller.Install(installable);
            }
            KitPackageGUI.DrawBusy();
        }

        // ── Top Bar ────────────────────────────────────────────────────────────
        private void DrawTopBar()
        {
            var barColor = EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.13f, 0.13f)
                : new Color(0.22f, 0.22f, 0.25f);

            var rect = new Rect(0, 0, position.width, 48);
            EditorGUI.DrawRect(rect, barColor);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 16,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            var subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            subStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();
            GUILayout.Label("📱 AdsIAPKit", titleStyle, GUILayout.Height(24));
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Status pill
            if (_settings != null)
            {
                bool hasProvider = _settings.adsConfig != null &&
                                   _settings.adsConfig.activeProvider != AdsProviderType.None;
                bool hasIAP      = _settings.iapConfig != null && _settings.iapConfig.enableIAP;

                var pillStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                pillStyle.normal.textColor = hasProvider ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.7f, 0.2f);

                string providerName = hasProvider
                    ? _settings.adsConfig.activeProvider.ToString()
                    : "No Provider";

                GUILayout.Label($"Ads: {providerName}  |  IAP: {(hasIAP ? "ON" : "OFF")}",
                    pillStyle, GUILayout.Height(48));
            }

            GUILayout.Space(8);
            if (GUILayout.Button("📖 Guide", GUILayout.Width(70), GUILayout.Height(22)))
                Application.OpenURL(GuideUrl);

            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab Bar ────────────────────────────────────────────────────────────
        private void DrawTabBar()
        {
            var barRect = new Rect(0, 48, position.width, 32);
            EditorGUI.DrawRect(barRect, EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.28f, 0.28f, 0.32f));

            GUILayout.Space(48); // push below top bar

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);

            for (int i = 0; i < _tabLabels.Length; i++)
            {
                bool isActive = _tabIndex == i;
                var style = isActive ? ActiveTabStyle() : InactiveTabStyle();

                if (GUILayout.Button(_tabLabels[i], style, GUILayout.Height(30)))
                    _tabIndex = i;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        // ── No Settings Screen ─────────────────────────────────────────────────
        private void DrawNoSettings()
        {
            GUILayout.Space(20);
            EditorGUILayout.HelpBox(
                "No AdsIAPSettings asset found.\nClick the button below to create all required assets.",
                MessageType.Info);
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Create Settings Assets", GUILayout.Width(200), GUILayout.Height(36)))
                {
                    _settings = LoadOrCreateSettings();
                    foreach (var tab in _tabs) tab.OnEnable(_settings);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
            }
        }

        // ── Asset creation helpers ─────────────────────────────────────────────
        private static AdsIAPSettings LoadOrCreateSettings()
        {
            const string resPath = "AdsIAPKit/AdsIAPSettings";
            var s = Resources.Load<AdsIAPSettings>(resPath);
            if (s != null) return s;

            // Not found — create the whole hierarchy
            EnsureDir("Assets/Resources/AdsIAPKit");

            var adsConfig = CreateAsset<AdsConfig>(AdsConfigPath);
            var iapConfig = CreateAsset<IAPConfig>(IAPConfigPath);
            var settings  = CreateAsset<AdsIAPSettings>(SettingsPath);

            settings.adsConfig = adsConfig;
            settings.iapConfig = iapConfig;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AdsIAPKit] Created settings assets at " + SettingsPath);
            return settings;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        // ── Styles ─────────────────────────────────────────────────────────────
        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;
        }

        private static GUIStyle ActiveTabStyle()
        {
            var s = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 30
            };
            s.normal.textColor   = Color.white;
            s.active.textColor   = Color.white;
            var bg = MakeTex(2, 2, EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.45f, 0.75f)
                : new Color(0.25f, 0.45f, 0.75f));
            s.normal.background  = bg;
            s.active.background  = bg;
            return s;
        }

        private static GUIStyle InactiveTabStyle()
        {
            var s = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize    = 12,
                alignment   = TextAnchor.MiddleCenter,
                fixedHeight = 30
            };
            s.normal.textColor  = new Color(0.8f, 0.8f, 0.8f);
            s.hover.textColor   = Color.white;
            return s;
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
    }
}
