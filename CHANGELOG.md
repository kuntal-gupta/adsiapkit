# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

## [2.0.0] - 2026-11-25

The kit now supports two ad providers, AdMob and Unity LevelPlay, with Meta Audience Network through mediation in either.

### Removed (breaking)
- **Unity Ads** and **AppLovin MAX** as ad providers: `UnityAdsProvider`, `AppLovinProvider`, `UnityAdsSettings`, `AppLovinSettings`, their enums and `AdsConfig.unityAds` / `AdsConfig.appLovin`. A config set to one of them loads as no provider. Switch it to AdMob or LevelPlay in the setup window. Both of those can still mediate Unity Ads and AppLovin as networks.
- AppLovin's scoped registry and adapter list in the setup window.
- AdMob mediation adapters other than LevelPlay (IronSource) and Meta Audience Network.
- The manual `IRONSOURCE_ENABLED` define. LevelPlay is now detected from its package, like the other SDKs.

### Changed (breaking)
- `AdsProviderType.IronSource` is now `AdsProviderType.LevelPlay`, with the same stored value (3), so a saved config keeps its provider.
- `IronSourceSettings` is now `LevelPlaySettings` and `AdsConfig.ironSource` is now `AdsConfig.levelPlay`. Saved app keys and options carry over. `IronSourceBannerPosition` and `IronSourceBannerSize` are now `LevelPlayBannerAnchor` and `LevelPlayBannerSizeOption`, named apart from the SDK's own types; the stored values keep their meaning (Smart is now Adaptive).
- The LevelPlay provider is rewritten on the LevelPlay 9 API (`com.unity.services.levelplay` 9.0 or newer) and replaces the legacy `IronSource.Agent` calls. LevelPlay needs an ad unit ID per format: enter them in the new banner, interstitial and rewarded fields.

### Fixed
- LevelPlay banners are now loaded and only reported to Banner Safe Area once they are on screen. Before, a banner was never loaded but its space was still reserved.
- LevelPlay interstitials and rewarded ads always call back, including when the ad fails to show or the player closes it without earning the reward. The reward is reported after the ad closes, and it is still counted when the SDK sends it after the close. Interstitials are loaded after initialization and after every show, and failed loads are retried after 30 seconds.
- AdMob's reward callback is moved to the main thread, so it is ordered with the close event.

### Added
- `LevelPlaySettings.enableTestSuite`: `AdsManager.OpenAdDebugger()` opens the LevelPlay Test Suite.
- LevelPlay impression-level revenue is raised through `AdsManager.OnImpressionRevenue` when **Impression Level Revenue** is on.

## [1.5.2] - 2026-11-25

### Fixed
- Players who restored a Remove Ads or feature unlock on 1.5.0 or earlier get it granted on the next launch. Those restores were only marked as owned, and 1.5.1 skipped anything already marked. The kit now records separately that `PurchaseHandler` granted a product. For products owned before this version, it grants again only Remove Ads (including **Also Removes Ads**) and Feature rewards, which are safe to apply twice; other non-consumables are left as they are.
- Confirmed subscriptions are no longer granted through `PurchaseHandler` at startup or on restore, because the kit does not track expiry and an expired subscription would have given its reward permanently. They keep the owned flag as in 1.5.0.
- `OnRestoreSuccess` now fires after the restored products were granted, not before, so the shop and game UI see the new ownership.
- Shop cards refresh on `OnPurchaseRestored`, so a product restored at startup shows **Owned** without reopening the shop.

## [1.5.1] - 2026-11-21

### Fixed
- Non-consumables restored from the store (reinstall, new device or **Restore Purchases**) are now granted through `IAPManager.PurchaseHandler`, not only marked as owned. Before, the shop showed **Owned** while the game never re-applied the entitlement, so a restored Remove Ads left ads on.
- **Restore Purchases** fetches purchases again after a successful restore, so restored orders are processed straight away.

### Added
- `IAPManager.OnPurchaseRestored` event for products granted by a restore. `OnPurchaseSuccess` does not fire for them, so games can skip their purchase message.

## [1.5.0] - 2026-11-21

### Added
- AdMob **Fall Back To Standard** option for Adaptive and SmartBanner sizes (on by default). When the adaptive banner fails to load, the kit retries straight away with the standard 320x50 banner; if that fails too it waits the usual 30 seconds and tries the adaptive size again. The next banner after a scene change or Remove Ads asks for the adaptive size first.
- `AdMobProvider.IsAdaptiveSize`, `EffectiveBannerSize` and `ShouldFallBackToStandard` helpers, covered by Editor tests.

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
