using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Switches Unity IAP's Editor fake store between asking with a Buy/Cancel dialog and approving
    /// every purchase at once, through the IAP_FAKE_STORE_DEFAULT define. Real store builds are unaffected:
    /// the fake store only runs in the Editor.
    /// </summary>
    public static class IAPTestPurchases
    {
        public const string Define = "IAP_FAKE_STORE_DEFAULT";

        private static readonly NamedBuildTarget[] Targets =
            { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone };

        /// <summary>True when the define is set for the active build target.</summary>
        public static bool AutoApprove
        {
            get
            {
                var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), out var defines);
                return defines.Contains(Define);
            }
            set
            {
                foreach (var target in Targets)
                {
                    PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                    bool has = defines.Contains(Define);
                    if (value == has) continue;
                    PlayerSettings.SetScriptingDefineSymbols(target,
                        value ? defines.Append(Define).ToArray() : defines.Where(d => d != Define).ToArray());
                }
            }
        }
    }
}
