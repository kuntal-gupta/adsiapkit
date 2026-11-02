#if ADSIAPKIT_UGUI
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AdsIAPKit
{
    /// <summary>
    /// One shop row for one catalog product: icon, amount, bonus icons, corner ribbon, sticker,
    /// the store's localized price and a Buy button. Text fields accept Text or TextMeshPro.
    /// Every field is optional, so the same component drives any card layout.
    /// </summary>
    [AddComponentMenu("AdsIAPKit/Shop Product View")]
    public class ShopProductView : MonoBehaviour
    {
        [SerializeField, IAPProductId] private string productId;

        [Header("Texts (Text or TextMeshPro)")]
        [SerializeField] private Graphic titleText;
        [SerializeField] private Graphic descriptionText;
        [SerializeField] private Graphic rewardText;
        [SerializeField] private Graphic priceText;
        [SerializeField] private Graphic badgeText;
        [SerializeField] private Graphic stickerText;

        [Header("Images and objects")]
        [SerializeField] private Image      iconImage;
        [Tooltip("Header strip shown only for products with Show Title. Leave empty to always show the title.")]
        [SerializeField] private GameObject titleRoot;
        [Tooltip("Pill behind the amount. Hidden when the product has no amount and its name is shown instead.")]
        [SerializeField] private Image      amountBackground;
        [Tooltip("Parent of the bonus icons; hidden when the product has none.")]
        [SerializeField] private Transform  bonusRoot;
        [Tooltip("One \"+ icon\" item, copied for each bonus icon. Its Image named Icon gets the sprite.")]
        [SerializeField] private GameObject bonusItemTemplate;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private GameObject stickerRoot;
        [Tooltip("Resize the sticker to its text so any length fits. Ignored when the sticker has a Content Size Fitter.")]
        [SerializeField] private bool  fitStickerToText = true;
        [SerializeField] private float stickerPadding   = 18;
        [SerializeField] private GameObject featuredRoot;
        [SerializeField] private GameObject ownedRoot;
        [SerializeField] private Button     buyButton;

        [Header("Labels")]
        [SerializeField] private string ownedLabel       = "Owned";
        [SerializeField] private string loadingLabel     = "…";
        [SerializeField] private string unavailableLabel = "Unavailable";

        [Header("Events")]
        [Tooltip("Runs after this card's purchase was granted by IAPManager.PurchaseHandler.")]
        public UnityEvent<string> onPurchased = new UnityEvent<string>();
        [Tooltip("Runs when this card's purchase fails or is cancelled. Arguments: product ID, message.")]
        public UnityEvent<string, string> onPurchaseFailed = new UnityEvent<string, string>();

        private readonly List<GameObject> _bonusItems = new List<GameObject>();

        public string     ProductId => productId;
        public IAPProduct Product   { get; private set; }

        /// <summary>Shows another product on this card.</summary>
        public void Bind(string id)
        {
            productId = id;
            Refresh();
        }

        private void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(Buy);
            if (bonusItemTemplate != null) bonusItemTemplate.SetActive(false);
        }

        private void OnEnable()
        {
            IAPManager.OnIAPInitialized  += Refresh;
            IAPManager.OnPurchaseSuccess += OnPurchaseEvent;
            IAPManager.OnRestoreSuccess  += OnPurchaseEvent;
            Refresh();
        }

        private void OnDisable()
        {
            IAPManager.OnIAPInitialized  -= Refresh;
            IAPManager.OnPurchaseSuccess -= OnPurchaseEvent;
            IAPManager.OnRestoreSuccess  -= OnPurchaseEvent;
        }

        // Any purchase or restore can change ownership, so refresh on all of them.
        private void OnPurchaseEvent(string _) => Refresh();

        /// <summary>Redraws the card from the catalog and the store's current state.</summary>
        public void Refresh()
        {
            var catalog = AdsIAPSettings.Instance != null ? AdsIAPSettings.Instance.iapConfig : null;
            Product = catalog != null ? catalog.GetProduct(productId) : null;

            if (Product == null)
            {
                ShopText.Set(titleText, string.IsNullOrEmpty(productId) ? "No product" : productId);
                ShopText.Set(priceText, unavailableLabel);
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            ShopText.Set(titleText, Product.displayName);
            ShopText.Set(descriptionText, Product.description);
            if (titleRoot != null) titleRoot.SetActive(Product.showTitle);

            // Coins and items show their amount in a pill; Remove Ads and unlocks show their name.
            var amount = Product.rewardType == RewardType.Currency || Product.rewardType == RewardType.Item
                ? Product.GetRewardLabel() : "";
            ShopText.Set(rewardText, string.IsNullOrEmpty(amount) ? Product.displayName : amount);
            if (amountBackground != null) amountBackground.enabled = !string.IsNullOrEmpty(amount);
            RefreshBonusIcons();

            if (iconImage != null)
            {
                iconImage.sprite = Product.icon;
                iconImage.enabled = Product.icon != null;
            }

            var badge = Product.GetBadgeText();
            ShopText.Set(badgeText, badge);
            if (badgeRoot != null)    badgeRoot.SetActive(!string.IsNullOrEmpty(badge));
            if (featuredRoot != null) featuredRoot.SetActive(Product.isFeatured);

            ShopText.Set(stickerText, Product.sticker);
            if (stickerRoot != null) stickerRoot.SetActive(!string.IsNullOrEmpty(Product.sticker));
            FitStickerToText();

            var iap = IAPManager.Instance;
            // Subscriptions can expire, which the kit does not track, so only non-consumables show as owned.
            var owned = Product.productType == IAPProductType.NonConsumable && iap.IsPurchased(productId);
            if (ownedRoot != null) ownedRoot.SetActive(owned);

            ShopText.Set(priceText, owned ? ownedLabel : PriceLabel(iap));
            if (buyButton != null)
                buyButton.interactable = !owned && iap.IsAvailable(productId) && !iap.IsPurchaseInProgress(productId);
        }

        private void FitStickerToText()
        {
            if (!fitStickerToText || stickerRoot == null || stickerText == null || !stickerRoot.activeSelf) return;
            if (stickerRoot.GetComponent<ContentSizeFitter>() != null) return; // its layout already sizes it

            var rect  = (RectTransform)stickerRoot.transform;
            var width = LayoutUtility.GetPreferredWidth(stickerText.rectTransform) + stickerPadding * 2 + 4;
            var grow  = width - rect.rect.width;
            if (Mathf.Abs(grow) < 0.5f) return;

            // Keep the left edge in place so the sticker grows into the card, not off its edge.
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.anchoredPosition += new Vector2(grow * rect.pivot.x, 0);
        }

        private void RefreshBonusIcons()
        {
            foreach (var item in _bonusItems)
                if (item != null) Destroy(item);
            _bonusItems.Clear();

            var icons = Product.bonusIcons;
            var count = 0;
            if (icons != null && bonusItemTemplate != null)
            {
                var parent = bonusRoot != null ? bonusRoot : bonusItemTemplate.transform.parent;
                foreach (var sprite in icons)
                {
                    if (sprite == null) continue;
                    var item = Instantiate(bonusItemTemplate, parent);
                    var icon = item.transform.Find("Icon");
                    var image = icon != null ? icon.GetComponent<Image>() : item.GetComponentInChildren<Image>(true);
                    if (image != null) image.sprite = sprite;
                    item.SetActive(true);
                    _bonusItems.Add(item);
                    count++;
                }
            }
            if (bonusRoot != null) bonusRoot.gameObject.SetActive(count > 0);
        }

        private string PriceLabel(IAPManager iap)
        {
            var price = iap.GetLocalizedPrice(productId);
            if (!string.IsNullOrEmpty(price)) return price;
            if (iap.IsInitialized) return unavailableLabel;
            // The fallback price is only a placeholder for the Editor; a device waits for the store.
            return Application.isEditor && !string.IsNullOrEmpty(Product.displayPrice) ? Product.displayPrice : loadingLabel;
        }

        /// <summary>Starts the purchase. Hooked to the Buy button automatically.</summary>
        public void Buy()
        {
            if (Product == null) return;
            if (buyButton != null) buyButton.interactable = false;

            var id = productId;
            IAPManager.Instance.Purchase(id, (success, message) =>
            {
                if (success) onPurchased.Invoke(id);
                else onPurchaseFailed.Invoke(id, message);
                if (this != null) Refresh();
            });
        }
    }
}
#endif
