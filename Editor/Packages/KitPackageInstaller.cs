using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditorInternal;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Installs and removes packages from the setup window: adds the scoped registries a package
    /// needs to Packages/manifest.json, then asks the Package Manager to add it.
    /// </summary>
    [InitializeOnLoad]
    internal static class KitPackageInstaller
    {
        private static Dictionary<string, string> _installed;
        private static Request _request;
        private static string _busyLabel;

        public static bool   IsBusy    => _request != null;
        public static string BusyLabel => _busyLabel;

        static KitPackageInstaller()
        {
            Events.registeredPackages += _ => _installed = null;
        }

        // ── Queries ────────────────────────────────────────────────────────────
        /// <summary>Installed version of a package, or null when it is not installed.</summary>
        public static string InstalledVersion(string packageId)
        {
            if (_installed == null)
            {
                _installed = new Dictionary<string, string>();
                foreach (var info in PackageInfo.GetAllRegisteredPackages())
                    _installed[info.name] = info.version;
            }
            return _installed.TryGetValue(packageId, out var version) ? version : null;
        }

        public static bool IsInstalled(string packageId) => InstalledVersion(packageId) != null;

        /// <summary>Installed and, when it needs one, with its scripting define set.</summary>
        public static bool IsReady(KitPackage package) =>
            IsInstalled(package.Id) &&
            (string.IsNullOrEmpty(package.RequiredDefine) || IsDefineSet(package.RequiredDefine));

        public static bool IsInstalled(KitAdapter adapter) => adapter.PackageIds.All(IsInstalled);

        // ── Install / remove ───────────────────────────────────────────────────
        public static void Install(KitPackage package) =>
            Install(package.DisplayName, new[] { package.Id }, package.Registries);

        public static void Install(IList<KitPackage> packages)
        {
            if (packages.Count == 0) return;
            Install(string.Join(", ", packages.Select(p => p.DisplayName)),
                packages.Select(p => p.Id).ToArray(),
                packages.SelectMany(p => p.Registries).ToArray());
        }

        public static void Install(KitAdapter adapter, KitRegistry[] registries) =>
            Install(adapter.Network + " adapter", adapter.PackageIds, registries);

        public static void Remove(KitAdapter adapter)
        {
            var installed = adapter.PackageIds.Where(IsInstalled).ToArray();
            if (installed.Length == 0 || IsBusy) return;
            Start("Removing " + adapter.Network + " adapter", Client.AddAndRemove(null, installed));
        }

        private static void Install(string label, string[] packageIds, KitRegistry[] registries)
        {
            if (IsBusy) return;
            try
            {
                KitManifest.AddScopedRegistries(registries);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AdsIAPKit] Could not update {KitManifest.Path}: {exception.Message}");
                return;
            }
            Start("Installing " + label, Client.AddAndRemove(packageIds));
        }

        private static void Start(string label, Request request)
        {
            _busyLabel = label + "…";
            _request   = request;
            EditorApplication.update += Poll;
            Debug.Log("[AdsIAPKit] " + _busyLabel);
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Failure)
            {
                var message = _request.Error?.message ?? "Unknown error";
                Debug.LogError($"[AdsIAPKit] {_busyLabel} failed: {message}");
                EditorUtility.DisplayDialog("AdsIAPKit", $"{_busyLabel.TrimEnd('…')} failed.\n\n{message}", "OK");
            }
            else
            {
                Debug.Log($"[AdsIAPKit] {_busyLabel.TrimEnd('…')} done.");
            }

            _request   = null;
            _busyLabel = null;
            _installed = null;
            InternalEditorUtility.RepaintAllViews();
        }

        // ── Scripting defines ──────────────────────────────────────────────────
        private static readonly NamedBuildTarget[] MobileTargets = { NamedBuildTarget.Android, NamedBuildTarget.iOS };

        public static bool IsDefineSet(string define) => AdsIAPKitSdkChecker.IsScriptingSymbolDefined(define);

        /// <summary>Adds the define to the Android and iOS Scripting Define Symbols.</summary>
        public static void AddDefine(string define)
        {
            foreach (var target in MobileTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                if (defines.Contains(define)) continue;
                PlayerSettings.SetScriptingDefineSymbols(target, defines.Append(define).ToArray());
            }
            Debug.Log($"[AdsIAPKit] Added {define} to the Android and iOS Scripting Define Symbols.");
        }
    }
}
