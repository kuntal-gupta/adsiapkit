using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    public class IAPTab : BaseTab
    {
        public override string TabName => "IAP";
        public override string TabIcon => "💳";

        private IAPConfig _cfg;
        private Vector2   _scroll;
        private int       _selectedProduct = -1;

        // Expand/collapse all products
        private List<bool> _folds = new List<bool>();

        public override void OnEnable(AdsIAPSettings settings)
        {
            base.OnEnable(settings);
            _cfg = settings?.iapConfig;
            RefreshFolds();
        }

        private void RefreshFolds()
        {
            if (_cfg == null) return;
            while (_folds.Count < _cfg.products.Count) _folds.Add(true);
            while (_folds.Count > _cfg.products.Count) _folds.RemoveAt(_folds.Count - 1);
        }

        public override void Draw()
        {
            if (_cfg == null)
            {
                EditorGUILayout.HelpBox("No IAPConfig assigned. Create one via Assets > Create > AdsIAPKit > IAP Config, then drag it into the master Settings.", MessageType.Warning);
                return;
            }

            RefreshFolds();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawGeneralSettings();
            DrawSeparator(2);
            DrawProductList();
            DrawSeparator(2);
            DrawBulkActions();

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        // ── General ────────────────────────────────────────────────────────────
        private void DrawGeneralSettings()
        {
            GUILayout.Label("⚙️  IAP General", SectionStyle());
            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            _cfg.enableIAP = EditorGUILayout.Toggle(
                new GUIContent("Enable IAP", "Master switch for all in-app purchases"), _cfg.enableIAP);


            if (EditorGUI.EndChangeCheck()) MarkDirty();

            if (!_cfg.enableIAP)
            {
                EditorGUILayout.HelpBox("IAP is disabled. Enable it above.", MessageType.Info);
                return;
            }

            // SDK check
            #if !UNITY_IAP_ENABLED
            EditorGUILayout.HelpBox(
                "Unity IAP package not found.\n" +
                "Go to Window > Package Manager, find 'In App Purchasing' and install it.",
                MessageType.Warning);
            if (GUILayout.Button("Open Package Manager", GUILayout.Height(26)))
                EditorApplication.ExecuteMenuItem("Window/Package Manager");
            #endif
        }

        // ── Product List ───────────────────────────────────────────────────────
        private void DrawProductList()
        {
            // Header row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"🛒  Products ({_cfg.products.Count})", SectionStyle());
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Product", GUILayout.Width(110), GUILayout.Height(22)))
            {
                _cfg.products.Add(new IAPProduct
                {
                    id          = $"com.company.app.product{_cfg.products.Count + 1}",
                    displayName = $"Product {_cfg.products.Count + 1}"
                });
                _folds.Add(true);
                _selectedProduct = _cfg.products.Count - 1;
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (_cfg.products.Count == 0)
            {
                EditorGUILayout.HelpBox("No products defined. Click '+ Add Product' to create your first IAP.", MessageType.Info);
                return;
            }

            for (int i = 0; i < _cfg.products.Count; i++)
                DrawProductCard(i);
        }

        private void DrawProductCard(int index)
        {
            var p = _cfg.products[index];

            // Card background
            var cardColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.88f, 0.88f, 0.88f);
            if (_selectedProduct == index)
                cardColor = EditorGUIUtility.isProSkin
                    ? new Color(0.2f, 0.28f, 0.38f)
                    : new Color(0.75f, 0.85f, 1f);

            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(rect.x - 4, rect.y - 2, rect.width + 8, rect.height + 4), cardColor);

            // Header bar
            EditorGUILayout.BeginHorizontal();
            _folds[index] = EditorGUILayout.Foldout(_folds[index], "", true);
            var lastRect = GUILayoutUtility.GetLastRect();

            // Product type badge
            var typeColor = p.productType switch
            {
                IAPProductType.Consumable    => new Color(0.2f, 0.7f, 0.4f),
                IAPProductType.NonConsumable => new Color(0.3f, 0.5f, 0.9f),
                IAPProductType.Subscription  => new Color(0.9f, 0.6f, 0.1f),
                _                           => Color.gray
            };

            GUI.Label(new Rect(lastRect.x + 14, lastRect.y, 200, lastRect.height),
                new GUIContent(string.IsNullOrEmpty(p.displayName) ? p.id : p.displayName),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

            GUILayout.FlexibleSpace();

            // Enabled toggle
            EditorGUI.BeginChangeCheck();
            p.isEnabled = EditorGUILayout.Toggle(p.isEnabled, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            // Type badge
            DrawBadge(p.productType.ToString(), typeColor);

            // Price badge
            if (!string.IsNullOrEmpty(p.displayPrice))
                GUILayout.Label(p.displayPrice,
                    new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter },
                    GUILayout.Width(45));

            // Delete button
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Remove Product",
                    $"Remove '{p.displayName}'?", "Remove", "Cancel"))
                {
                    _cfg.products.RemoveAt(index);
                    _folds.RemoveAt(index);
                    if (_selectedProduct >= _cfg.products.Count)
                        _selectedProduct = _cfg.products.Count - 1;
                    MarkDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Expanded body
            if (_folds[index])
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                GUILayout.Space(4);
                GUILayout.Label("Identity", SectionStyle());
                p.id          = EditorGUILayout.TextField(new GUIContent("Product ID", "Unique reverse-domain ID"), p.id);
                p.displayName = EditorGUILayout.TextField("Display Name", p.displayName);
                p.description = EditorGUILayout.TextArea(p.description, GUILayout.Height(40));

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Type & Platform IDs", SectionStyle());
                p.productType = (IAPProductType)EditorGUILayout.EnumPopup("Type", p.productType);

                EditorGUILayout.HelpBox("Leave platform IDs empty to use the Product ID for both stores.", MessageType.None);
                p.androidId = EditorGUILayout.TextField(new GUIContent("Android ID", "Google Play Store product ID (blank = use Product ID)"), p.androidId);
                p.iosId     = EditorGUILayout.TextField(new GUIContent("iOS ID",     "App Store product ID (blank = use Product ID)"),     p.iosId);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Pricing & UI", SectionStyle());
                p.displayPrice = EditorGUILayout.TextField(new GUIContent("Display Price", "For UI only — actual price comes from the store"), p.displayPrice);
                p.icon         = (Sprite)EditorGUILayout.ObjectField("Icon", p.icon, typeof(Sprite), false);
                p.isFeatured   = EditorGUILayout.Toggle("Featured / Best Value", p.isFeatured);

                DrawSeparator(0.5f, 4f);
                GUILayout.Label("Reward", SectionStyle());
                p.rewardType   = (RewardType)EditorGUILayout.EnumPopup("Reward Type",   p.rewardType);
                p.rewardId     = EditorGUILayout.TextField("Reward ID",     p.rewardId);
                p.rewardAmount = EditorGUILayout.IntField("Reward Amount", p.rewardAmount);

                if (EditorGUI.EndChangeCheck()) MarkDirty();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
        }

        // ── Bulk actions ───────────────────────────────────────────────────────
        private void DrawBulkActions()
        {
            GUILayout.Label("🔧  Tools", SectionStyle());
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All",  GUILayout.Height(24)))
                for (int i = 0; i < _folds.Count; i++) _folds[i] = true;
            if (GUILayout.Button("Collapse All", GUILayout.Height(24)))
                for (int i = 0; i < _folds.Count; i++) _folds[i] = false;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Product List (CSV)", GUILayout.Height(24)))
                ExportCSV();
            if (GUILayout.Button("Add Sample Products", GUILayout.Height(24)))
                AddSampleProducts();
            EditorGUILayout.EndHorizontal();
        }

        private void ExportCSV()
        {
            if (_cfg.products.Count == 0) { EditorUtility.DisplayDialog("Export", "No products to export.", "OK"); return; }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,DisplayName,Type,AndroidID,iOSID,DisplayPrice,RewardType,RewardAmount");
            foreach (var p in _cfg.products)
                sb.AppendLine($"{p.id},{p.displayName},{p.productType},{p.GetAndroidId()},{p.GetIOSId()},{p.displayPrice},{p.rewardType},{p.rewardAmount}");

            string path = EditorUtility.SaveFilePanel("Export Products", "", "iap_products", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export", $"Exported {_cfg.products.Count} products.", "OK");
            }
        }

        private void AddSampleProducts()
        {
            _cfg.products.Add(new IAPProduct
            {
                id          = "com.company.app.coins_small",
                displayName = "100 Coins",
                description = "A small pack of coins.",
                productType = IAPProductType.Consumable,
                displayPrice = "$0.99",
                rewardType  = RewardType.Currency,
                rewardId    = "coins",
                rewardAmount = 100
            });
            _cfg.products.Add(new IAPProduct
            {
                id           = "com.company.app.remove_ads",
                displayName  = "Remove Ads",
                description  = "Permanently remove all ads.",
                productType  = IAPProductType.NonConsumable,
                displayPrice = "$1.99",
                rewardType   = RewardType.RemoveAds
            });
            _cfg.products.Add(new IAPProduct
            {
                id           = "com.company.app.premium_monthly",
                displayName  = "Premium Monthly",
                description  = "Access all premium features for one month.",
                productType  = IAPProductType.Subscription,
                displayPrice = "$4.99/mo",
                rewardType   = RewardType.Feature,
                rewardId     = "premium",
                isFeatured   = true
            });
            RefreshFolds();
            MarkDirty();
        }
    }
}
