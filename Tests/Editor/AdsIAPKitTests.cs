using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AdsIAPKit.Tests
{
    public class IAPConfigTests
    {
        private IAPConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<IAPConfig>();
            _config.products.Add(new IAPProduct { id = "coins_small", rewardAmount = 100 });
            _config.products.Add(new IAPProduct { id = "remove_ads", rewardType = RewardType.RemoveAds, androidId = "android.remove_ads" });
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        [Test]
        public void GetProduct_ReturnsMatchingProduct()
        {
            Assert.AreEqual(100, _config.GetProduct("coins_small").rewardAmount);
        }

        [Test]
        public void GetProduct_ReturnsNullForUnknownId()
        {
            Assert.IsNull(_config.GetProduct("missing"));
        }

        [Test]
        public void StoreIds_FallBackToProductId()
        {
            var product = _config.GetProduct("coins_small");
            Assert.AreEqual("coins_small", product.GetAndroidId());
            Assert.AreEqual("coins_small", product.GetIOSId());
        }

        [Test]
        public void StoreIds_UsePlatformOverride()
        {
            var product = _config.GetProduct("remove_ads");
            Assert.AreEqual("android.remove_ads", product.GetAndroidId());
            Assert.AreEqual("remove_ads", product.GetIOSId());
        }
    }

    public class IAPCatalogValidationTests
    {
        private IAPConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<IAPConfig>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private static IAPProduct Coins(string id) => new IAPProduct
        {
            id = id, displayName = "Coins", productType = IAPProductType.Consumable,
            rewardType = RewardType.Currency, rewardId = "coins", rewardAmount = 100
        };

        private bool HasError(string fragment) => _config.Validate().Exists(i =>
            i.Severity == IAPIssueSeverity.Error && i.Message.Contains(fragment));

        [Test]
        public void ValidCatalog_HasNoIssues()
        {
            _config.products.Add(Coins("game.coins_small"));
            _config.products.Add(new IAPProduct
            {
                id = "game.remove_ads", displayName = "Remove Ads",
                productType = IAPProductType.NonConsumable, rewardType = RewardType.RemoveAds
            });
            CollectionAssert.IsEmpty(_config.Validate());
        }

        [Test]
        public void DuplicateId_IsError()
        {
            _config.products.Add(Coins("game.coins"));
            _config.products.Add(Coins("game.coins"));
            Assert.IsTrue(HasError("used by another product"));
        }

        [Test]
        public void UppercaseGooglePlayId_IsError()
        {
            _config.products.Add(Coins("Game.Coins"));
            Assert.IsTrue(HasError("Google Play ID"));
        }

        [Test]
        public void ConsumableRemoveAds_IsError()
        {
            var product = Coins("game.remove_ads");
            product.rewardType = RewardType.RemoveAds;
            _config.products.Add(product);
            Assert.IsTrue(HasError("Remove Ads must be Non-Consumable"));
        }

        [Test]
        public void RemovesAds_TrueForRemoveAdsAndBundles()
        {
            var removeAds = new IAPProduct { rewardType = RewardType.RemoveAds };
            var pack = Coins("game.coins_big");
            Assert.IsTrue(removeAds.RemovesAds());
            Assert.IsFalse(pack.RemovesAds());
            pack.alsoRemovesAds = true;
            Assert.IsTrue(pack.RemovesAds());
        }

        [Test]
        public void ConsumableBundleWithNoAds_IsWarning()
        {
            var pack = Coins("game.coins_big");
            pack.alsoRemovesAds = true;
            _config.products.Add(pack);
            Assert.IsTrue(_config.Validate().Exists(i =>
                i.Severity == IAPIssueSeverity.Warning && i.Message.Contains("not restored after a reinstall")));
            Assert.IsFalse(_config.Validate().Exists(i => i.Severity == IAPIssueSeverity.Error));
        }

        [Test]
        public void BonusAmount_AddsToTotalAndLabel()
        {
            var pack = Coins("game.coins_bonus");
            Assert.AreEqual(100, pack.GetTotalAmount());
            pack.bonusAmount = 50;
            Assert.AreEqual(150, pack.GetTotalAmount());
            StringAssert.Contains("+", pack.GetRewardLabel());
        }

        [Test]
        public void NegativeBonusAmount_IsError()
        {
            var pack = Coins("game.coins_bonus");
            pack.bonusAmount = -5;
            _config.products.Add(pack);
            Assert.IsTrue(HasError("Bonus amount cannot be negative"));
        }

        [Test]
        public void DisabledProduct_SkipsStoreChecks()
        {
            var product = Coins("Bad ID");
            product.isEnabled = false;
            _config.products.Add(product);
            CollectionAssert.IsEmpty(_config.Validate());
        }

        [Test]
        public void ShopProducts_SkipDisabledHiddenAndOtherCategories()
        {
            var coins  = Coins("a"); coins.category = "Coins";
            var hidden = Coins("b"); hidden.category = "Coins"; hidden.hideInShop = true;
            var off    = Coins("c"); off.category = "Coins"; off.isEnabled = false;
            var item   = Coins("d"); item.category = "Items";
            _config.products.AddRange(new[] { coins, hidden, off, item });

            CollectionAssert.AreEqual(new[] { coins }, _config.GetShopProducts("Coins"));
            CollectionAssert.AreEqual(new[] { coins, item }, _config.GetShopProducts());
        }

        [Test]
        public void Badge_FallsBackToBestValueWhenFeatured()
        {
            var product = Coins("a");
            Assert.AreEqual("", product.GetBadgeText());
            product.isFeatured = true;
            Assert.AreEqual("BEST VALUE", product.GetBadgeText());
            product.badge = "-50%";
            Assert.AreEqual("-50%", product.GetBadgeText());
        }
    }

    public class PackageInstallerTests
    {
        [Test]
        public void MergeScopedRegistries_AddsRegistryAndMissingScopes()
        {
            var manifest = (Dictionary<string, object>)Editor.MiniJson.Parse(
                "{ \"dependencies\": {}, \"scopedRegistries\": [ { \"name\": \"OpenUPM\", \"url\": \"https://package.openupm.com/\", \"scopes\": [ \"com.other\" ] } ] }");

            var changed = Editor.KitManifest.MergeScopedRegistries(manifest,
                new[] { Editor.KitPackages.OpenUpmGoogle, Editor.KitPackages.AppLovinRegistry });

            Assert.IsTrue(changed);
            var registries = (List<object>)manifest["scopedRegistries"];
            Assert.AreEqual(2, registries.Count, "the OpenUPM URL with a trailing slash is the same registry");
            var openUpmScopes = (List<object>)((Dictionary<string, object>)registries[0])["scopes"];
            CollectionAssert.AreEqual(new object[] { "com.other", "com.google" }, openUpmScopes);

            Assert.IsFalse(Editor.KitManifest.MergeScopedRegistries(manifest,
                new[] { Editor.KitPackages.OpenUpmGoogle }), "a second merge changes nothing");
        }

        [Test]
        public void MiniJson_RoundTripsManifest()
        {
            const string json = "{\n  \"dependencies\": {\n    \"com.unity.ugui\": \"2.5.0\"\n  },\n  \"testables\": [],\n  \"n\": 3,\n  \"s\": \"a\\\"b\"\n}";
            Assert.AreEqual(json, Editor.MiniJson.Serialize(Editor.MiniJson.Parse(json)));
        }

        [Test]
        public void ParseCsv_HandlesQuotesCommasAndLineBreaks()
        {
            var rows = Editor.IAPTab.ParseCsv("ID,Description\r\ncoins,\"Big, \"\"shiny\"\"\npack\"\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "coins", "Big, \"shiny\"\npack" }, rows[1]);
        }
    }

    public class AdsConfigTests
    {
        [Test]
        public void NewConfig_IsSafeByDefault()
        {
            var config = ScriptableObject.CreateInstance<AdsConfig>();
            try
            {
                Assert.AreEqual(AdsProviderType.None, config.activeProvider);
                Assert.IsTrue(config.enableTestMode);
                Assert.IsTrue(config.adMob.useTestIds);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
