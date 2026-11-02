using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    /// <summary>Package and mediation adapter rows shared by the setup window's tabs.</summary>
    internal static class KitPackageGUI
    {
        private static readonly Color Green  = new Color(0.30f, 0.80f, 0.45f);
        private static readonly Color Orange = new Color(1.00f, 0.65f, 0.20f);

        private static GUIStyle _wrapMini;
        private static GUIStyle WrapMini => _wrapMini ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        /// <summary>A box listing the packages with an install button each, and one for all missing ones.</summary>
        public static void DrawPanel(string title, IList<KitPackage> packages)
        {
            if (packages.Count == 0) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(title, EditorStyles.boldLabel);
                foreach (var package in packages) DrawRow(package);

                var missing = packages.Where(p => !KitPackageInstaller.IsInstalled(p.Id)).ToList();
                if (missing.Count > 1)
                {
                    GUILayout.Space(2);
                    using (new EditorGUI.DisabledScope(KitPackageInstaller.IsBusy))
                        if (GUILayout.Button($"Install All Missing ({missing.Count})", GUILayout.Height(24)))
                            KitPackageInstaller.Install(missing);
                }
                DrawBusy();
            }
        }

        public static void DrawRow(KitPackage package)
        {
            var version = KitPackageInstaller.InstalledVersion(package.Id);
            var ready   = KitPackageInstaller.IsReady(package);

            using (new EditorGUILayout.HorizontalScope())
            {
                StatusIcon(ready, version != null);
                GUILayout.Label(package.DisplayName, EditorStyles.label);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(KitPackageInstaller.IsBusy))
                {
                    if (version == null)
                    {
                        if (GUILayout.Button("Install", GUILayout.Width(70)))
                            KitPackageInstaller.Install(package);
                    }
                    else if (!ready)
                    {
                        if (GUILayout.Button("Enable " + package.RequiredDefine, GUILayout.Width(170)) &&
                            EditorUtility.DisplayDialog("AdsIAPKit",
                                $"Add {package.RequiredDefine} to the Android and iOS Scripting Define Symbols?\n\n{package.Note}",
                                "Add", "Cancel"))
                            KitPackageInstaller.AddDefine(package.RequiredDefine);
                    }
                    else
                    {
                        GUILayout.Label("v" + version, EditorStyles.miniLabel, GUILayout.Width(70));
                    }
                }

                if (GUILayout.Button("Docs", EditorStyles.miniButton, GUILayout.Width(42)))
                    Application.OpenURL(package.DocsUrl);
            }

            var detail = package.Id + (string.IsNullOrEmpty(package.Note) ? "" : "  ·  " + package.Note);
            GUILayout.Label(detail, WrapMini);
        }

        /// <summary>One row per mediation network with Install / Remove.</summary>
        public static void DrawAdapters(IList<KitAdapter> adapters, KitRegistry[] registries)
        {
            foreach (var adapter in adapters)
            {
                var installed = KitPackageInstaller.IsInstalled(adapter);
                var partial   = !installed && adapter.PackageIds.Any(KitPackageInstaller.IsInstalled);

                using (new EditorGUILayout.HorizontalScope())
                {
                    StatusIcon(installed, partial);
                    GUILayout.Label(adapter.Network);
                    GUILayout.FlexibleSpace();

                    if (installed)
                        GUILayout.Label("v" + KitPackageInstaller.InstalledVersion(adapter.PackageIds[0]),
                            EditorStyles.miniLabel, GUILayout.Width(110));

                    using (new EditorGUI.DisabledScope(KitPackageInstaller.IsBusy))
                    {
                        if (!installed && GUILayout.Button(partial ? "Complete" : "Install", GUILayout.Width(70)))
                            KitPackageInstaller.Install(adapter, registries);
                        if ((installed || partial) && GUILayout.Button("Remove", GUILayout.Width(70)) &&
                            EditorUtility.DisplayDialog("AdsIAPKit", $"Remove the {adapter.Network} adapter?", "Remove", "Cancel"))
                            KitPackageInstaller.Remove(adapter);
                    }
                }
            }
            DrawBusy();
        }

        public static void DrawBusy()
        {
            if (!KitPackageInstaller.IsBusy) return;
            EditorGUILayout.HelpBox(KitPackageInstaller.BusyLabel + " Unity recompiles when it finishes.", MessageType.None);
        }

        private static void StatusIcon(bool ready, bool partial)
        {
            var previous = GUI.contentColor;
            GUI.contentColor = ready ? Green : partial ? Orange : Color.gray;
            GUILayout.Label(ready ? "✓" : partial ? "!" : "○", EditorStyles.boldLabel, GUILayout.Width(16));
            GUI.contentColor = previous;
        }
    }
}
