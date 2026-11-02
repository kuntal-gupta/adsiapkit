using System;
using System.Collections.Generic;
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

        [Header("UI")]
        public Sprite icon;
        public bool   isFeatured = false;
        public bool   isEnabled  = true;

        // Runtime helpers
        public string GetAndroidId() => string.IsNullOrEmpty(androidId) ? id : androidId;
        public string GetIOSId()     => string.IsNullOrEmpty(iosId)     ? id : iosId;
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
}
