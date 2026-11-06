# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [1.3.0] - 2026-11-06

### Added
- Product **Bonus Amount** (`IAPProduct.bonusAmount`): extra currency or items on top of the amount, e.g. a +500 coins promotion, set in the catalog editor and CSV (`BonusAmount` column).
- `IAPProduct.GetTotalAmount()`: amount plus bonus, for purchase handlers.
- `GetRewardLabel()` shows the bonus, e.g. "1,000 + 500".
- Catalog check: a negative bonus is an error; a bonus on Remove Ads or Feature products is a warning.

## [1.2.0] - 2026-11-06

### Added
- Product **Also Removes Ads** option (`IAPProduct.alsoRemovesAds`) for bundles such as a coin pack with No Ads, set in the catalog editor and CSV (`AlsoRemovesAds` column).
- `IAPProduct.RemovesAds()`: true for Remove Ads products and for bundles with Also Removes Ads, so a purchase handler needs one check.
- Catalog check warns that No Ads from a Consumable is not restored after a reinstall.

## [1.1.0] - 2026-11-02

### Added
- Setup window lists the packages the active ad network and IAP need, with **Install** buttons that also add the OpenUPM and AppLovin scoped registries to the manifest.
- Mediation section: install or remove AdMob and AppLovin MAX adapters for 12 networks; links to the AppLovin Integration Manager and LevelPlay Network Manager.
- `AdsManager.OpenAdDebugger()`: AdMob Ad Inspector or AppLovin Mediation Debugger.
- iOS ATT settings and package row in the Ads tab.
- IAP catalog editor: product templates, search, reorder, duplicate, store ID preview, bundle-ID suggestions, CSV import, undo.
- Catalog check (`IAPConfig.Validate()`): duplicate and invalid store IDs, Remove Ads set to Consumable, empty rewards and more.
- Product shop fields: category, badge (corner ribbon), sticker, show title, bonus icons, card override and hide in shop.
- Custom shop cards: **Auto-Assign by Name** in the `ShopProductView` Inspector wires fields from child names and warns about a missing Buy button or price; a product's **Card Override** prefab replaces the template for that product.
- Shop UI: `ShopCatalogView` and `ShopProductView`, with **GameObject → AdsIAPKit** menu items that build a shop on a Canvas. Works with TextMeshPro or legacy Text.
- `IAPManager.InitializeFromSettings()`, `IsAvailable()`, `IsPurchaseInProgress()`, `IsInitializing`.
- `[IAPProductId]` attribute: a catalog dropdown for string fields.
- Setup Guide (`Documentation~/SetupGuide.md`), opened from the window's **Guide** button.

### Changed
- The setup window no longer opens a modal dialog for missing SDKs.
- CSV export quotes fields correctly and includes the new columns.

## [1.0.0] - 2026-11-02

First release as a Unity package.

### Added
- `AdsManager`: one API for banner, interstitial and rewarded ads across AdMob, Unity Ads, IronSource and AppLovin MAX.
- `IAPManager`: Unity IAP 5 wrapper with a product catalog, restore and localized prices.
- `IAPManager.PurchaseHandler`: purchases are confirmed with the store only after the game grants them.
- Google UMP consent before ads are requested, plus `IsPrivacyOptionsRequired` and `ShowPrivacyOptions()`.
- iOS App Tracking Transparency request and `Info.plist` usage description.
- Setup window (**Window → AdsIAPKit**) that creates the settings and checks for missing SDKs.
- Optional SDK detection through version defines, including AppLovin MAX and iOS support packages.
- "Initialize On Start" option that starts ads without any scene setup.
- AdMob: interstitial and rewarded ads with automatic reload, retry after a failed load and impression revenue.
- Basic Integration sample and EditMode tests.

### Changed
- SDK callbacks are moved to the Unity main thread.
- SDK stubs are only active in the Editor; a build with a missing SDK grants nothing.
- Consumable purchases are no longer stored as owned.

### Fixed
- AdMob iOS test ad unit IDs for interstitial and rewarded ads.
- AdMob Editor placeholder banner disappearing after a scene load.
- Remove-ads purchases did not clear the banner request.
