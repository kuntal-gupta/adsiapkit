using System;
using UnityEngine;

namespace AdsIAPKit
{
    public interface IAdsProvider
    {
        string ProviderName { get; }
        bool   IsInitialized { get; }

        // Lifecycle
        void Initialize(AdsConfig config);

        // Banner
        void ShowBanner();
        void HideBanner();
        void DestroyBanner();

        // Interstitial
        bool IsInterstitialReady();
        void LoadInterstitial();
        void ShowInterstitial(Action onClosed = null);

        // Rewarded
        bool IsRewardedReady();
        void LoadRewarded();
        void ShowRewarded(Action<bool> onResult);
    }
}
