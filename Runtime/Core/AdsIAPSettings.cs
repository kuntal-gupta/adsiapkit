using UnityEngine;

namespace AdsIAPKit
{
    /// <summary>
    /// Master settings asset — place one instance in Resources/AdsIAPKit/
    /// so both the Editor window and runtime can find it via Resources.Load.
    /// </summary>
    [CreateAssetMenu(fileName = "AdsIAPSettings", menuName = "AdsIAPKit/Settings (Master)")]
    public class AdsIAPSettings : ScriptableObject
    {
        private const string ResourcePath = "AdsIAPKit/AdsIAPSettings";

        [Header("Modules")]
        public AdsConfig adsConfig;
        public IAPConfig iapConfig;

        [Header("Debug")]
        public bool enableVerboseLogs = false;

        // ── Singleton-style loader ──────────────────────────────────────────
        private static AdsIAPSettings _instance;
        public static AdsIAPSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<AdsIAPSettings>(ResourcePath);

                if (_instance == null)
                    Debug.LogWarning("[AdsIAPKit] AdsIAPSettings not found in Resources. " +
                                     "Open Tools > AdsIAPKit to generate it.");
                return _instance;
            }
        }

        // Supports Enter Play Mode without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static void Log(string msg)
        {
            if (Instance != null && Instance.enableVerboseLogs)
                Debug.Log($"[AdsIAPKit] {msg}");
        }
    }
}
