using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace AdsIAPKit.Editor
{
    internal static class AdsIAPKitSdkChecker
    {
        internal sealed class Result
        {
            public readonly List<string> Missing = new List<string>();
            public readonly List<string> Installed = new List<string>();
            public bool IsReady => Missing.Count == 0;
        }

        internal static Result Check(AdsIAPSettings settings)
        {
            var result = new Result();
            if (settings == null) return result;

            if (settings.iapConfig != null && settings.iapConfig.enableIAP)
                CheckPackage(result, "com.unity.purchasing", "Unity IAP");

            var ads = settings.adsConfig;
            if (ads == null || ads.activeProvider == AdsProviderType.None) return result;

            switch (ads.activeProvider)
            {
                case AdsProviderType.AdMob:
                    CheckPackage(result, "com.google.ads.mobile", "Google Mobile Ads / AdMob");
                    break;
                case AdsProviderType.UnityAds:
                    CheckPackage(result, "com.unity.ads", "Unity Ads");
                    break;
                case AdsProviderType.IronSource:
                    CheckDefine(result, "IRONSOURCE_ENABLED", "IronSource / LevelPlay");
                    break;
                case AdsProviderType.AppLovin:
                    if (UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.applovin.mediation.ads") != null)
                        result.Installed.Add("AppLovin MAX");
                    else
                        CheckDefine(result, "APPLOVIN_ENABLED", "AppLovin MAX");
                    break;
            }

            return result;
        }

        internal static void ShowSetupPrompt(Result result)
        {
            if (result == null || result.IsReady) return;

            string missing = string.Join("\n", result.Missing);
            bool open = EditorUtility.DisplayDialog(
                "AdsIAPKit SDK Setup Required",
                "The following SDKs are required by your current AdsIAPKit settings but were not detected:\n\n" + missing +
                "\n\nInstall or import them, then reopen this window to check again.",
                "Open Package Manager", "Later");

            if (open)
                EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        private static void CheckPackage(Result result, string packageName, string displayName)
        {
            if (UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName) != null)
                result.Installed.Add(displayName);
            else
                result.Missing.Add(displayName + " (" + packageName + ")");
        }

        private static void CheckDefine(Result result, string define, string displayName)
        {
            if (IsScriptingSymbolDefined(define)) result.Installed.Add(displayName);
            else result.Missing.Add(displayName + " (import plugin and enable " + define + ")");
        }

        /// <summary>True when the symbol is in the active build target's Scripting Define Symbols.</summary>
        internal static bool IsScriptingSymbolDefined(string define)
        {
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
            foreach (var symbol in symbols.Split(';'))
                if (symbol.Trim() == define) return true;
            return false;
        }
    }
}
