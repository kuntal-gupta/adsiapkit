using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdsIAPKit
{
    /// <summary>Unity IAP v5 wrapper used by AdsIAPKit.</summary>
    public class IAPManager : MonoBehaviour
    {
        private static IAPManager _instance;
        private IAPConfig _config;
        private bool _isInitializing;
        private readonly Dictionary<string, Action<bool, string>> _pendingPurchases = new Dictionary<string, Action<bool, string>>();

        public static IAPManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[IAPManager]");
                    _instance = go.AddComponent<IAPManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public bool IsInitialized { get; private set; }
        public bool IsInitializing => _isInitializing;
        public static event Action OnIAPInitialized;
        public static event Action<string> OnPurchaseSuccess;
        public static event Action<string, string> OnPurchaseFailure;
        public static event Action<string> OnRestoreSuccess;
        public static event Action OnRestoreFailed;

        /// <summary>
        /// Grants the product to the player and returns true once it is saved.
        /// A purchase is only confirmed with the store after this returns true; otherwise it stays
        /// pending and is delivered again on the next launch, so the player never loses it.
        /// </summary>
        public static Func<string, bool> PurchaseHandler { get; set; }

#if UNITY_IAP_ENABLED
        private UnityEngine.Purchasing.StoreController _controller;
#endif

        // Supports Enter Play Mode without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            PurchaseHandler = null;
            OnIAPInitialized = null;
            OnPurchaseSuccess = null;
            OnPurchaseFailure = null;
            OnRestoreSuccess = null;
            OnRestoreFailed = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Initializes with the catalog in AdsIAPSettings. Safe to call more than once.</summary>
        public void InitializeFromSettings()
        {
            var settings = AdsIAPSettings.Instance;
            if (settings != null && settings.iapConfig != null) Initialize(settings.iapConfig);
            else Debug.LogWarning("[AdsIAPKit] No IAP Config in AdsIAPSettings. Open Window > AdsIAPKit > Setup Window.");
        }

        public void Initialize(IAPConfig config)
        {
            _config = config;
            if (config == null || !config.enableIAP || _isInitializing || IsInitialized) return;

#if UNITY_IAP_ENABLED
            _isInitializing = true;
            InitializeIAP5Async();
#else
            Debug.LogWarning("[AdsIAPKit] Unity IAP not found. Install 'com.unity.purchasing' from Package Manager.");
            IsInitialized = Application.isEditor; // stub mode only in the Editor, never grant free items in a build
#endif
        }

#if UNITY_IAP_ENABLED
        private async void InitializeIAP5Async()
        {
            try
            {
                _controller = UnityEngine.Purchasing.UnityIAPServices.StoreController();
                _controller.OnPurchasePending += OnPurchasePending;
                _controller.OnPurchaseFailed += OnPurchaseFailed;
                _controller.OnProductsFetched += OnProductsFetched;
                _controller.OnProductsFetchFailed += OnProductsFetchFailed;
                _controller.OnPurchasesFetched += OnPurchasesFetched;
                _controller.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                await _controller.Connect();

                var definitions = new List<UnityEngine.Purchasing.ProductDefinition>();
                foreach (var product in _config.products)
                {
                    if (!product.isEnabled) continue;
#if UNITY_ANDROID
                    var storeId = product.GetAndroidId();
#elif UNITY_IOS
                    var storeId = product.GetIOSId();
#else
                    var storeId = product.id;
#endif
                    definitions.Add(new UnityEngine.Purchasing.ProductDefinition(product.id, storeId,
                        (UnityEngine.Purchasing.ProductType)(int)product.productType));
                }

                if (definitions.Count == 0)
                {
                    Debug.LogWarning("[AdsIAPKit] No enabled IAP products are configured.");
                    IsInitialized = true;
                    _isInitializing = false;
                    OnIAPInitialized?.Invoke();
                    return;
                }
                _controller.FetchProducts(definitions);
            }
            catch (Exception exception)
            {
                _isInitializing = false;
                Debug.LogError($"[AdsIAPKit] IAP initialization failed: {exception.Message}");
            }
        }

        private void OnProductsFetched(List<UnityEngine.Purchasing.Product> products)
        {
            IsInitialized = true;
            _isInitializing = false;
            AdsIAPSettings.Log($"IAP initialized with {products.Count} product(s).");
            OnIAPInitialized?.Invoke();
            _controller.FetchPurchases();
        }

        private void OnProductsFetchFailed(UnityEngine.Purchasing.ProductFetchFailed failure)
        {
            _isInitializing = false;
            Debug.LogError($"[AdsIAPKit] Product fetch failed: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(UnityEngine.Purchasing.Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders) MarkOrderProductsAsOwned(order);
            foreach (var order in orders.PendingOrders) OnPurchasePending(order);
        }

        private void OnPurchasesFetchFailed(UnityEngine.Purchasing.PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"[AdsIAPKit] Existing purchases could not be restored: {failure.Message}");
        }

        private void OnPurchasePending(UnityEngine.Purchasing.PendingOrder order)
        {
            var items = order.CartOrdered.Items();
            if (items == null || items.Count == 0) { _controller.ConfirmPurchase(order); return; }

            var allGranted = true;
            foreach (var item in items)
            {
                var id = item.Product.definition.id;
                if (!GrantProduct(id))
                {
                    allGranted = false;
                    continue;
                }
                if (item.Product.definition.type != UnityEngine.Purchasing.ProductType.Consumable)
                    MarkOwned(id);
                if (_pendingPurchases.TryGetValue(id, out var callback))
                {
                    callback.Invoke(true, "");
                    _pendingPurchases.Remove(id);
                }
                AdsIAPSettings.Log($"Purchase success: {id}");
                OnPurchaseSuccess?.Invoke(id);
            }

            // Leave the order pending if anything was not granted; the store delivers it again later.
            if (allGranted) _controller.ConfirmPurchase(order);
            else Debug.LogWarning("[AdsIAPKit] Purchase not confirmed because it could not be granted yet.");
        }

        private void OnPurchaseFailed(UnityEngine.Purchasing.FailedOrder failedOrder)
        {
            var items = failedOrder.CartOrdered.Items();
            var id = items != null && items.Count > 0 ? items[0].Product.definition.id : "unknown";
            var message = string.IsNullOrEmpty(failedOrder.Details) ? failedOrder.FailureReason.ToString() : failedOrder.Details;
            if (_pendingPurchases.TryGetValue(id, out var callback))
            {
                callback.Invoke(false, message);
                _pendingPurchases.Remove(id);
            }
            AdsIAPSettings.Log($"Purchase failed: {id} — {message}");
            OnPurchaseFailure?.Invoke(id, message);
        }

        private static void MarkOrderProductsAsOwned(UnityEngine.Purchasing.Order order)
        {
            var items = order.CartOrdered.Items();
            if (items == null) return;
            foreach (var item in items)
                if (item.Product.definition.type != UnityEngine.Purchasing.ProductType.Consumable)
                    MarkOwned(item.Product.definition.id);
        }
#endif

        private static bool GrantProduct(string productId)
        {
            if (PurchaseHandler == null)
            {
                Debug.LogWarning($"[AdsIAPKit] No PurchaseHandler set, cannot grant {productId}.");
                return false;
            }

            try { return PurchaseHandler(productId); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private static void MarkOwned(string productId)
        {
            PlayerPrefs.SetInt($"IAP_{productId}", 1);
            PlayerPrefs.Save();
        }

        public void Purchase(string productId, Action<bool, string> onResult = null)
        {
            if (!IsInitialized) { onResult?.Invoke(false, "IAP not initialized"); return; }
            if (onResult != null) _pendingPurchases[productId] = onResult;

#if UNITY_IAP_ENABLED
            var product = _controller.GetProductById(productId);
            if (product != null && product.availableToPurchase) _controller.PurchaseProduct(product);
            else
            {
                onResult?.Invoke(false, "Product unavailable");
                _pendingPurchases.Remove(productId);
            }
#else
            // Editor stub (see Initialize): grant through the same path as a real purchase.
            var granted = GrantProduct(productId);
            onResult?.Invoke(granted, granted ? "" : "Not granted");
            _pendingPurchases.Remove(productId);
            if (granted) OnPurchaseSuccess?.Invoke(productId);
#endif
        }

        public void RestorePurchases()
        {
#if UNITY_IAP_ENABLED
            if (!IsInitialized) { OnRestoreFailed?.Invoke(); return; }
            _controller.RestoreTransactions((success, message) =>
            {
                if (success)
                {
                    AdsIAPSettings.Log("Restore complete.");
                    OnRestoreSuccess?.Invoke("restored");
                }
                else
                {
                    Debug.LogWarning($"[AdsIAPKit] Restore failed: {message}");
                    OnRestoreFailed?.Invoke();
                }
            });
#else
            AdsIAPSettings.Log("Restore only needed on iOS.");
#endif
        }

        /// <summary>True when the store returned the product and it can be bought now.</summary>
        public bool IsAvailable(string productId)
        {
            if (!IsInitialized) return false;
#if UNITY_IAP_ENABLED
            var product = _controller.GetProductById(productId);
            return product != null && product.availableToPurchase;
#else
            return _config?.GetProduct(productId) != null;
#endif
        }

        /// <summary>True while a purchase started with a callback is waiting for the store.</summary>
        public bool IsPurchaseInProgress(string productId) => _pendingPurchases.ContainsKey(productId);

        public bool IsPurchased(string productId)
        {
            return PlayerPrefs.GetInt($"IAP_{productId}", 0) == 1;
        }

        public string GetLocalizedPrice(string productId)
        {
#if UNITY_IAP_ENABLED
            if (!IsInitialized) return "";
            return _controller.GetProductById(productId)?.metadata.localizedPriceString ?? "";
#else
            return _config?.GetProduct(productId)?.displayPrice ?? "";
#endif
        }
    }
}
