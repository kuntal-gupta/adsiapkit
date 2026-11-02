#if ADSIAPKIT_UGUI
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Builds a ready-to-use uGUI shop in the open scene: GameObject > AdsIAPKit.
    /// Everything is plain uGUI, so restyle it freely or save it as a prefab.
    /// </summary>
    internal static class ShopUIBuilder
    {
        // Warm mobile-game palette; swap the sprites and colors for your game's art.
        private static readonly Color PanelColor  = new Color(0.42f, 0.26f, 0.15f, 1f);
        private static readonly Color CardColor   = new Color(1.00f, 0.94f, 0.82f);
        private static readonly Color StripColor  = new Color(0.95f, 0.85f, 0.68f);
        private static readonly Color PillColor   = new Color(0.55f, 0.38f, 0.22f, 0.25f);
        private static readonly Color TextColor   = new Color(0.45f, 0.25f, 0.12f);
        private static readonly Color AmountColor = new Color(0.80f, 0.30f, 0.08f);
        private static readonly Color BuyColor    = new Color(0.25f, 0.72f, 0.15f);
        private static readonly Color PlusColor   = new Color(0.30f, 0.70f, 0.15f);
        private static readonly Color RibbonColor = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color StickerColor = new Color(1.00f, 0.60f, 0.10f);

        private const float RowHeight = 120;

        [MenuItem("GameObject/AdsIAPKit/Shop Catalog", false, 10)]
        public static void CreateCatalog()
        {
            var parent = FindOrCreateCanvas();

            // Full-screen panel: header with title and close button, scrolling list of rows, restore link.
            var shop = NewUI("Shop", parent);
            Stretch(shop);
            AddImage(shop, PanelColor);
            var layout = shop.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 32, 24);
            layout.spacing = 16;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = NewUI("Header", shop);
            Size(header.gameObject, height: 110);
            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = headerLayout.childForceExpandHeight = false;
            Size(NewUI("Spacer", header).gameObject, width: 96);
            var title = CreateText(header, "Title", "SHOP", 64, true, Color.white);
            Size(title.gameObject, flexibleWidth: 1);
            var close = CreateButton(header, "Close Button", "X", new Color(1, 1, 1, 0.15f), Color.white, 48);
            Size(close.gameObject, width: 96, height: 96);
            UnityEventTools.AddBoolPersistentListener(close.onClick, shop.gameObject.SetActive, false);

            // Scroll view with a vertical list that grows with the number of products.
            var scroll = NewUI("Scroll View", shop);
            Size(scroll.gameObject, flexibleHeight: 1);
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = NewUI("Viewport", scroll);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot     = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(4, 4, 12, 12);
            list.spacing = 16;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content  = content;

            var template = BuildCard(content);
            template.name = "Card Template";

            var restore = CreateButton(shop, "Restore Button", "Restore", new Color(1, 1, 1, 0), Color.white, 36);
            Underline(FindText(restore.transform));
            Size(restore.gameObject, height: 72);

            var view = shop.gameObject.AddComponent<ShopCatalogView>();
            var so = new SerializedObject(view);
            so.FindProperty("cardTemplate").objectReferenceValue  = template;
            so.FindProperty("content").objectReferenceValue       = content;
            so.FindProperty("restoreButton").objectReferenceValue = restore;
            so.ApplyModifiedPropertiesWithoutUndo();

            Finish(shop.gameObject, "Create Shop Catalog");
            Debug.Log("[AdsIAPKit] Shop created. It lists every enabled product in the IAP catalog, one per row, when you press Play.");
        }

        [MenuItem("GameObject/AdsIAPKit/Shop Product Card", false, 11)]
        public static void CreateCard()
        {
            var parent = Selection.activeTransform is RectTransform selected && selected.GetComponentInParent<Canvas>() != null
                ? selected
                : FindOrCreateCanvas();

            var card = BuildCard(parent);
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(1000, RowHeight + 40);

            var catalog = Resources.Load<AdsIAPSettings>("AdsIAPKit/AdsIAPSettings")?.iapConfig;
            if (catalog != null && catalog.products.Count > 0)
            {
                var so = new SerializedObject(card);
                so.FindProperty("productId").stringValue = catalog.products[0].id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Finish(card.gameObject, "Create Shop Product Card");
        }

        // ── Card: one product per row ──────────────────────────────────────────
        //  [ optional title strip                                    ]
        //  [ icon ][ amount pill ][ + bonus ][ + bonus ]    [  Buy  ]
        //  with a corner ribbon (badge), a top-left sticker and an Owned overlay.
        private static ShopProductView BuildCard(Transform parent)
        {
            var card = NewUI("Shop Product Card", parent);
            AddImage(card, CardColor);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 16, 16);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title strip, shown only for products with Show Title.
            var titleRoot = NewUI("Title", card);
            AddImage(titleRoot, StripColor);
            Size(titleRoot.gameObject, height: 52);
            var title = CreateText(titleRoot, "Text", "Product", 30, true, TextColor);
            Stretch((RectTransform)title.transform);
            titleRoot.gameObject.SetActive(false);

            var row = NewUI("Row", card);
            Size(row.gameObject, height: RowHeight);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

            var icon = NewUI("Icon", row);
            var iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            Size(icon.gameObject, width: 110, height: 110);

            var amount = NewUI("Amount", row);
            var amountBackground = AddImage(amount, PillColor);
            Size(amount.gameObject, width: 240, height: 76);
            var reward = CreateText(amount, "Text", "500", 44, true, AmountColor);
            Stretch((RectTransform)reward.transform);

            // "+ icon" items for bonus icons; the template is copied once per sprite.
            var bonus = NewUI("Bonus", row);
            var bonusLayout = bonus.gameObject.AddComponent<HorizontalLayoutGroup>();
            bonusLayout.spacing = 8;
            bonusLayout.childAlignment = TextAnchor.MiddleLeft;
            bonusLayout.childControlWidth = bonusLayout.childControlHeight = true;
            bonusLayout.childForceExpandWidth = bonusLayout.childForceExpandHeight = false;
            var bonusItem = NewUI("Bonus Item", bonus);
            var bonusItemLayout = bonusItem.gameObject.AddComponent<HorizontalLayoutGroup>();
            bonusItemLayout.spacing = 6;
            bonusItemLayout.childAlignment = TextAnchor.MiddleLeft;
            bonusItemLayout.childControlWidth = bonusItemLayout.childControlHeight = true;
            bonusItemLayout.childForceExpandWidth = bonusItemLayout.childForceExpandHeight = false;
            var plus = CreateText(bonusItem, "Plus", "+", 52, true, PlusColor);
            Size(plus.gameObject, width: 36, height: 72);
            var bonusIcon = NewUI("Icon", bonusItem);
            bonusIcon.gameObject.AddComponent<Image>().preserveAspect = true;
            Size(bonusIcon.gameObject, width: 72, height: 72);
            bonusItem.gameObject.SetActive(false);
            bonus.gameObject.SetActive(false);

            Size(NewUI("Spacer", row).gameObject, flexibleWidth: 1);

            var buy = CreateButton(row, "Buy Button", "$0.99", BuyColor, Color.white, 40);
            Size(buy.gameObject, width: 300, height: 100);
            var price = FindText(buy.transform);

            // Corner ribbon in the top-right, clipped to a square so it reads as a folded band.
            var badge = NewUI("Badge", card);
            badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(1, 1);
            badge.anchoredPosition = Vector2.zero;
            badge.sizeDelta = new Vector2(150, 150);
            badge.gameObject.AddComponent<RectMask2D>();
            var ribbon = NewUI("Ribbon", badge);
            ribbon.anchorMin = ribbon.anchorMax = ribbon.pivot = new Vector2(0.5f, 0.5f);
            ribbon.anchoredPosition = new Vector2(16, 16);
            ribbon.sizeDelta = new Vector2(240, 44);
            ribbon.localRotation = Quaternion.Euler(0, 0, -45);
            AddImage(ribbon, RibbonColor);
            var badgeText = CreateText(ribbon, "Text", "20% MORE", 24, true, Color.white);
            // Only the middle of the band is inside the corner, so keep the text there and shrink long text to fit.
            var badgeRect = (RectTransform)badgeText.transform;
            Stretch(badgeRect);
            badgeRect.offsetMin = new Vector2(55, 2);
            badgeRect.offsetMax = new Vector2(-55, -2);
            ShrinkToFit(badgeText, 12);
            badge.gameObject.SetActive(false);

            // Sticker on the top-left corner, e.g. HOT. It grows to the right to fit any text.
            var sticker = NewUI("Sticker", card);
            sticker.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            sticker.anchorMin = sticker.anchorMax = new Vector2(0, 1);
            sticker.pivot = new Vector2(0, 0.5f);
            sticker.anchoredPosition = new Vector2(-10, -6);
            sticker.localRotation = Quaternion.Euler(0, 0, 8);
            AddImage(sticker, StickerColor);
            var stickerLayout = sticker.gameObject.AddComponent<HorizontalLayoutGroup>();
            stickerLayout.padding = new RectOffset(18, 18, 6, 6);
            stickerLayout.childAlignment = TextAnchor.MiddleCenter;
            stickerLayout.childControlWidth = stickerLayout.childControlHeight = true;
            stickerLayout.childForceExpandWidth = stickerLayout.childForceExpandHeight = false;
            var stickerFitter = sticker.gameObject.AddComponent<ContentSizeFitter>();
            stickerFitter.horizontalFit = stickerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var stickerText = CreateText(sticker, "Text", "HOT", 28, true, Color.white);
            NoWrap(stickerText);
            sticker.gameObject.SetActive(false);

            // Dimmed overlay shown once a non-consumable is owned.
            var owned = NewUI("Owned", card);
            owned.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Stretch(owned);
            AddImage(owned, new Color(0, 0, 0, 0.45f));
            var ownedText = CreateText(owned, "Text", "OWNED", 44, true, Color.white);
            Stretch((RectTransform)ownedText.transform);
            owned.gameObject.SetActive(false);

            var view = card.gameObject.AddComponent<ShopProductView>();
            var so = new SerializedObject(view);
            so.FindProperty("titleText").objectReferenceValue         = title;
            so.FindProperty("rewardText").objectReferenceValue        = reward;
            so.FindProperty("priceText").objectReferenceValue         = price;
            so.FindProperty("badgeText").objectReferenceValue         = badgeText;
            so.FindProperty("stickerText").objectReferenceValue       = stickerText;
            so.FindProperty("iconImage").objectReferenceValue         = iconImage;
            so.FindProperty("titleRoot").objectReferenceValue         = titleRoot.gameObject;
            so.FindProperty("amountBackground").objectReferenceValue  = amountBackground;
            so.FindProperty("bonusRoot").objectReferenceValue         = bonus;
            so.FindProperty("bonusItemTemplate").objectReferenceValue = bonusItem.gameObject;
            so.FindProperty("badgeRoot").objectReferenceValue         = badge.gameObject;
            so.FindProperty("stickerRoot").objectReferenceValue       = sticker.gameObject;
            so.FindProperty("ownedRoot").objectReferenceValue         = owned.gameObject;
            so.FindProperty("buyButton").objectReferenceValue         = buy;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ── Building blocks ────────────────────────────────────────────────────
        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image AddImage(Component target, Color color)
        {
            var image = target.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type   = Image.Type.Sliced;
            image.color  = color;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color, Color textColor, int size)
        {
            var rect = NewUI(name, parent);
            var image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(rect, "Text", label, size, true, textColor);
            Stretch((RectTransform)text.transform);
            return button;
        }

        private static Graphic CreateText(Transform parent, string name, string value, int size, bool bold, Color color)
        {
            var rect = NewUI(name, parent);
#if ADSIAPKIT_TMP
            var tmp = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
#else
            var text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
#endif
        }

        private static Graphic FindText(Transform root)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                if (!(graphic is Image)) return graphic;
            return null;
        }

        private static void Size(GameObject go, float width = -1, float height = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredWidth  = width;
            element.preferredHeight = height;
            element.flexibleWidth   = flexibleWidth;
            element.flexibleHeight  = flexibleHeight;
        }

        /// <summary>Keeps text on one line so a layout can size its parent to the text.</summary>
        private static void NoWrap(Graphic text)
        {
            if (text is Text legacy)
            {
                legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
                return;
            }
            // TextMeshPro renamed this setting in Unity 6; set whichever one this version serializes.
            var so = new SerializedObject(text);
            var mode = so.FindProperty("m_TextWrappingMode");
            if (mode != null) mode.intValue = 0; // NoWrap
            else if (so.FindProperty("m_enableWordWrapping") is SerializedProperty wrap) wrap.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Lets long text shrink down to minSize instead of overflowing its rect.</summary>
        private static void ShrinkToFit(Graphic text, int minSize)
        {
            if (text is Text legacy)
            {
                legacy.resizeTextForBestFit = true;
                legacy.resizeTextMinSize = minSize;
                legacy.resizeTextMaxSize = legacy.fontSize;
                return;
            }
#if ADSIAPKIT_TMP
            if (text is TMPro.TMP_Text tmp)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = minSize;
                tmp.fontSizeMax = tmp.fontSize;
                NoWrap(tmp);
            }
#endif
        }

        private static void Underline(Graphic text)
        {
#if ADSIAPKIT_TMP
            if (text is TMPro.TMP_Text tmp) tmp.fontStyle |= TMPro.FontStyles.Underline;
#endif
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Transform FindOrCreateCanvas()
        {
            if (Selection.activeTransform != null)
            {
                var selectedCanvas = Selection.activeTransform.GetComponentInParent<Canvas>();
                if (selectedCanvas != null) return selectedCanvas.transform;
            }

            var canvases = FindAll<Canvas>();
            if (canvases.Length > 0) return canvases[0].rootCanvas.transform;

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            EnsureEventSystem();
            return go.transform;
        }

        private static void EnsureEventSystem()
        {
            if (FindAll<EventSystem>().Length > 0) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            // Use the Input System's UI module when that package is the active input handler.
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static T[] FindAll<T>() where T : UnityEngine.Object
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>();
#else
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#endif
        }

        private static void Finish(GameObject go, string undoName)
        {
            Undo.RegisterCreatedObjectUndo(go, undoName);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
#endif
