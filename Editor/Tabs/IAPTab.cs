using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private string    _search = "";
        private bool      _issuesFold = true;
        private List<IAPIssue> _issues = new List<IAPIssue>();

        // Expand/collapse per product
        private readonly List<bool> _folds = new List<bool>();

        // List changes are applied after drawing so the layout stays consistent within a frame.
        private Action _deferred;

        public override void OnEnable(AdsIAPSettings settings)
        {
            base.OnEnable(settings);
            _cfg = settings?.iapConfig;
            RefreshFolds();
        }

        private void RefreshFolds()
        {
            if (_cfg == null) return;
            while (_folds.Count < _cfg.products.Count) _folds.Add(false);
            while (_folds.Count > _cfg.products.Count) _folds.RemoveAt(_folds.Count - 1);
        }

        public override void Draw()
        {
            if (_cfg == null)
            {
                EditorGUILayout.HelpBox("No IAPConfig assigned. Create one via Assets > Create > AdsIAPKit > IAP Config, then drag it into the master Settings.", MessageType.Warning);
                return;
            }

            // Unity records an undo step only if something actually changes this frame.
            Undo.RecordObject(_cfg, "Edit IAP Catalog");
            RefreshFolds();
            _issues = _cfg.Validate();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawGeneralSettings();
            if (_cfg.enableIAP)
            {
                DrawSeparator(2);
                DrawValidationSummary();
                DrawProductList();
                DrawSeparator(2);
                DrawShopSection();
                DrawSeparator(2);
                DrawTools();
            }

            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();

            if (_deferred != null)
            {
                var action = _deferred;
                _deferred = null;
                action();
                RefreshFolds();
                MarkDirty();
                GUI.changed = true;
            }
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

            GUILayout.Space(4);
            KitPackageGUI.DrawPanel("📦  Required for in-app purchases", new[] { KitPackages.UnityIap });

            DrawTestPurchases();
        }

        private static void DrawTestPurchases()
        {
            GUILayout.Space(4);
            var autoApprove = IAPTestPurchases.AutoApprove;
            EditorGUI.BeginChangeCheck();
            autoApprove = EditorGUILayout.Toggle(new GUIContent("Auto-Approve Test Purchases",
                "Editor only. Unity IAP's fake store approves every purchase without its Buy/Cancel dialog. " +
                "Use it when that dialog cannot be clicked, e.g. in the Device Simulator. Sets the " +
                IAPTestPurchases.Define + " define; device builds use the real store either way."), autoApprove);
            if (EditorGUI.EndChangeCheck())
            {
                if (EditorApplication.isPlaying)
                    EditorUtility.DisplayDialog("Auto-Approve Test Purchases", "Stop Play mode first; changing this recompiles scripts.", "OK");
                else
                    IAPTestPurchases.AutoApprove = autoApprove;
            }
            GUILayout.Label(autoApprove
                    ? "Editor purchases succeed at once. Turn this off to test Cancel in the fake store dialog."
                    : "Editor purchases show the fake store's Buy/Cancel dialog (not clickable in the Device Simulator).",
                EditorStyles.wordWrappedMiniLabel);
        }

        // ── Validation ─────────────────────────────────────────────────────────
        private void DrawValidationSummary()
        {
            var errors   = _issues.Count(i => i.Severity == IAPIssueSeverity.Error);
            var warnings = _issues.Count(i => i.Severity == IAPIssueSeverity.Warning);
            var notes    = _issues.Count - errors - warnings;
            if (_issues.Count == 0)
            {
                if (_cfg.products.Count > 0)
                    EditorGUILayout.HelpBox($"Catalog OK: {_cfg.products.Count} product(s), no problems found.", MessageType.Info);
                return;
            }

            var type = errors > 0 ? MessageType.Error : MessageType.Warning;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _issuesFold = EditorGUILayout.Foldout(_issuesFold,
                    notes > 0
                        ? $"Catalog check: {errors} error(s), {warnings} warning(s), {notes} note(s)"
                        : $"Catalog check: {errors} error(s), {warnings} warning(s)", true);
                if (!_issuesFold) return;

                foreach (var issue in _issues)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var product = _cfg.products[issue.ProductIndex];
                        var name = string.IsNullOrEmpty(product.displayName) ? product.id : product.displayName;
                        EditorGUILayout.HelpBox($"{name}: {issue.Message}", ToMessageType(issue.Severity));
                        if (GUILayout.Button("Show", GUILayout.Width(46), GUILayout.Height(38)))
                        {
                            _search = "";
                            _folds[issue.ProductIndex] = true;
                        }
                    }
                }
                if (type == MessageType.Error)
                    GUILayout.Label("Errors stop products from working in the stores. Fix them before you build.",
                        EditorStyles.wordWrappedMiniLabel);
            }
        }

        // ── Product List ───────────────────────────────────────────────────────
        private void DrawProductList()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"🛒  Products ({_cfg.products.Count})", SectionStyle());
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.DropdownButton(new GUIContent("+ Add Product"), FocusType.Passive,
                    GUILayout.Width(110), GUILayout.Height(22)))
                ShowAddMenu();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Expand", EditorStyles.miniButtonLeft, GUILayout.Width(56)))
                    for (int i = 0; i < _folds.Count; i++) _folds[i] = true;
                if (GUILayout.Button("Collapse", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                    for (int i = 0; i < _folds.Count; i++) _folds[i] = false;
            }
            GUILayout.Space(6);

            if (_cfg.products.Count == 0)
            {
                EditorGUILayout.HelpBox("No products yet. Use '+ Add Product' and pick a template, or 'Add Sample Products' below.",
                    MessageType.Info);
                return;
            }

            for (int i = 0; i < _cfg.products.Count; i++)
                if (MatchesSearch(_cfg.products[i]))
                    DrawProductCard(i);
        }

        private bool MatchesSearch(IAPProduct p)
        {
            if (string.IsNullOrWhiteSpace(_search)) return true;
            var term = _search.Trim();
            return Contains(p.id, term) || Contains(p.displayName, term) || Contains(p.category, term) ||
                   Contains(p.GetAndroidId(), term) || Contains(p.GetIOSId(), term);
        }

        private static MessageType ToMessageType(IAPIssueSeverity severity) =>
            severity == IAPIssueSeverity.Error   ? MessageType.Error :
            severity == IAPIssueSeverity.Warning ? MessageType.Warning : MessageType.Info;

        private static bool Contains(string text, string term) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawProductCard(int index)
        {
            var p = _cfg.products[index];
            var productIssues = _issues.Where(i => i.ProductIndex == index).ToList();
            var hasError = productIssues.Any(i => i.Severity == IAPIssueSeverity.Error);
            var hasWarning = productIssues.Any(i => i.Severity == IAPIssueSeverity.Warning);

            var cardColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.88f, 0.88f, 0.88f);
            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(rect.x - 4, rect.y - 2, rect.width + 8, rect.height + 4), cardColor);
            if (!p.isEnabled) GUI.color = new Color(1f, 1f, 1f, 0.55f);

            // Header
            EditorGUILayout.BeginHorizontal();
            _folds[index] = EditorGUILayout.Foldout(_folds[index], "", true);
            var foldRect = GUILayoutUtility.GetLastRect();
            var title = string.IsNullOrEmpty(p.displayName) ? p.id : p.displayName;
            if (hasError || hasWarning) title = (hasError ? "⛔ " : "⚠ ") + title;
            GUI.Label(new Rect(foldRect.x + 14, foldRect.y, 220, foldRect.height), title,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            p.isEnabled = EditorGUILayout.Toggle(new GUIContent("", "Enabled"), p.isEnabled, GUILayout.Width(18));
            if (EditorGUI.EndChangeCheck()) MarkDirty();

            DrawBadge(TypeLabel(p.productType), TypeColor(p.productType));
            if (!string.IsNullOrEmpty(p.displayPrice))
                GUILayout.Label(p.displayPrice,
                    new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter },
                    GUILayout.Width(52));

            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button(new GUIContent("▲", "Move up (shop order)"), EditorStyles.miniButtonLeft, GUILayout.Width(22)))
                    _deferred = () => Swap(index, index - 1);
            using (new EditorGUI.DisabledScope(index == _cfg.products.Count - 1))
                if (GUILayout.Button(new GUIContent("▼", "Move down (shop order)"), EditorStyles.miniButtonMid, GUILayout.Width(22)))
                    _deferred = () => Swap(index, index + 1);
            if (GUILayout.Button(new GUIContent("⧉", "Duplicate"), EditorStyles.miniButtonMid, GUILayout.Width(22)))
                _deferred = () => Duplicate(index);
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button(new GUIContent("✕", "Remove"), EditorStyles.miniButtonRight, GUILayout.Width(22)) &&
                EditorUtility.DisplayDialog("Remove Product", $"Remove '{title}'?", "Remove", "Cancel"))
                _deferred = () => { _cfg.products.RemoveAt(index); _folds.RemoveAt(index); };
            GUI.backgroundColor = bg;
            EditorGUILayout.EndHorizontal();

            if (_folds[index]) DrawProductBody(p, productIssues);

            GUI.color = Color.white;
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawProductBody(IAPProduct p, List<IAPIssue> productIssues)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            foreach (var issue in productIssues)
                EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));

            // Identity
            GUILayout.Space(4);
            GUILayout.Label("Identity", SectionStyle());
            using (new EditorGUILayout.HorizontalScope())
            {
                p.id = EditorGUILayout.TextField(new GUIContent("Product ID",
                    "Your ID for this product. Code, the shop and the purchase handler use it."), p.id);
                if (GUILayout.Button(new GUIContent("Suggest", "Build an ID from the app's bundle identifier"),
                        EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    var key = string.IsNullOrEmpty(p.displayName) ? "product" : p.displayName;
                    p.id = UniqueId(BundlePrefix() + "." + Slug(key), p);
                    GUI.FocusControl(null);
                }
            }
            p.displayName = EditorGUILayout.TextField(new GUIContent("Display Name", "Title on the shop card"), p.displayName);
            EditorGUILayout.LabelField("Description", EditorStyles.miniBoldLabel);
            p.description = EditorGUILayout.TextArea(p.description, GUILayout.Height(34));

            // Store
            DrawSeparator(0.5f, 4f);
            GUILayout.Label("Store", SectionStyle());
            p.productType = (IAPProductType)EditorGUILayout.EnumPopup(new GUIContent("Type", TypeHelp), p.productType);
            p.androidId = EditorGUILayout.TextField(new GUIContent("Google Play ID", "Blank = use the Product ID"), p.androidId);
            p.iosId     = EditorGUILayout.TextField(new GUIContent("App Store ID",   "Blank = use the Product ID"), p.iosId);
            EditorGUILayout.LabelField(" ", $"Play: {p.GetAndroidId()}   ·   App Store: {p.GetIOSId()}", EditorStyles.miniLabel);
            p.displayPrice = EditorGUILayout.TextField(new GUIContent("Fallback Price",
                "Shown in the Editor only. On a device the shop shows the store's localized price."), p.displayPrice);

            // Reward
            DrawSeparator(0.5f, 4f);
            GUILayout.Label("Reward", SectionStyle());
            p.rewardType = (RewardType)EditorGUILayout.EnumPopup(new GUIContent("Reward Type",
                "What your purchase handler grants. The kit passes it the Product ID; read this from the catalog."), p.rewardType);
            switch (p.rewardType)
            {
                case RewardType.Currency:
                case RewardType.Item:
                    p.rewardId     = EditorGUILayout.TextField(new GUIContent(p.rewardType == RewardType.Currency ? "Currency ID" : "Item ID",
                        "e.g. coins, gems, hint"), p.rewardId);
                    p.rewardAmount = EditorGUILayout.IntField("Amount", p.rewardAmount);
                    DrawBonusAmount(p);
                    p.alsoRemovesAds = EditorGUILayout.Toggle(new GUIContent("Also Removes Ads", AlsoRemovesAdsHelp), p.alsoRemovesAds);
                    break;
                case RewardType.Feature:
                    p.rewardId = EditorGUILayout.TextField(new GUIContent("Feature ID", "e.g. premium, all_levels"), p.rewardId);
                    p.alsoRemovesAds = EditorGUILayout.Toggle(new GUIContent("Also Removes Ads", AlsoRemovesAdsHelp), p.alsoRemovesAds);
                    break;
                case RewardType.RemoveAds:
                    EditorGUILayout.LabelField(" ", "Your handler should save it and call AdsManager.Instance.DestroyBanner().",
                        EditorStyles.wordWrappedMiniLabel);
                    break;
                case RewardType.Custom:
                    p.rewardId     = EditorGUILayout.TextField(new GUIContent("Reward ID", "Any key your handler understands"), p.rewardId);
                    p.rewardAmount = EditorGUILayout.IntField("Amount", p.rewardAmount);
                    DrawBonusAmount(p);
                    p.alsoRemovesAds = EditorGUILayout.Toggle(new GUIContent("Also Removes Ads", AlsoRemovesAdsHelp), p.alsoRemovesAds);
                    break;
            }

            // Shop
            DrawSeparator(0.5f, 4f);
            GUILayout.Label("Shop", SectionStyle());
            p.icon       = (Sprite)EditorGUILayout.ObjectField("Icon", p.icon, typeof(Sprite), false, GUILayout.Height(18));
            p.category   = EditorGUILayout.TextField(new GUIContent("Category", "Shop tab, e.g. Coins or Bundles. Blank = no category."), p.category);
            p.isFeatured = EditorGUILayout.Toggle(new GUIContent("Featured", "Highlighted card; shows BEST VALUE unless a badge is set"), p.isFeatured);
            p.badge      = EditorGUILayout.TextField(new GUIContent("Badge", "Corner ribbon text, e.g. 20% MORE or POPULAR"), p.badge);
            p.sticker    = EditorGUILayout.TextField(new GUIContent("Sticker", "Top-left sticker, e.g. HOT or NEW"), p.sticker);
            p.showTitle  = EditorGUILayout.Toggle(new GUIContent("Show Title", "Header strip with the display name above the row, e.g. for bundles"), p.showTitle);
            DrawBonusIcons(p);
            p.shopCard   = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Card Override",
                "Optional card prefab with a Shop Product View for this product only, e.g. a big special-offer card. Empty = the shop's Card Template."),
                p.shopCard, typeof(GameObject), false);
            if (p.shopCard != null && p.shopCard.GetComponent("ShopProductView") == null)
                EditorGUILayout.HelpBox("This prefab has no Shop Product View component, so the shop uses the Card Template instead.", MessageType.Warning);
            p.hideInShop = EditorGUILayout.Toggle(new GUIContent("Hide In Shop", "Purchasable from code but not listed by ShopCatalogView"), p.hideInShop);

            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUI.indentLevel--;
            GUILayout.Space(4);
        }

        private static void DrawBonusIcons(IAPProduct p)
        {
            p.bonusIcons ??= new List<Sprite>();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bonus Icons", "Shown after the amount as \"+ icon\", e.g. a no-ads icon. Display only."));
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24))) p.bonusIcons.Add(null);
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < p.bonusIcons.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                p.bonusIcons[i] = (Sprite)EditorGUILayout.ObjectField(" ", p.bonusIcons[i], typeof(Sprite), false, GUILayout.Height(18));
                var remove = GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    p.bonusIcons.RemoveAt(i);
                    GUI.changed = true;
                    break;
                }
            }
        }

        private static void DrawBonusAmount(IAPProduct p)
        {
            p.bonusAmount = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Bonus Amount",
                "Granted on top of Amount, e.g. a +500 promotion. Your handler grants GetTotalAmount(). 0 = none."), p.bonusAmount));
            if (p.bonusAmount > 0)
                EditorGUILayout.LabelField(" ", $"Grants {p.GetTotalAmount():N0} in total", EditorStyles.miniLabel);
        }

        private const string AlsoRemovesAdsHelp =
            "This product also turns ads off, e.g. a big coin pack with No Ads. " +
            "Your purchase handler grants it: check product.RemovesAds().";

        private const string TypeHelp =
            "Consumable: can be bought again (coins, hints).\n" +
            "Non-Consumable: bought once, restorable (Remove Ads, unlocks).\n" +
            "Subscription: renews until cancelled (VIP, premium).";

        private static string TypeLabel(IAPProductType type) => type switch
        {
            IAPProductType.NonConsumable => "Non-Cons.",
            IAPProductType.Subscription  => "Subscr.",
            _                            => "Consumable"
        };

        private static Color TypeColor(IAPProductType type) => type switch
        {
            IAPProductType.Consumable    => new Color(0.2f, 0.7f, 0.4f),
            IAPProductType.NonConsumable => new Color(0.3f, 0.5f, 0.9f),
            IAPProductType.Subscription  => new Color(0.9f, 0.6f, 0.1f),
            _                            => Color.gray
        };

        // ── Add / templates ────────────────────────────────────────────────────
        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Coin Pack (Consumable)"), false, () => AddFromTemplate(Template.Coins));
            menu.AddItem(new GUIContent("Remove Ads (Non-Consumable)"), false, () => AddFromTemplate(Template.RemoveAds));
            menu.AddItem(new GUIContent("Unlock Feature (Non-Consumable)"), false, () => AddFromTemplate(Template.Unlock));
            menu.AddItem(new GUIContent("Item Pack (Consumable)"), false, () => AddFromTemplate(Template.Item));
            menu.AddItem(new GUIContent("Subscription"), false, () => AddFromTemplate(Template.Subscription));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Empty Product"), false, () => AddFromTemplate(Template.Empty));
            menu.ShowAsContext();
        }

        private enum Template { Coins, RemoveAds, Unlock, Item, Subscription, Empty }

        private void AddFromTemplate(Template template)
        {
            Undo.RecordObject(_cfg, "Add IAP Product");
            var product = CreateFromTemplate(template);
            product.id = UniqueId(product.id, null);
            _cfg.products.Add(product);
            RefreshFolds();
            _folds[_folds.Count - 1] = true;
            _search = "";
            MarkDirty();
            EditorUtility.SetDirty(_cfg);
            EditorWindow.focusedWindow?.Repaint();
        }

        private IAPProduct CreateFromTemplate(Template template)
        {
            var prefix = BundlePrefix();
            switch (template)
            {
                case Template.Coins:
                    return new IAPProduct
                    {
                        id = prefix + ".coins_small", displayName = "Handful of Coins", description = "500 coins to spend in the game.",
                        productType = IAPProductType.Consumable, rewardType = RewardType.Currency, rewardId = "coins",
                        rewardAmount = 500, displayPrice = "$0.99", category = "Coins"
                    };
                case Template.RemoveAds:
                    return new IAPProduct
                    {
                        id = prefix + ".remove_ads", displayName = "Remove Ads", description = "No more banner and interstitial ads.",
                        productType = IAPProductType.NonConsumable, rewardType = RewardType.RemoveAds, rewardId = "",
                        rewardAmount = 0, displayPrice = "$2.99", badge = "POPULAR"
                    };
                case Template.Unlock:
                    return new IAPProduct
                    {
                        id = prefix + ".unlock_all", displayName = "Unlock Everything", description = "Every level, unlocked forever.",
                        productType = IAPProductType.NonConsumable, rewardType = RewardType.Feature, rewardId = "unlock_all",
                        rewardAmount = 0, displayPrice = "$4.99"
                    };
                case Template.Item:
                    return new IAPProduct
                    {
                        id = prefix + ".hints_10", displayName = "10 Hints", description = "Ten hints for tricky puzzles.",
                        productType = IAPProductType.Consumable, rewardType = RewardType.Item, rewardId = "hint",
                        rewardAmount = 10, displayPrice = "$1.99", category = "Items"
                    };
                case Template.Subscription:
                    return new IAPProduct
                    {
                        id = prefix + ".vip_monthly", displayName = "VIP Monthly", description = "No ads and double daily rewards.",
                        productType = IAPProductType.Subscription, rewardType = RewardType.Feature, rewardId = "vip",
                        rewardAmount = 0, displayPrice = "$4.99/mo", isFeatured = true
                    };
                default:
                    return new IAPProduct
                    {
                        id = prefix + ".product", displayName = "New Product", description = "",
                        rewardId = "", rewardAmount = 0, displayPrice = ""
                    };
            }
        }

        private void Swap(int a, int b)
        {
            (_cfg.products[a], _cfg.products[b]) = (_cfg.products[b], _cfg.products[a]);
            (_folds[a], _folds[b]) = (_folds[b], _folds[a]);
        }

        private void Duplicate(int index)
        {
            var copy = JsonUtility.FromJson<IAPProduct>(JsonUtility.ToJson(_cfg.products[index]));
            copy.icon = _cfg.products[index].icon;
            copy.bonusIcons = new List<Sprite>(_cfg.products[index].bonusIcons ?? new List<Sprite>());
            copy.shopCard = _cfg.products[index].shopCard;
            copy.id = UniqueId(copy.id + "_copy", null);
            copy.androidId = "";
            copy.iosId = "";
            _cfg.products.Insert(index + 1, copy);
            _folds.Insert(index + 1, true);
        }

        /// <summary>The app's bundle identifier in a form both stores accept.</summary>
        private static string BundlePrefix()
        {
            var bundle = PlayerSettings.applicationIdentifier;
            var slug = Slug(string.IsNullOrEmpty(bundle) ? "com.company.app" : bundle, keepDots: true);
            return string.IsNullOrEmpty(slug) ? "com.company.app" : slug;
        }

        private static string Slug(string text, bool keepDots = false)
        {
            var sb = new StringBuilder();
            foreach (var c in text.Trim().ToLowerInvariant())
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || (keepDots && c == '.')) sb.Append(c);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_', '.');
        }

        private string UniqueId(string id, IAPProduct self)
        {
            var candidate = id;
            for (var n = 2; _cfg.products.Any(p => p != self && p.id == candidate); n++)
                candidate = id + "_" + n;
            return candidate;
        }

        // ── Shop UI ────────────────────────────────────────────────────────────
        private void DrawShopSection()
        {
            GUILayout.Label("🏪  Shop UI", SectionStyle());
            GUILayout.Label(
                "The shop fills itself from this catalog. Add a Shop Catalog to a Canvas: it creates one card per product " +
                "in the order above, shows the store's localized price and handles Buy, Owned and Restore.",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(4);
#if ADSIAPKIT_UGUI
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Shop Catalog", GUILayout.Height(24))) ShopUIBuilder.CreateCatalog();
                if (GUILayout.Button("Create Single Product Card", GUILayout.Height(24))) ShopUIBuilder.CreateCard();
            }
            GUILayout.Label("Also in GameObject → AdsIAPKit. The card is plain uGUI, so restyle it or save it as a prefab.",
                EditorStyles.wordWrappedMiniLabel);
#else
            EditorGUILayout.HelpBox("The shop components need the Unity UI package (com.unity.ugui).", MessageType.Info);
#endif
        }

        // ── Tools ──────────────────────────────────────────────────────────────
        private void DrawTools()
        {
            GUILayout.Label("🔧  Tools", SectionStyle());
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export CSV", GUILayout.Height(24))) ExportCSV();
            if (GUILayout.Button("Import CSV", GUILayout.Height(24))) ImportCSV();
            if (GUILayout.Button("Add Sample Products", GUILayout.Height(24)))
            {
                foreach (var t in new[] { Template.Coins, Template.Item, Template.RemoveAds, Template.Subscription })
                    AddFromTemplate(t);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Store Setup Guide (Google Play & App Store) ↗", GUILayout.Height(22)))
                Application.OpenURL(AdsIAPKitWindow.GuideUrl + "#4-in-app-purchases");
        }

        private static readonly string[] CsvColumns =
        {
            "ID", "DisplayName", "Description", "Type", "GooglePlayID", "AppStoreID", "FallbackPrice",
            "RewardType", "RewardID", "RewardAmount", "BonusAmount", "AlsoRemovesAds", "Category", "Badge", "Sticker", "ShowTitle", "Featured", "Enabled", "HideInShop"
        };

        private void ExportCSV()
        {
            if (_cfg.products.Count == 0) { EditorUtility.DisplayDialog("Export", "No products to export.", "OK"); return; }

            string path = EditorUtility.SaveFilePanel("Export Products", "", "iap_products", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", CsvColumns));
            foreach (var p in _cfg.products)
            {
                var fields = new[]
                {
                    p.id, p.displayName, p.description, p.productType.ToString(), p.androidId, p.iosId, p.displayPrice,
                    p.rewardType.ToString(), p.rewardId, p.rewardAmount.ToString(), p.bonusAmount.ToString(), p.alsoRemovesAds.ToString(), p.category, p.badge,
                    p.sticker, p.showTitle.ToString(), p.isFeatured.ToString(), p.isEnabled.ToString(), p.hideInShop.ToString()
                };
                sb.AppendLine(string.Join(",", fields.Select(CsvEscape)));
            }
            File.WriteAllText(path, sb.ToString());
            EditorUtility.DisplayDialog("Export", $"Exported {_cfg.products.Count} products.", "OK");
        }

        private void ImportCSV()
        {
            string path = EditorUtility.OpenFilePanel("Import Products", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            List<List<string>> rows;
            try { rows = ParseCsv(File.ReadAllText(path)); }
            catch (Exception e) { EditorUtility.DisplayDialog("Import", "Could not read the file: " + e.Message, "OK"); return; }
            if (rows.Count < 2) { EditorUtility.DisplayDialog("Import", "The file has no product rows.", "OK"); return; }

            var header = rows[0].Select(h => h.Trim()).ToList();
            int Col(string name) => header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
            if (Col("ID") < 0) { EditorUtility.DisplayDialog("Import", "The first row must contain an ID column.", "OK"); return; }

            Undo.RecordObject(_cfg, "Import IAP Products");
            int added = 0, updated = 0;
            foreach (var row in rows.Skip(1))
            {
                string Get(string name) { var c = Col(name); return c >= 0 && c < row.Count ? row[c] : null; }
                var id = Get("ID")?.Trim();
                if (string.IsNullOrEmpty(id)) continue;

                var p = _cfg.GetProduct(id);
                if (p == null) { p = new IAPProduct { id = id }; _cfg.products.Add(p); added++; }
                else updated++;

                p.displayName  = Get("DisplayName")   ?? p.displayName;
                p.description  = Get("Description")   ?? p.description;
                p.androidId    = Get("GooglePlayID")  ?? Get("AndroidID") ?? p.androidId;
                p.iosId        = Get("AppStoreID")    ?? Get("iOSID") ?? p.iosId;
                p.displayPrice = Get("FallbackPrice") ?? Get("DisplayPrice") ?? p.displayPrice;
                p.rewardId     = Get("RewardID")      ?? p.rewardId;
                p.category     = Get("Category")      ?? p.category;
                p.badge        = Get("Badge")         ?? p.badge;
                p.sticker      = Get("Sticker")       ?? p.sticker;
                if (Enum.TryParse(Get("Type"), true, out IAPProductType type)) p.productType = type;
                if (Enum.TryParse(Get("RewardType"), true, out RewardType reward)) p.rewardType = reward;
                if (int.TryParse(Get("RewardAmount"), out var amount)) p.rewardAmount = amount;
                if (int.TryParse(Get("BonusAmount"), out var bonus)) p.bonusAmount = Math.Max(0, bonus);
                if (bool.TryParse(Get("ShowTitle"), out var showTitle)) p.showTitle = showTitle;
                if (bool.TryParse(Get("Featured"), out var featured)) p.isFeatured = featured;
                if (bool.TryParse(Get("Enabled"), out var enabled)) p.isEnabled = enabled;
                if (bool.TryParse(Get("HideInShop"), out var hidden)) p.hideInShop = hidden;
                if (bool.TryParse(Get("AlsoRemovesAds"), out var noAds)) p.alsoRemovesAds = noAds;
            }

            RefreshFolds();
            MarkDirty();
            EditorUtility.SetDirty(_cfg);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Import", $"Added {added} and updated {updated} product(s).", "OK");
        }

        private static string CsvEscape(string value)
        {
            value ??= "";
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        /// <summary>RFC 4180 CSV: quoted fields, doubled quotes and line breaks inside quotes.</summary>
        internal static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else field.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString()); field.Clear();
                    if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
                    row = new List<string>();
                }
                else field.Append(c);
            }
            row.Add(field.ToString());
            if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
            return rows;
        }
    }
}
