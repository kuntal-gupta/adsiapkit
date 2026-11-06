# Ads & IAP Kit: Setup Guide

This guide takes a project from an empty install to a shipped game with ads, mediation, in-app purchases and a working shop. Each step happens in the setup window (**Tools → AdsIAPKit → Setup Window**) unless it says otherwise.

1. [Install the kit](#1-install-the-kit)
2. [The setup window](#2-the-setup-window)
3. [Ads](#3-ads)
4. [In-app purchases](#4-in-app-purchases)
5. [The shop window](#5-the-shop-window)
6. [Testing](#6-testing)
7. [Release checklist](#7-release-checklist)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Install the kit

**Package Manager → + → Add package from git URL…**

```
https://github.com/kuntal-gupta/adsiapkit.git#v1.3.2
```

Then open **Tools → AdsIAPKit → Setup Window**. The first time, it creates three assets in `Assets/Resources/AdsIAPKit/`:

| Asset | Holds |
|---|---|
| `AdsIAPSettings.asset` | Links the two configs; loaded by the kit at runtime |
| `AdsConfig.asset` | Ad network, ad unit IDs, test mode, consent, ATT |
| `IAPConfig.asset` | Your product catalog |

Commit these assets. They belong to your project, and updating the package never touches them.

You do not install any SDK by hand. The window shows what your settings need and installs it for you (next section).

## 2. The setup window

| Area | What it does |
|---|---|
| Header | Active network and IAP status, and **📖 Guide** opens this page |
| Package banner | Appears when your settings need a package that is missing. **Install Missing Packages** installs all of them |
| **Ads** tab | Network choice, required SDK, mediation adapters, ad unit IDs, consent and ATT |
| **IAP** tab | Required SDK, product catalog, catalog check, shop UI and CSV tools |
| **Settings** tab | Verbose logs, detected defines, SDK documentation links |

### How package installs work

Every package row shows its state:

| Icon | Meaning |
|---|---|
| ✓ and a version | Installed and active |
| ○ and **Install** | Not installed |
| ! and **Enable …** | Installed, but needs a scripting define (IronSource only) |

Pressing **Install**:

1. Adds the package's scoped registry to `Packages/manifest.json` if it comes from outside the Unity registry (OpenUPM for Google, AppLovin's own registry for MAX). Existing registries are kept, and only missing scopes are added.
2. Asks the Package Manager to install the newest version.
3. Unity recompiles. The kit detects the SDK through version defines and switches from its Editor stub to the real SDK.

You can watch progress at the bottom of the panel and in the Console. If an install fails, the window shows the Package Manager's error.

| Feature | Package | Registry | Define set automatically |
|---|---|---|---|
| In-app purchases | `com.unity.purchasing` 5.0+ | Unity | `UNITY_IAP_ENABLED` |
| AdMob | `com.google.ads.mobile` | OpenUPM (`com.google`) | `ADMOB_ENABLED` |
| Unity Ads | `com.unity.ads` 4.0+ | Unity | `UNITY_ADS_ENABLED` |
| AppLovin MAX | `com.applovin.mediation.ads` | AppLovin + OpenUPM | `APPLOVIN_ENABLED` |
| IronSource (LevelPlay) | `com.unity.services.levelplay` | Unity | `IRONSOURCE_ENABLED`: press **Enable** after install |
| iOS ATT prompt | `com.unity.ads.ios-support` | Unity | `ATT_SUPPORT_ENABLED` |

> **IronSource is in preview.** The kit's IronSource provider uses the legacy `IronSource.Agent` API. Recent LevelPlay releases are moving away from it, so check that your LevelPlay version still includes that API before you enable `IRONSOURCE_ENABLED`.

---

## 3. Ads

### 3.1 Choose a network

**Ads → General Settings → Active Provider.** As soon as you pick one, the **Required for …** panel shows its SDK with an **Install** button.

| Setting | Use |
|---|---|
| Test Mode | Uses test ad units. Keep it on until release |
| Initialize On Start | Starts ads when the first scene loads, with no scene setup |
| GDPR Consent (UMP) | Shows Google's consent form in the EEA/UK before ads load (AdMob) |

Then open the network's section (for example **🟠 AdMob**), turn it on, and enter your app ID and ad unit IDs for Android and iOS.

**AdMob only:** also enter the app IDs in **Assets → Google Mobile Ads → Settings**. Google's SDK reads them from there at build time.

### 3.2 Mediation

Mediation lets several ad networks compete for each ad request, so you earn more per impression. The kit's code does not change: you install an **adapter** per network, then configure the networks in the network's dashboard. The SDK does the rest.

Open **Ads → 🔀 Mediation**. What you see depends on the active network.

#### AdMob mediation

1. Install the Google Mobile Ads SDK (the row above).
2. Press **Install** next to each network you want. Adapters come from Google on OpenUPM (`com.google.ads.mobile.mediation.<network>`) and include Android and iOS.

   Available: AppLovin, Unity Ads, IronSource, Meta Audience Network, Liftoff Monetize, Mintegral, Pangle, InMobi, DT Exchange, Chartboost, Moloco, myTarget.
3. In the **AdMob console → Mediation → Create mediation group**, create one group per ad format (banner, interstitial, rewarded). Add your ad units, then add each network as a bidding or waterfall source with the IDs from that network's dashboard.
4. Test on a device (section 3.4).

#### AppLovin MAX mediation

1. Install AppLovin MAX.
2. Press **Install** next to each network. The kit installs the Android **and** iOS adapter packages (`com.applovin.mediation.adapters.<network>.android` / `.ios`). A row marked **!** has only one of them installed; press **Complete**.
3. You can also use **Open AppLovin Integration Manager** to manage the same adapters through AppLovin's tool.
4. In the **MAX dashboard → Mediation → Manage → Ad Units**, turn on each network for each ad unit.

#### LevelPlay (IronSource) mediation

LevelPlay installs adapters with its own Network Manager. Press **Open LevelPlay Network Manager**; if your LevelPlay version uses a different menu, the button opens LevelPlay's documentation instead. Then turn the networks on in the LevelPlay dashboard.

#### Unity Ads

Unity Ads is a single network with no mediation. To mediate, choose AdMob, AppLovin MAX or LevelPlay as the active network. Unity Ads can still join as an adapter in those.

#### Privacy and mediation

**GDPR Consent (UMP)** stores the player's choice as an IAB TCF consent string on the device, and networks that support TCF read it from there. Networks that do not, and US state privacy or COPPA rules, can need their own calls. Check each adapter's documentation before release.

### 3.3 Show ads from code

```csharp
using AdsIAPKit;

AdsManager.Instance.ShowBanner();

AdsManager.Instance.ShowInterstitial(() => LoadNextLevel());

if (AdsManager.Instance.IsRewardedReady())
    AdsManager.Instance.ShowRewarded(rewarded => { if (rewarded) GiveCoins(50); });
```

Add a **Privacy settings** button to your settings screen when consent is on. Google requires one:

```csharp
privacyButton.SetActive(AdsManager.Instance.IsPrivacyOptionsRequired);
privacyButton.onClick.AddListener(() => AdsManager.Instance.ShowPrivacyOptions());
```

### 3.4 Check mediation on a device

Add a hidden debug button to a test build:

```csharp
AdsManager.Instance.OpenAdDebugger();
```

This opens AdMob's **Ad Inspector** or AppLovin's **Mediation Debugger**. They list each network, whether its adapter loaded, and let you request test ads from each one. For AdMob, register your device as a test device in the AdMob console first.

### 3.5 iOS App Tracking Transparency

**Ads → 🔒 iOS App Tracking Transparency.** With **Request ATT** on, install the **iOS 14 Advertising Support** package from the row that appears. The kit asks for permission at startup and writes your **Usage Description** to `Info.plist` during the iOS build.

---

## 4. In-app purchases

### 4.1 Product types

| Type | Bought | Restorable | Use for |
|---|---|---|---|
| **Consumable** | Again and again | No | Coins, gems, hints, lives |
| **Non-Consumable** | Once | Yes | Remove Ads, level packs, permanent unlocks |
| **Subscription** | Renews until cancelled | Yes | VIP, premium, season pass |

### 4.2 Add products

**IAP tab → Enable IAP**, install **Unity In-App Purchasing 5** from the row below it, then **+ Add Product** and choose a template:

| Template | Creates |
|---|---|
| Coin Pack | Consumable, Currency reward `coins` × 500, category `Coins` |
| Remove Ads | Non-Consumable, Remove Ads reward, `POPULAR` badge |
| Unlock Feature | Non-Consumable, Feature reward `unlock_all` |
| Item Pack | Consumable, Item reward `hint` × 10, category `Items` |
| Subscription | Subscription, Feature reward `vip`, featured |
| Empty Product | A blank product |

Templates build the ID from your bundle identifier (**Player Settings → Identification**), for example `com.puzzlecraft.wordfinder.coins_small`. Edit everything afterwards.

Each product card has four groups:

**Identity**

| Field | Meaning |
|---|---|
| Product ID | Your ID for the product. Code, the shop and your purchase handler use it. **Suggest** builds one from the display name |
| Display Name | Card title |
| Description | Card text |

**Store**

| Field | Meaning |
|---|---|
| Type | Consumable, Non-Consumable or Subscription; must match the store |
| Google Play ID / App Store ID | The ID in each store. Leave blank to use the Product ID. The line below shows the ID each store will receive |
| Fallback Price | Shown **in the Editor only**. On a device the shop always shows the store's localized price (`₹89.00`, `€0,99`…) |

**Reward**: what your purchase handler grants. The fields change with the type:

| Reward Type | Fields | Typical handling |
|---|---|---|
| Currency | Currency ID, Amount | `wallet.Add(rewardId, rewardAmount)` |
| Item | Item ID, Amount | `inventory.Add(rewardId, rewardAmount)` |
| Remove Ads | – | Save it, then `AdsManager.Instance.DestroyBanner()` |
| Feature | Feature ID | `unlocks.Set(rewardId, true)` |
| Custom | Reward ID, Amount | Anything your handler understands |

**Shop**

| Field | Meaning |
|---|---|
| Icon | Reward icon at the left of the row, e.g. a coin stack |
| Category | Groups products into shop tabs, e.g. `Coins`, `Items`, `Bundles` |
| Featured | Highlights the row; shows **BEST VALUE** unless a badge is set |
| Badge | Corner ribbon text, e.g. `20% MORE`, `POPULAR`, `x2` |
| Sticker | Small top-left sticker, e.g. `HOT`, `NEW` |
| Show Title | Shows the display name as a header strip above the row, e.g. `Mega Pack` |
| Bonus Icons | Icons shown after the amount as **+ icon**, e.g. a no-ads icon on a coin bundle. Display only: your purchase handler grants the bonus |
| Card Override | Optional card prefab for this product only, e.g. a big special-offer card. Empty = the shop's Card Template (see 5.6) |
| Hide In Shop | Purchasable from code (e.g. a level-end offer) but not listed in the shop |

The header buttons **▲ ▼** set the order of cards in the shop, **⧉** duplicates and **✕** removes. The checkbox enables or disables a product; disabled products are not sent to the store. Undo (Ctrl/Cmd+Z) works for every change.

### 4.3 Create the products in the stores

The catalog only describes products. Each one must also exist in the store with **the same ID and type**.

**Google Play Console**

1. Upload a build with the Billing permission to a testing track (internal testing is enough). Play only allows products after the app has a build.
2. **Monetize → Products → In-app products → Create product.** Product ID = the card's **Google Play ID**. Set the name, description and price, then **Activate**.
3. Subscriptions: **Monetize → Products → Subscriptions**. Create the subscription with the same ID, then add a base plan.
4. **Setup → License testing**: add your testers' Google accounts so they can buy without being charged.

Google Play IDs must start with a lowercase letter or number and use only `a-z`, `0-9`, `_` and `.`. They cannot be reused after deletion.

**App Store Connect**

1. Sign the **Paid Apps** agreement and fill in banking and tax (**Business**). Products stay unavailable until this is done.
2. **Your app → Monetization → In-App Purchases → +**. Choose the type, enter the **App Store ID** as Product ID, add a price and a localized display name. Add a review screenshot.
3. Subscriptions: **Monetization → Subscriptions**. Create a group, then the subscription.
4. **Users and Access → Sandbox → Testers**: create sandbox accounts for testing.

Use the same ID in both stores when you can. Then you only fill in the Product ID and leave the store IDs blank.

### 4.4 Catalog check

The IAP tab checks the catalog on every change and lists problems above the products. **Show** opens the product.

| Severity | Examples |
|---|---|
| ⛔ Error (the store will reject it, or players lose purchases) | Empty or duplicate IDs; a Google Play ID with uppercase letters or spaces; Remove Ads set to Consumable |
| ⚠ Warning (works, but probably a mistake) | `com.company` placeholder ID; no display name; reward amount 0; empty reward ID; Currency that is not Consumable |

Fix every error before you build. From code you can run the same check with `IAPConfig.Validate()`.

### 4.5 Grant purchases

The kit never decides what a purchase gives. You set one **purchase handler** that grants the product and returns `true` once it is saved. Only then does the kit confirm the purchase with the store. If the handler returns `false` or throws, the purchase stays pending and the store delivers it again on the next launch, so a crash can never lose a player's purchase.

Set the handler in `Awake` of an object in your first scene, before IAP initializes:

```csharp
using AdsIAPKit;
using UnityEngine;

public class Purchases : MonoBehaviour
{
    private void Awake()
    {
        IAPManager.PurchaseHandler = Grant;
        IAPManager.Instance.InitializeFromSettings();
    }

    private bool Grant(string productId)
    {
        var product = AdsIAPSettings.Instance.iapConfig.GetProduct(productId);
        if (product == null) return false;           // unknown product: leave it pending

        switch (product.rewardType)
        {
            case RewardType.Currency:
                Wallet.Add(product.rewardId, product.rewardAmount);
                break;
            case RewardType.Item:
                Inventory.Add(product.rewardId, product.rewardAmount);
                break;
            case RewardType.RemoveAds:
                PlayerPrefs.SetInt("no_ads", 1);
                AdsManager.Instance.DestroyBanner();
                break;
            case RewardType.Feature:
                PlayerPrefs.SetInt("feature_" + product.rewardId, 1);
                break;
            default:
                return false;
        }
        PlayerPrefs.Save();
        return true;                                  // saved: the kit confirms with the store
    }
}
```

Other useful calls:

| Call | Use |
|---|---|
| `IAPManager.Instance.Purchase(id, (ok, error) => …)` | Buy from your own UI |
| `IAPManager.Instance.GetLocalizedPrice(id)` | Store price string |
| `IAPManager.Instance.IsAvailable(id)` | The store returned the product |
| `IAPManager.Instance.IsPurchased(id)` | Owned non-consumable |
| `IAPManager.Instance.RestorePurchases()` | Restore button (required on iOS) |

### 4.6 CSV import and export

**Tools → Export CSV** writes the whole catalog, so you can edit it in a spreadsheet or share it with a designer. **Import CSV** reads it back: rows are matched by `ID`, existing products are updated, new IDs are added, and icons are kept. Columns:

```
ID, DisplayName, Description, Type, GooglePlayID, AppStoreID, FallbackPrice,
RewardType, RewardID, RewardAmount, Category, Badge, Sticker, ShowTitle,
Featured, Enabled, HideInShop
```

Icons, bonus icons and card overrides are assets, so CSV leaves them unchanged.

Only `ID` is required; missing columns leave fields unchanged.

---

## 5. The shop window

The kit includes uGUI components that build the shop from your catalog. Add, reorder or change products in the IAP tab and the shop follows. No code is needed.

### 5.1 Create a shop

**IAP tab → Shop UI → Create Shop Catalog**, or **GameObject → AdsIAPKit → Shop Catalog**. The kit uses the selected Canvas, or creates a Canvas (1080 × 1920, scale with screen) and an EventSystem for your input system. It builds:

```
Shop                     ← ShopCatalogView
├── Header → Title, Close Button (hides the shop)
├── Scroll View
│   └── Viewport
│       └── Content      ← Vertical Layout Group: one row per product
│           └── Card Template (hidden at runtime)   ← ShopProductView
│               ├── Title (strip, only with Show Title)
│               ├── Row
│               │   ├── Icon
│               │   ├── Amount (pill) → Text
│               │   ├── Bonus → Bonus Item (+ Icon), copied per bonus icon
│               │   └── Buy Button → Text (price)
│               ├── Badge → Ribbon → Text (corner ribbon)
│               ├── Sticker → Text
│               └── Owned → Text
└── Restore Button (underlined link)
```

Every product is one full-width row, the usual layout for mobile game shops:

```
┌──────────────────────────────────────────────────────╲20%╲
│ (HOT)                                                    │
│  [coins]  ( 2880 )  + [no-ads]          [ BDT 1,200.00 ] │
└──────────────────────────────────────────────────────────┘
┌─────────────────── Mega Pack ────────────────────────────┐   ← Show Title
│  [coins]  ( 16000 ) + [no-ads] + [hint] [ BDT 6,000.00 ] │
└──────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────┐
│  [no-ads]  Remove Ads                   [   BDT 700.00 ] │   ← name instead of an amount
└──────────────────────────────────────────────────────────┘
                          Restore
```

Press Play. You get one row per product in catalog order, with Editor fallback prices and working Buy buttons through the Editor stub. The generated art is plain colored shapes; drop your game's sprites onto the card's Images (background, pill, ribbon, sticker, button) to match your style.

It uses TextMeshPro when your project has it (Unity 6, or the TextMeshPro package), and legacy Text otherwise. TextMeshPro asks to import its essentials the first time.

### 5.2 How a product becomes a card

`ShopCatalogView` copies **Card Template** once for each product that is **enabled**, not **Hide In Shop**, and in the view's **Category** (blank = all). Then `ShopProductView` fills each card:

| Card element | Comes from |
|---|---|
| Title strip | Display Name; shown only with **Show Title** |
| Amount | Reward Amount in a pill for Currency/Item (`2,880`); the Display Name without the pill for Remove Ads, unlocks and subscriptions |
| Icon | Icon (hidden when empty) |
| Bonus | One **+ icon** per Bonus Icon; hidden when there are none |
| Corner ribbon | Badge, or **BEST VALUE** when Featured; hidden when neither |
| Sticker | Sticker; hidden when empty |
| Price (on the Buy button) | The store's localized price |
| Owned overlay | Shown when a Non-Consumable is owned |

#### Example: the bundle rows above

| Product | Reward | Badge | Sticker | Bonus Icons |
|---|---|---|---|---|
| 720 coins | Currency `coins` × 720 | | | |
| 2880 coins + no ads | Currency `coins` × 2880 | `20% MORE` | | no-ads sprite |
| 6000 coins + no ads | Currency `coins` × 6000 | `30% MORE` | `HOT` | no-ads sprite |
| Remove Ads | Remove Ads (Non-Consumable) | | | |

For the bundles, your `PurchaseHandler` adds the coins **and** turns off ads (see 4.5): the bonus icon only shows it.

### 5.3 Card states

| State | Price label | Buy button |
|---|---|---|
| Store still connecting (device) | `…` | Disabled |
| Store still connecting (Editor) | Fallback Price | Disabled until the stub is ready |
| Ready | `$0.99`, `₹89.00`… | Enabled |
| Purchase in progress | Price | Disabled |
| Owned (Non-Consumable) | `Owned` | Disabled, overlay shown |
| Not returned by the store | `Unavailable` | Disabled |

Cards refresh themselves when the store connects, after each purchase and after a restore. The labels `Owned`, `…` and `Unavailable` are fields on `ShopProductView`, so you can translate them.

"Unavailable" on a device almost always means the product ID or type does not match the store, the product is not active, or (on iOS) the Paid Apps agreement is not signed.

### 5.4 Restore button

Apple requires a way to restore purchases. **Restore Button** calls `IAPManager.RestorePurchases()`. With **Show Restore Only On iOS** on (the default), the button is hidden on Android, where purchases restore automatically.

### 5.5 Tabs by category

Put several `ShopCatalogView`s on different tab pages, each with its own **Category**, or switch one view from code:

```csharp
[SerializeField] private ShopCatalogView shop;

public void ShowCoins() => shop.Category = "Coins";
public void ShowItems() => shop.Category = "Items";
public void ShowAll()   => shop.Category = "";
```

Turn on **Featured First** to move featured products to the top.

### 5.6 Use your own design

The product data always comes from the catalog; the card is only how it looks. There are three levels.

**Restyle the generated card.** Select **Shop → … → Content → Card Template** and change it: put your sprites on its Images (background, pill, ribbon, sticker, Buy button), change fonts, colors and sizes, move or delete parts. Every row at runtime is a copy of the template, so one change restyles the whole shop. Deleting a part is safe: its field becomes empty and the card simply does without it.

**Build your own card.**

1. Build the card however you like: any layout, any art, a vertical tile instead of a row.
2. Add **Shop Product View** to its root.
3. Click **Auto-Assign by Name** in its Inspector. It fills the empty fields from child names:

   | Field | Child named (case, spaces and `_` ignored) |
   |---|---|
   | Buy Button | `Buy Button`, `Buy`, `Purchase` (or the card's only Button) |
   | Price Text | `Price`, or the text inside the Buy Button |
   | Reward Text | `Amount`, `Reward`, `Quantity`; an Image on that object becomes the pill |
   | Title Text | `Title`, `Header`, `Product Name`; a `Title` object holding a text becomes the strip hidden unless **Show Title** |
   | Description Text | `Description`, `Desc` |
   | Icon Image | `Icon`, `Product Icon`, `Reward Icon` |
   | Badge Root / Text | `Badge`, `Ribbon`, `Corner`, and the text inside |
   | Sticker Root / Text | `Sticker`, and the text inside |
   | Bonus Root / Item Template | `Bonus` (or `Bonuses`, `Extras`) and `Bonus Item`, or its first child |
   | Featured Root | `Featured`, `Glow`, `Highlight` |
   | Owned Root | `Owned`, `Purchased`, `Sold Out` |

   It never overwrites a field you set, and Undo reverts it. Fill in anything it missed by dragging. The Inspector warns when the card has no Buy Button or no Price Text.
4. Save it as a prefab, or keep it as a disabled child of the content, and drag it into **Card Template** on `ShopCatalogView`.

Text fields accept `Text` or `TextMeshProUGUI`, and every field is optional.

**A different card for one product.** Build a second card the same way, save it as a prefab, and drag it into that product's **Card Override** in the IAP tab. That product uses its own card, e.g. a big gift bundle, while the rest keep the template. The override prefab needs a **Shop Product View**; the IAP tab warns if it has none.

**Featured Root** is an extra object shown only for featured products (a glow or frame, for example). **Title Root**, **Sticker Root**, **Badge Root** and **Bonus Root** are hidden automatically when the product does not use them; leave **Title Root** empty to always show the title, e.g. on a vertical card.

### 5.7 A single product anywhere

**GameObject → AdsIAPKit → Shop Product Card** creates one card. Pick its product from the **Product Id** dropdown, which lists your catalog. Use it for a Remove Ads button in your settings screen or a special offer pop-up.

### 5.8 React to purchases

Each `ShopProductView` has two UnityEvents:

| Event | When |
|---|---|
| `onPurchased (string productId)` | After your purchase handler granted the product. Play a sound, update the coin counter, close the pop-up |
| `onPurchaseFailed (string productId, string message)` | The player cancelled or the store failed. Show a message |

Granting itself always happens in your `PurchaseHandler` (section 4.5), never in these events, so the reward is given even when the shop is closed.

---

## 6. Testing

### In the Editor

Without an SDK installed, or in the Editor, the kit runs stubs so you can click through every flow:

- Ads are always "ready", and rewarded ads always reward.
- Purchases call your purchase handler and succeed. Prices show the Fallback Price.

In a build, a missing SDK reports "not initialized", so nothing is ever granted for free.

### Ads on a device

- Keep **Test Mode** and **Use Google Test IDs** on. Clicking real ads on your own app can get your AdMob account suspended.
- For mediation, register your device as a test device and use `OpenAdDebugger()` (section 3.4).

### Purchases on Android

1. Upload the build to **Internal testing** and install it from the Play Store link. Sideloaded builds with a different signature cannot buy.
2. Use a Google account listed under **License testing**. Test purchases show "Test card, always approves".
3. Purchases can take a few hours to work after you create a product.

### Purchases on iOS

1. Sign in with a **sandbox tester** on the device: **Settings → App Store → Sandbox Account**.
2. Run a development or TestFlight build. In sandbox, subscriptions renew every few minutes.

---

## 7. Release checklist

- [ ] **Test Mode** off; real ad unit IDs entered for both platforms
- [ ] AdMob app IDs set in **Assets → Google Mobile Ads → Settings**
- [ ] A consent message published in AdMob **Privacy & messaging**, and a Privacy settings button in your game
- [ ] Mediation groups (AdMob) or network settings (MAX/LevelPlay) set up for every installed adapter
- [ ] `app-ads.txt` published on your developer website
- [ ] The IAP catalog check shows no errors
- [ ] Every enabled product exists and is active in both stores with the same ID and type
- [ ] `PurchaseHandler` set before IAP initializes, and saves before returning `true`
- [ ] Restore button visible on iOS
- [ ] ATT usage description written in your own words (iOS)
- [ ] Tested a real purchase with a license tester (Android) and a sandbox tester (iOS)

---

## 8. Troubleshooting

| Problem | Fix |
|---|---|
| **Install** fails with "unable to find package" | The registry is unreachable or blocked by a proxy. Check `scopedRegistries` in `Packages/manifest.json`, then **Window → Package Manager → ⋮ → Refresh** |
| Package installed but the window still says missing | Wait for the recompile to finish. For IronSource press **Enable IRONSOURCE_ENABLED** |
| Ads never initialize on a device | AdMob app IDs missing from Google Mobile Ads Settings, or consent not given (check the log with Verbose Logs on) |
| A mediation network never fills | The adapter is missing (check `OpenAdDebugger()`), or the network is not in a mediation group / not turned on in the dashboard |
| Shop shows `…` forever on a device | IAP did not initialize: Unity IAP not installed, IAP disabled, or no internet. With **Initialize IAP** off on the shop, call `IAPManager.Instance.InitializeFromSettings()` yourself |
| Card shows `Unavailable` | The product's ID or type does not match the store, or it is not active (see 5.3) |
| Purchase succeeded but nothing was granted | `PurchaseHandler` was not set, or returned `false`. The purchase is delivered again on the next launch |
| Shop is empty | No enabled products, all are **Hide In Shop**, or **Category** does not match any product |
| Card text is blank with TextMeshPro | Import TMP Essentials: **Window → TextMeshPro → Import TMP Essential Resources** |

More detail on the API is in the [README](../README.md).
