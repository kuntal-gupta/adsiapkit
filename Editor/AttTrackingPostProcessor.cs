#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace AdsIAPKit.Editor
{
    internal static class AttTrackingPostProcessor
    {
        [PostProcessBuild(900)]
        private static void AddTrackingUsageDescription(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;

            var settings = UnityEngine.Resources.Load<AdsIAPSettings>("AdsIAPKit/AdsIAPSettings");
            var config = settings != null ? settings.adsConfig : null;
            if (config == null || !config.enableATT) return;

            string plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath)) return;

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", config.attDescription);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
