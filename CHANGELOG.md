# Changelog

All notable changes to this package are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses [Semantic Versioning](https://semver.org/).

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
