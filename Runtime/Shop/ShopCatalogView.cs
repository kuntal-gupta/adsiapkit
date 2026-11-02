#if ADSIAPKIT_UGUI
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AdsIAPKit
{
    /// <summary>
    /// Fills a shop from the IAP catalog: one copy of the card template per product, in catalog order.
    /// Add products in Window > AdsIAPKit > IAP and they appear here without code.
    /// </summary>
    [AddComponentMenu("AdsIAPKit/Shop Catalog View")]
    public class ShopCatalogView : MonoBehaviour
    {
        [Tooltip("Card to copy for each product. A disabled child of the content works, or a prefab.")]
        [SerializeField] private ShopProductView cardTemplate;
        [Tooltip("Parent of the cards, usually a Layout Group inside a Scroll View.")]
        [SerializeField] private Transform content;

        [Header("Filter")]
        [Tooltip("Only products in this category. Blank shows every category.")]
        [SerializeField] private string category = "";
        [Tooltip("Move featured products to the top.")]
        [SerializeField] private bool featuredFirst = false;

        [Header("Store")]
        [Tooltip("Call IAPManager.InitializeFromSettings() on Start. Set IAPManager.PurchaseHandler in Awake.")]
        [SerializeField] private bool initializeIAP = true;
        [Tooltip("Restore Purchases button. Apple requires one on iOS.")]
        [SerializeField] private Button restoreButton;
        [SerializeField] private bool showRestoreOnlyOnIOS = true;

        private readonly List<ShopProductView> _cards = new List<ShopProductView>();

        public IReadOnlyList<ShopProductView> Cards => _cards;

        /// <summary>Shows one category, or every category for null/empty, and rebuilds.</summary>
        public string Category
        {
            get => category;
            set { category = value; Rebuild(); }
        }

        private void Awake()
        {
            // An in-scene template is only a pattern; hide it so it is never shown as a card.
            if (cardTemplate != null && cardTemplate.gameObject.scene.IsValid())
                cardTemplate.gameObject.SetActive(false);

            if (restoreButton != null)
            {
                restoreButton.onClick.AddListener(RestorePurchases);
                var show = !showRestoreOnlyOnIOS || Application.platform == RuntimePlatform.IPhonePlayer || Application.isEditor;
                restoreButton.gameObject.SetActive(show);
            }
        }

        private void Start()
        {
            if (initializeIAP) IAPManager.Instance.InitializeFromSettings();
            Rebuild();
        }

        /// <summary>Destroys the cards and creates them again from the catalog.</summary>
        public void Rebuild()
        {
            foreach (var card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();

            var catalog = AdsIAPSettings.Instance != null ? AdsIAPSettings.Instance.iapConfig : null;
            if (catalog == null) return;

            IEnumerable<IAPProduct> products = catalog.GetShopProducts(category);
            if (featuredFirst) products = products.OrderByDescending(p => p.isFeatured); // stable: keeps catalog order

            var parent = content != null ? content : cardTemplate != null ? cardTemplate.transform.parent : transform;
            foreach (var product in products)
            {
                // A product's own Card Override wins over the shared template.
                var source = product.shopCard != null ? product.shopCard.GetComponent<ShopProductView>() : null;
                if (product.shopCard != null && source == null)
                    Debug.LogWarning($"[AdsIAPKit] Card Override of {product.id} has no ShopProductView; using the Card Template.");
                if (source == null) source = cardTemplate;
                if (source == null) continue;

                var card = Instantiate(source, parent);
                card.name = "Card " + product.id;
                card.Bind(product.id);
                card.gameObject.SetActive(true);
                _cards.Add(card);
            }
        }

        public void RestorePurchases() => IAPManager.Instance.RestorePurchases();
    }
}
#endif
