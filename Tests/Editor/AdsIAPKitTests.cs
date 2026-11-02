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
