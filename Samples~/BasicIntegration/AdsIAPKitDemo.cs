using AdsIAPKit;
using UnityEngine;

namespace AdsIAPKit.Samples
{
    /// <summary>
    /// Minimal integration: add this to an empty GameObject and press Play.
    /// Configure the kit first in Window > AdsIAPKit > Setup Window.
    /// </summary>
    public class AdsIAPKitDemo : MonoBehaviour
    {
        private int    _coins;
        private bool   _adsRemoved;
        private string _status = "Starting…";

        private void Start()
        {
            // Grant purchases here and return true once saved, so the kit can confirm them.
            IAPManager.PurchaseHandler = GrantProduct;
            IAPManager.OnPurchaseFailure += (id, message) => _status = $"Purchase failed: {id} ({message})";

            var settings = AdsIAPSettings.Instance;
            if (settings != null && settings.iapConfig != null)
                IAPManager.Instance.Initialize(settings.iapConfig);

            AdsManager.OnAdsInitialized += () =>
            {
                _status = $"Ads ready ({AdsManager.Instance.ProviderName})";
                if (!_adsRemoved) AdsManager.Instance.ShowBanner();
            };
            // Touching Instance starts the ad SDK if "Initialize On Start" is off.
            _ = AdsManager.Instance;
        }

        private bool GrantProduct(string productId)
        {
            var product = AdsIAPSettings.Instance?.iapConfig?.GetProduct(productId);
            if (product == null) return false;

            if (product.rewardType != RewardType.RemoveAds) _coins += product.GetTotalAmount();
            if (product.RemovesAds())
            {
                _adsRemoved = true;
                AdsManager.Instance.DestroyBanner();
            }

            _status = $"Purchased {product.displayName}";
            return true;
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(1f, Screen.dpi / 160f));
            GUILayout.BeginArea(new Rect(16, 16, 320, 900));

            GUILayout.Label($"Coins: {_coins}");
            GUILayout.Label(_status);
            GUILayout.Space(8);

            if (GUILayout.Button("Show Banner")) AdsManager.Instance.ShowBanner();
            if (GUILayout.Button("Hide Banner")) AdsManager.Instance.HideBanner();

            GUI.enabled = AdsManager.Instance.IsInterstitialReady();
            if (GUILayout.Button("Show Interstitial"))
                AdsManager.Instance.ShowInterstitial(() => _status = "Interstitial closed");

            GUI.enabled = AdsManager.Instance.IsRewardedReady();
            if (GUILayout.Button("Show Rewarded (+50 coins)"))
            {
                AdsManager.Instance.ShowRewarded(rewarded =>
                {
                    if (rewarded) _coins += 50;
                    _status = rewarded ? "Reward earned" : "Reward not earned";
                });
            }
            GUI.enabled = true;

            GUILayout.Space(8);
            var catalog = AdsIAPSettings.Instance?.iapConfig;
            if (catalog != null)
            {
                foreach (var product in catalog.products)
                {
                    if (!product.isEnabled) continue;
                    var price = IAPManager.Instance.GetLocalizedPrice(product.id);
                    if (GUILayout.Button($"Buy {product.displayName} {price}"))
                        IAPManager.Instance.Purchase(product.id);
                }
            }
            if (GUILayout.Button("Restore Purchases")) IAPManager.Instance.RestorePurchases();

            if (AdsManager.Instance.IsPrivacyOptionsRequired && GUILayout.Button("Privacy Settings"))
                AdsManager.Instance.ShowPrivacyOptions();

            GUILayout.EndArea();
        }
    }
}
