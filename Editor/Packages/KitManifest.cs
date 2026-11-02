using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    /// <summary>Edits the scoped registries in Packages/manifest.json.</summary>
    internal static class KitManifest
    {
        public const string Path = "Packages/manifest.json";

        /// <summary>Adds each registry to the manifest, or its missing scopes when it is already there.</summary>
        internal static bool AddScopedRegistries(IEnumerable<KitRegistry> registries)
        {
            var wanted = registries.Where(r => r != null).ToList();
            if (wanted.Count == 0) return false;

            var manifest = MiniJson.Parse(File.ReadAllText(Path)) as Dictionary<string, object>
                ?? throw new InvalidDataException("manifest.json is not a JSON object.");
            if (!MergeScopedRegistries(manifest, wanted)) return false;

            File.WriteAllText(Path, MiniJson.Serialize(manifest) + "\n");
            Debug.Log("[AdsIAPKit] Added scoped registries to " + Path + ": " +
                      string.Join(", ", wanted.Select(r => r.Name).Distinct()));
            return true;
        }

        /// <summary>Merges registries into a parsed manifest. Returns true when it changed.</summary>
        internal static bool MergeScopedRegistries(Dictionary<string, object> manifest, IEnumerable<KitRegistry> registries)
        {
            if (!(manifest.TryGetValue("scopedRegistries", out var value) && value is List<object> list))
            {
                list = new List<object>();
                manifest["scopedRegistries"] = list;
            }

            var changed = false;
            foreach (var registry in registries)
            {
                var entry = list.OfType<Dictionary<string, object>>()
                    .FirstOrDefault(r => SameUrl(r.TryGetValue("url", out var url) ? url as string : null, registry.Url));

                if (entry == null)
                {
                    list.Add(new Dictionary<string, object>
                    {
                        { "name",   registry.Name },
                        { "url",    registry.Url },
                        { "scopes", registry.Scopes.Cast<object>().ToList() }
                    });
                    changed = true;
                    continue;
                }

                if (!(entry.TryGetValue("scopes", out var scopesValue) && scopesValue is List<object> scopes))
                {
                    scopes = new List<object>();
                    entry["scopes"] = scopes;
                }
                foreach (var scope in registry.Scopes)
                {
                    if (scopes.OfType<string>().Any(s => s == scope)) continue;
                    scopes.Add(scope);
                    changed = true;
                }
            }
            return changed;
        }

        private static bool SameUrl(string a, string b) =>
            a != null && string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}
