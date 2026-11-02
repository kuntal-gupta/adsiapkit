using UnityEngine;

#if UNITY_IOS && ATT_SUPPORT_ENABLED
using Unity.Advertisement.IosSupport;
#endif

namespace AdsIAPKit
{
    /// <summary>
    /// Owns the iOS App Tracking Transparency request for AdsIAPKit.
    /// Requires the "com.unity.ads.ios-support" package; without it the request is skipped.
    /// </summary>
    public static class AttTracking
    {
        public static void RequestIfNeeded(AdsConfig config)
        {
#if UNITY_IOS && ATT_SUPPORT_ENABLED
            if (config == null || !config.enableATT || Application.platform != RuntimePlatform.IPhonePlayer)
                return;

            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
            }
#endif
        }
    }
}
