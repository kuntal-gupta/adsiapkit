#if ADSIAPKIT_UGUI
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdsIAPKit.Editor
{
    /// <summary>
    /// Inspector for a custom shop card: fills the fields from child names and points out missing ones,
    /// so a designer can build a card freely and wire it with one click.
    /// </summary>
    [CustomEditor(typeof(ShopProductView))]
    internal class ShopProductViewEditor : UnityEditor.Editor
    {
        // Child names each field looks for, compared without case, spaces, '_' or '-'. Exact names win over partial ones.
        private static readonly (string field, string[] names)[] Objects =
        {
            ("buyButton",         new[] { "buybutton", "buy", "purchasebutton", "purchase", "pricebutton" }),
            ("bonusRoot",         new[] { "bonus", "bonuses", "bonusroot", "extras" }),
            ("bonusItemTemplate", new[] { "bonusitem", "bonustemplate", "bonusitemtemplate" }),
            ("badgeRoot",         new[] { "badge", "ribbon", "badgeroot", "corner" }),
            ("stickerRoot",       new[] { "sticker", "stickerroot" }),
            ("featuredRoot",      new[] { "featured", "featuredroot", "glow", "highlight" }),
            ("ownedRoot",         new[] { "owned", "ownedroot", "purchased", "soldout" }),
        };

        private static readonly (string field, string[] names)[] Texts =
        {
            ("titleText",       new[] { "title", "titletext", "productname", "header" }),
            ("descriptionText", new[] { "description", "desc", "descriptiontext" }),
            ("rewardText",      new[] { "amount", "reward", "rewardtext", "amounttext", "quantity" }),
            ("priceText",       new[] { "price", "pricetext", "pricelabel" }),
        };

        private static readonly string[] IconNames = { "icon", "producticon", "rewardicon" };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var view = (ShopProductView)target;
            EditorGUILayout.Space(8);
            if (GUILayout.Button(new GUIContent("Auto-Assign by Name",
                    "Fills empty fields from child objects named Title, Amount, Price, Icon, Buy Button, Badge, Sticker, Bonus, Owned…"),
                    GUILayout.Height(26)))
            {
                foreach (var t in targets) AutoAssign((ShopProductView)t);
            }

            DrawChecks(view);
        }

        // ── Checks ─────────────────────────────────────────────────────────────
        private void DrawChecks(ShopProductView view)
        {
            var so = new SerializedObject(view);
            bool Missing(string field) => so.FindProperty(field).objectReferenceValue == null;

            if (Missing("buyButton"))
                EditorGUILayout.HelpBox("No Buy Button: the card cannot start a purchase. Assign one, or call Buy() from your own button.", MessageType.Error);
            if (Missing("priceText"))
                EditorGUILayout.HelpBox("No Price Text: players will not see the store's localized price, which the stores require.", MessageType.Warning);
            if (!Missing("bonusRoot") && Missing("bonusItemTemplate"))
                EditorGUILayout.HelpBox("Bonus Root is set but Bonus Item Template is empty, so bonus icons will not show.", MessageType.Warning);

            var stray = so.FindProperty("bonusItemTemplate").objectReferenceValue as GameObject;
            if (stray != null && !stray.transform.IsChildOf(view.transform))
                EditorGUILayout.HelpBox("Bonus Item Template must be a child of this card.", MessageType.Warning);
        }

        // ── Auto-assign ────────────────────────────────────────────────────────
        internal static int AutoAssign(ShopProductView view)
        {
            var so = new SerializedObject(view);
            var children = Descendants(view.transform).ToList();
            var assigned = 0;

            bool Empty(string field) => so.FindProperty(field).objectReferenceValue == null;
            void Set(string field, Object value)
            {
                if (value == null || !Empty(field)) return;
                so.FindProperty(field).objectReferenceValue = value;
                assigned++;
            }

            foreach (var (field, names) in Objects)
            {
                var found = Find(children, names);
                if (found == null) continue;
                // No ?? here: a missing component is a fake-null Unity object in the Editor.
                if (field == "buyButton") Set(field, found.GetComponentInChildren<Button>(true));
                else if (field == "bonusRoot") Set(field, found);
                else Set(field, found.gameObject);
            }
            // A single button anywhere on the card is the Buy button.
            if (Empty("buyButton"))
            {
                var buttons = view.GetComponentsInChildren<Button>(true);
                if (buttons.Length == 1) Set("buyButton", buttons[0]);
            }
            // The first child of the bonus container is the "+ icon" item to copy.
            if (Empty("bonusItemTemplate") && so.FindProperty("bonusRoot").objectReferenceValue is Transform bonus && bonus.childCount > 0)
                Set("bonusItemTemplate", bonus.GetChild(0).gameObject);

            // Ignore what lives inside the bonus item, the badge and the sticker when looking for the main texts and icon.
            var exclude = new[] { "bonusItemTemplate", "badgeRoot", "stickerRoot", "ownedRoot" }
                .Select(f => so.FindProperty(f).objectReferenceValue as GameObject)
                .Where(go => go != null)
                .Select(go => go.transform)
                .ToList();
            var main = children.Where(c => !exclude.Any(c.IsChildOf)).ToList();

            foreach (var (field, names) in Texts)
            {
                var found = Find(main, names);
                if (found == null) continue;
                var text = TextOn(found);
                Set(field, text);

                // "Title" holding a text child is a strip the card can hide; "Amount" holding one has a pill behind it.
                if (text != null && text.transform != found)
                {
                    if (field == "titleText") Set("titleRoot", found.gameObject);
                    if (field == "rewardText") Set("amountBackground", found.GetComponent<Image>());
                }
            }

            if (Empty("priceText") && so.FindProperty("buyButton").objectReferenceValue is Button buy)
                Set("priceText", TextOn(buy.transform));
            if (so.FindProperty("badgeRoot").objectReferenceValue is GameObject badge)
                Set("badgeText", TextOn(badge.transform));
            if (so.FindProperty("stickerRoot").objectReferenceValue is GameObject sticker)
                Set("stickerText", TextOn(sticker.transform));

            var icon = Find(main, IconNames);
            if (icon != null) Set("iconImage", icon.GetComponent<Image>());

            if (assigned > 0) so.ApplyModifiedProperties(); // records Undo
            Debug.Log(assigned > 0
                ? $"[AdsIAPKit] Assigned {assigned} field(s) on {view.name}. Check the Inspector and fill in anything it could not find."
                : $"[AdsIAPKit] Nothing to assign on {view.name}: the fields are already set, or no child names match.", view);
            return assigned;
        }

        /// <summary>Children in breadth-first order, so a card's own Icon is found before an icon nested deeper.</summary>
        private static IEnumerable<Transform> Descendants(Transform root)
        {
            var queue = new Queue<Transform>();
            foreach (Transform child in root) queue.Enqueue(child);
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                yield return next;
                foreach (Transform child in next) queue.Enqueue(child);
            }
        }

        private static Transform Find(List<Transform> candidates, string[] names)
        {
            foreach (var name in names)
                foreach (var c in candidates)
                    if (Normalize(c.name) == name) return c;
            foreach (var name in names)
                foreach (var c in candidates)
                    if (Normalize(c.name).Contains(name)) return c;
            return null;
        }

        private static string Normalize(string name) =>
            new string(name.Where(ch => ch != ' ' && ch != '_' && ch != '-').ToArray()).ToLowerInvariant();

        /// <summary>The text on this object, or the first text below it. Text and TextMeshPro both count.</summary>
        private static Graphic TextOn(Transform t) =>
            t.GetComponentsInChildren<Graphic>(true).FirstOrDefault(g => !(g is Image) && !(g is RawImage));
    }
}
#endif
