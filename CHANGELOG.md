# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [1.4.0] - 2026-11-06

### Added
- Banner layout state: `AdsManager.IsBannerRequested`, `IsBannerLoaded`, `BannerHeightPixels`, `BannerEdge`, `GetBannerHeight(Canvas)` and the `OnBannerLayoutChanged` event. AdMob reports the real loaded height (adaptive banners included) and AppLovin MAX the adaptive height; Unity Ads and IronSource report their standard size. Without an ad SDK the Editor simulates a 50 dp banner.
- **Banner Safe Area** component (`BannerSafeArea`). Put it on a scene's content panel to inset it by the banner height, so UI never sits under the banner. It can reserve the space as soon as a banner is requested so the layout does not jump when the ad loads.

### Changed
- AppLovin MAX creates the banner once and reuses it on later `ShowBanner()` calls instead of creating it on every call.

## [1.3.3] - 2026-11-06

### Added
- **Auto-Approve Test Purchases** toggle in the IAP tab. It sets `IAP_FAKE_STORE_DEFAULT` (Android, iOS, Standalone) so Unity IAP's Editor fake store approves purchases without its Buy/Cancel dialog, which cannot be clicked in the Device Simulator. Device builds are unaffected.

## [1.3.2] - 2026-11-06

### Fixed
- Deferred purchases (iOS Ask to Buy, Google Play pending payments) are handled. Unity IAP no longer warns "Purchase called without a callback defined for IPurchaseService.OnPurchaseDeferred", and the Purchase callback ends with `IAPManager.DeferredMessage` instead of waiting forever.

### Added
- `IAPManager.OnPurchaseDeferred` event. Nothing is granted until the store approves; the purchase then arrives through `PurchaseHandler` and `OnPurchaseSuccess` as usual.

## [1.3.1] - 2026-11-06

### Changed
- The setup window menu moved from **Window → AdsIAPKit** to **Tools → AdsIAPKit** (Setup Window, Quick Select Settings).
- Catalog check: No Ads on a Consumable pack is now an info note instead of a warning when the catalog also sells a Non-Consumable Remove Ads, since players can get No Ads back that way.

### Added
- `IAPIssueSeverity.Info` for notes that need no fix. The catalog editor shows them as info and counts them separately.

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
