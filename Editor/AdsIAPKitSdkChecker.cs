using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace AdsIAPKit.Editor
{
    internal static class AdsIAPKitSdkChecker
    {
        internal sealed class Result
        {
            public readonly List<KitPackage> Missing = new List<KitPackage>();
            public readonly List<KitPackage> Installed = new List<KitPackage>();
            public bool IsReady => Missing.Count == 0;
        }

        /// <summary>Checks the packages the active ad provider and IAP need.</summary>
        internal static Result Check(AdsIAPSettings settings)
        {
            var result = new Result();
            foreach (var package in KitPackages.Required(settings))
            {
                if (KitPackageInstaller.IsReady(package)) result.Installed.Add(package);
                else result.Missing.Add(package);
            }
            return result;
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
