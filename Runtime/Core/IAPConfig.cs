using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AdsIAPKit
{
    [CreateAssetMenu(fileName = "IAPConfig", menuName = "AdsIAPKit/IAP Config")]
    public class IAPConfig : ScriptableObject
    {
        [Header("General")]
        public bool enableIAP = true;
        // Reserved for server-side receipt validation, which is not implemented yet.
        [HideInInspector] public bool enableReceiptValidation = false;
        [HideInInspector] public string validationUrl = "";

        [Header("Catalog")]
        public List<IAPProduct> products = new List<IAPProduct>();

        public IAPProduct GetProduct(string productId)
        {
            return products.Find(p => p.id == productId);
        }

        /// <summary>
        /// Products to list in a shop, in catalog order: enabled, not hidden, and in the category
        /// when one is given (empty means every category).
        /// </summary>
        public List<IAPProduct> GetShopProducts(string category = null)
        {
            return products.FindAll(p => p.isEnabled && !p.hideInShop &&
                                         (string.IsNullOrEmpty(category) || p.category == category));
        }

        // ── Validation ─────────────────────────────────────────────────────────
        private static readonly Regex GooglePlayId = new Regex("^[a-z0-9][a-z0-9_.]*$");
        private static readonly Regex AppStoreId   = new Regex("^[A-Za-z0-9_.]+$");

        /// <summary>Problems that would make products fail in the stores or in the shop.</summary>
        public List<IAPIssue> Validate()
        {
            var issues     = new List<IAPIssue>();
            var ids        = new HashSet<string>();
            var androidIds = new HashSet<string>();
            var iosIds     = new HashSet<string>();

            // A restorable Remove Ads lets players get No Ads back after a reinstall.
            var sellsRestorableRemoveAds = products.Exists(x => x != null && x.isEnabled &&
                x.rewardType == RewardType.RemoveAds && x.productType == IAPProductType.NonConsumable);

            for (var i = 0; i < products.Count; i++)
            {
                var p = products[i];
                void Error(string message)   => issues.Add(new IAPIssue(i, IAPIssueSeverity.Error, message));
                void Warning(string message) => issues.Add(new IAPIssue(i, IAPIssueSeverity.Warning, message));
                void Info(string message)    => issues.Add(new IAPIssue(i, IAPIssueSeverity.Info, message));

                if (string.IsNullOrWhiteSpace(p.id)) { Error("Product ID is empty."); continue; }
                if (!ids.Add(p.id)) Error($"Product ID '{p.id}' is used by another product.");
                if (!p.isEnabled) continue;

                var androidId = p.GetAndroidId();
                var iosId     = p.GetIOSId();
                if (!GooglePlayId.IsMatch(androidId))
                    Error($"Google Play ID '{androidId}' must start with a lowercase letter or number and use only a-z, 0-9, '_' and '.'.");
                if (!AppStoreId.IsMatch(iosId))
                    Error($"App Store ID '{iosId}' may only use letters, numbers, '_' and '.'.");
                if (!androidIds.Add(androidId)) Error($"Google Play ID '{androidId}' is used by another product.");
                if (!iosIds.Add(iosId))         Error($"App Store ID '{iosId}' is used by another product.");

                if (p.id.StartsWith("com.company.", StringComparison.Ordinal))
                    Warning("Product ID still uses the 'com.company' placeholder.");
                if (string.IsNullOrWhiteSpace(p.displayName))
                    Warning("Display name is empty; the shop shows nothing as the title.");

                if (p.rewardType == RewardType.RemoveAds && p.productType == IAPProductType.Consumable)
                    Error("Remove Ads must be Non-Consumable, or players lose it on reinstall and cannot restore it.");
                if ((p.rewardType == RewardType.Currency || p.rewardType == RewardType.Item) && p.rewardAmount <= 0)
                    Warning("Reward amount is 0, so the purchase grants nothing.");
                if (p.bonusAmount < 0)
                    Error("Bonus amount cannot be negative.");
                else if (p.bonusAmount > 0 && (p.rewardType == RewardType.RemoveAds || p.rewardType == RewardType.Feature))
                    Warning("Bonus amount is ignored for this reward type; it only adds to Currency, Item or Custom amounts.");
                if ((p.rewardType == RewardType.Currency || p.rewardType == RewardType.Item || p.rewardType == RewardType.Feature) &&
                    string.IsNullOrWhiteSpace(p.rewardId))
                    Warning("Reward ID is empty; your purchase handler cannot tell what to grant.");
                if (p.alsoRemovesAds && p.rewardType == RewardType.RemoveAds)
                    Warning("Also Removes Ads is redundant on a Remove Ads product.");
                else if (p.alsoRemovesAds && p.productType == IAPProductType.Consumable)
                {
                    if (sellsRestorableRemoveAds)
                        Info("No Ads from this Consumable pack is lost on reinstall; players get it back with the Non-Consumable Remove Ads product.");
                    else
                        Warning("No Ads from a Consumable is not restored after a reinstall. Also sell a Non-Consumable Remove Ads so players can restore it.");
                }
                if (p.rewardType == RewardType.Currency && p.productType != IAPProductType.Consumable)
                    Warning("Currency packs are usually Consumable so players can buy them again.");
            }
            return issues;
        }
    }

    /// <summary>Error: breaks purchases. Warning: likely a mistake. Info: worth knowing, nothing to fix.</summary>
    public enum IAPIssueSeverity { Warning, Error, Info }

    /// <summary>One problem found by <see cref="IAPConfig.Validate"/>.</summary>
    public readonly struct IAPIssue
    {
        public readonly int              ProductIndex;
        public readonly IAPIssueSeverity Severity;
        public readonly string           Message;

        public IAPIssue(int productIndex, IAPIssueSeverity severity, string message)
        {
            ProductIndex = productIndex;
            Severity     = severity;
            Message      = message;
        }

        public override string ToString() => $"{Severity}: {Message}";
    }

    [Serializable]
    public class IAPProduct
    {
        [Header("Identity")]
        public string id          = "com.company.app.product";
        public string displayName = "Product Name";
        [TextArea(2, 3)]
        public string description = "Product description";

        [Header("Type")]
        public IAPProductType productType = IAPProductType.Consumable;

        [Header("Platform IDs")]
        public string androidId = "";   // leave empty to use 'id'
        public string iosId     = "";   // leave empty to use 'id'

        [Header("Pricing (display only — set in store)")]
        public string displayPrice = "$0.99";

        [Header("Reward")]
        public RewardType rewardType   = RewardType.Currency;
        public string     rewardId     = "coins";
        public int        rewardAmount = 100;
        [Tooltip("Extra amount granted on top of Reward Amount, e.g. a +500 coins promotion. Shown as \"1,000 + 500\". 0 = none.")]
        public int        bonusAmount  = 0;
        [Tooltip("Also removes ads, e.g. a big coin pack with No Ads. Your purchase handler grants it: check RemovesAds().")]
        public bool       alsoRemovesAds = false;

        [Header("UI")]
        public Sprite icon;
        public bool   isFeatured = false;
        public bool   isEnabled  = true;

        [Header("Shop")]
        [Tooltip("Groups products into shop tabs, e.g. Coins or Bundles. A ShopCatalogView can show one category.")]
        public string category = "";
        [Tooltip("Corner ribbon text such as 20% MORE. Featured products without one show BEST VALUE.")]
        public string badge = "";
        [Tooltip("Small sticker on the card's top-left corner, e.g. HOT or NEW. Blank = none.")]
        public string sticker = "";
        [Tooltip("Show the display name as a header strip above the row, e.g. for bundles.")]
        public bool showTitle = false;
        [Tooltip("Extra icons shown after the amount as \"+ icon\", e.g. a no-ads icon on a bundle. Display only: your purchase handler grants them.")]
        public List<Sprite> bonusIcons = new List<Sprite>();
        [Tooltip("Optional card prefab with a ShopProductView, used for this product instead of the shop's Card Template. E.g. a big special-offer card.")]
        public GameObject shopCard;
        [Tooltip("Keep the product purchasable from code but leave it out of ShopCatalogView lists.")]
        public bool hideInShop = false;

        // Runtime helpers
        public string GetAndroidId() => string.IsNullOrEmpty(androidId) ? id : androidId;
        public string GetIOSId()     => string.IsNullOrEmpty(iosId)     ? id : iosId;

        /// <summary>What the purchase grants: Reward Amount plus Bonus Amount.</summary>
        public int GetTotalAmount() => rewardAmount + Math.Max(0, bonusAmount);

        /// <summary>True when buying this product should turn ads off: Remove Ads itself, or a bundle that includes it.</summary>
        public bool RemovesAds() => rewardType == RewardType.RemoveAds || alsoRemovesAds;

        /// <summary>Badge to show, or empty for none.</summary>
        public string GetBadgeText() =>
            !string.IsNullOrEmpty(badge) ? badge : isFeatured ? "BEST VALUE" : "";

        /// <summary>Short reward line for a shop card, e.g. "500" coins or "No Ads".</summary>
        public string GetRewardLabel()
        {
            switch (rewardType)
            {
                case RewardType.Currency:
                case RewardType.Item:
                    if (rewardAmount <= 0) return "";
                    return bonusAmount > 0 ? $"{rewardAmount:N0} + {bonusAmount:N0}" : rewardAmount.ToString("N0");
                case RewardType.RemoveAds: return "No Ads";
                default:                   return "";
            }
        }
    }

    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    public enum RewardType
    {
        Currency,
        Item,
        RemoveAds,
        Feature,
        Custom
    }

    /// <summary>Shows a dropdown of the catalog's product IDs for a string field in the Inspector.</summary>
    public class IAPProductIdAttribute : PropertyAttribute { }
}
