# Ads & IAP Kit

One API for mobile ads and in-app purchases in Unity. Pick an ad network in a setup window, call `AdsManager` and `IAPManager` from your game, and switch networks later without touching game code.

- **Ads:** banner, interstitial and rewarded through AdMob, Unity Ads, IronSource or AppLovin MAX
- **IAP:** Unity IAP 5 with a product catalog, restore, localized prices and purchases that are never lost
- **Privacy:** Google UMP consent (GDPR) and iOS App Tracking Transparency
- **Safe by default:** test ad IDs in test mode, SDK callbacks moved to the main thread, and no free rewards or items in a build when an SDK is missing

## Requirements

- Unity 2022.3 or newer (tested on Unity 6000.5)
- Android or iOS build target
- The SDKs for the features you use (all optional, see below)

## Installation

### 1. Add the package

**Package Manager → + → Add package from git URL…**

```
https://github.com/pixelsbd-solution/word-finder-crossword-puzzles.git?path=/Packages/com.puzzlecraftgames.adsiapkit#adsiapkit-v1.0.0
```

Or add it to `Packages/manifest.json`:

```json
"com.puzzlecraftgames.adsiapkit": "https://github.com/pixelsbd-solution/word-finder-crossword-puzzles.git?path=/Packages/com.puzzlecraftgames.adsiapkit#adsiapkit-v1.0.0"
```

The `#adsiapkit-v1.0.0` tag pins a version. Use `#main` to follow the latest changes.

### 2. Install the SDKs you need

The kit turns each integration on automatically when it finds the matching package:

| Feature | Package | Enables |
|---|---|---|
| In-app purchases | `com.unity.purchasing` 5.0+ | `UNITY_IAP_ENABLED` |
| AdMob | `com.google.ads.mobile` (Google Mobile Ads, UPM/OpenUPM) | `ADMOB_ENABLED` |
| Unity Ads | `com.unity.ads` 4.0+ | `UNITY_ADS_ENABLED` |
| AppLovin MAX | `com.applovin.mediation.ads` | `APPLOVIN_ENABLED` |
| IronSource | IronSource Unity plugin, then add `IRONSOURCE_ENABLED` to **Player Settings → Scripting Define Symbols** | `IRONSOURCE_ENABLED` |
| iOS ATT prompt | `com.unity.ads.ios-support` | `ATT_SUPPORT_ENABLED` |

> AdMob must be installed as a **UPM package**. The old `.unitypackage` import does not set `ADMOB_ENABLED`.

### 3. Configure

Open **Window → AdsIAPKit → Setup Window**. The first time, it creates the settings in `Assets/Resources/AdsIAPKit/`:

- `AdsIAPSettings.asset`: the master asset the kit loads at runtime
- `AdsConfig.asset`: active ad network, ad unit IDs, test mode, consent, ATT
- `IAPConfig.asset`: your product catalog

These assets belong to your project, so commit them. Updating the package never overwrites them.

The window warns you when an SDK that your settings need is missing.

## Quick start

### Ads

With **Initialize On Start** on (the default), ads start when the first scene loads. No scene setup is needed.

```csharp
using AdsIAPKit;

AdsManager.Instance.ShowBanner();

// Interstitial: the callback runs when the ad closes, or right away if no ad is ready.
AdsManager.Instance.ShowInterstitial(() => ResumeGame());

// Rewarded: only grant the reward when `rewarded` is true.
if (AdsManager.Instance.IsRewardedReady())
{
    AdsManager.Instance.ShowRewarded(rewarded =>
    {
        if (rewarded) GiveCoins(50);
    });
}
```

Ads load again by themselves after each show, and a failed load is retried after 30 seconds.

### In-app purchases

Set a **purchase handler** that gives the player the product and returns `true` once it is saved. The kit confirms a purchase with the store only after your handler returns `true`. If it returns `false` or throws, the purchase stays pending and the store delivers it again on the next launch, so players never lose a purchase.

```csharp
using AdsIAPKit;

void Start()
{
    IAPManager.PurchaseHandler = productId =>
    {
        var product = AdsIAPSettings.Instance.iapConfig.GetProduct(productId);
        if (product == null) return false;

        if (product.rewardType == RewardType.RemoveAds)
        {
            SaveRemoveAds();
            AdsManager.Instance.DestroyBanner();
        }
        else
        {
            AddCoins(product.rewardAmount);
        }
        return true;
    };

    // Set the handler before Initialize, so purchases waiting from a previous session are granted.
    IAPManager.Instance.Initialize(AdsIAPSettings.Instance.iapConfig);
}

public void OnBuyClicked(string productId)
{
    IAPManager.Instance.Purchase(productId, (success, error) =>
    {
        if (!success) ShowMessage("Purchase failed: " + error);
    });
}
```

Use `IAPManager.Instance.GetLocalizedPrice(id)` for price labels and `RestorePurchases()` for a **Restore** button (required on iOS). `IsPurchased(id)` reports owned non-consumables.

### Privacy (GDPR / UMP)

With **GDPR Consent (UMP)** on, the kit asks Google whether consent is needed and shows Google's consent form before any ad is requested. Create the consent message in the AdMob console under **Privacy & messaging**.

Google requires a way for users to change their choice later. Add a button to your settings screen:

```csharp
privacyButton.SetActive(AdsManager.Instance.IsPrivacyOptionsRequired);
privacyButton.onClick.AddListener(() => AdsManager.Instance.ShowPrivacyOptions());
```

### iOS App Tracking Transparency

With **ATT** on and `com.unity.ads.ios-support` installed, the kit requests tracking permission at startup and writes `NSUserTrackingUsageDescription` to `Info.plist` during the iOS build.

## API reference

### `AdsManager.Instance`

| Member | Description |
|---|---|
| `IsInitialized`, `ProviderName` | SDK state and active network |
| `ShowBanner()`, `HideBanner()`, `DestroyBanner()` | Banner control |
| `IsInterstitialReady()`, `ShowInterstitial(Action onClosed)` | Interstitial |
| `IsRewardedReady()`, `ShowRewarded(Action<bool> onResult)` | Rewarded; `true` means the user earned the reward |
| `IsPrivacyOptionsRequired`, `ShowPrivacyOptions(Action onClosed)` | UMP privacy options (AdMob) |

Static events: `OnAdsInitialized`, `OnInterstitialClosed`, `OnRewardedResult(bool)`, `OnImpressionRevenue(double)`. Impression revenue is in the ad network's currency, in whole units (not micros).

### `IAPManager`

| Member | Description |
|---|---|
| `PurchaseHandler` (static) | `Func<string, bool>` that grants a product; required |
| `Instance.Initialize(IAPConfig)` | Connects to the store and fetches products |
| `Instance.Purchase(id, Action<bool, string>)` | Starts a purchase |
| `Instance.RestorePurchases()` | Restores purchases (iOS) |
| `Instance.IsPurchased(id)` | Owned non-consumables |
| `Instance.GetLocalizedPrice(id)` | Store price string |

Static events: `OnIAPInitialized`, `OnPurchaseSuccess(id)`, `OnPurchaseFailure(id, message)`, `OnRestoreSuccess`, `OnRestoreFailed`.

## Ad network support

| Network | Banner | Interstitial | Rewarded | Consent (UMP) | Status |
|---|---|---|---|---|---|
| AdMob | ✓ | ✓ | ✓ | ✓ | Stable |
| Unity Ads | ✓ | ✓ | ✓ | – | Preview |
| AppLovin MAX | ✓ | ✓ | ✓ | – | Preview |
| IronSource | ✓ | ✓ | ✓ | – | Preview (legacy `IronSource.Agent` API) |

**Preview** networks have not yet been tested against the current SDK releases. Test them in your project before shipping.

## Testing in the Editor

- In **Test Mode** with **Use Test IDs**, AdMob uses Google's official test ad units. Never ship with Test Mode on; the kit logs a warning if a Google test ID is used with Test Mode off.
- When an ad or IAP SDK is missing, the kit runs a stub in the Editor so you can click through your flows: ads are always "ready" and purchases always succeed. In a build, a missing SDK reports "not initialized", so nothing is granted for free.
- AdMob's Editor placeholder banner is recreated after each scene load so it behaves like the device banner.
- Enter Play Mode without a domain reload is supported.

## Sample

**Package Manager → Ads & IAP Kit → Samples → Basic Integration → Import**, then add `AdsIAPKitDemo` to an empty GameObject and press Play.

## Troubleshooting

| Problem | Fix |
|---|---|
| "AdsIAPSettings not found in Resources" | Open **Window → AdsIAPKit → Setup Window** once to create the settings |
| AdMob never initializes | Check that `com.google.ads.mobile` is installed as a package and the AdMob app IDs are set in **Assets → Google Mobile Ads → Settings** |
| No ads in the EEA/UK | Publish a consent message in AdMob **Privacy & messaging** |
| Purchase succeeded but nothing was granted | Set `IAPManager.PurchaseHandler` before `Initialize`; the purchase is delivered again on the next launch |
| Verbose logs | Turn on **Verbose Logs** in the Settings tab |

## Known limitations

- Rewarded interstitial and MREC settings exist in the config but are not wired up yet.
- Server-side receipt validation is not implemented.

## License

See [LICENSE.md](LICENSE.md).
