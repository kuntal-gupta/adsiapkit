using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    public abstract class BaseTab
    {
        public abstract string TabName { get; }
        public abstract string TabIcon { get; }

        protected AdsIAPSettings Settings;
        protected bool IsDirty;

        public virtual void OnEnable(AdsIAPSettings settings)
        {
            Settings = settings;
        }

        public abstract void Draw();

        protected void MarkDirty()
        {
            if (Settings != null) EditorUtility.SetDirty(Settings);
            IsDirty = true;
        }

        // ── Shared style helpers ───────────────────────────────────────────────
        protected static GUIStyle HeaderStyle()
        {
            var s = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft
            };
            s.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.85f, 0.85f, 0.85f)
                : new Color(0.15f, 0.15f, 0.15f);
            return s;
        }

        protected static GUIStyle SectionStyle()
        {
            var s = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            s.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.85f, 1f)
                : new Color(0.1f, 0.3f, 0.6f);
            return s;
        }

        protected static void DrawSeparator(float thickness = 1f, float padding = 6f)
        {
            GUILayout.Space(padding);
            var rect = GUILayoutUtility.GetRect(1, thickness, GUILayout.ExpandWidth(true));
            var col  = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.1f)
                : new Color(0f, 0f, 0f, 0.1f);
            EditorGUI.DrawRect(rect, col);
            GUILayout.Space(padding);
        }

        protected static void DrawBadge(string text, Color color)
        {
            var s = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(text, s, GUILayout.Height(18), GUILayout.Width(70));
            GUI.backgroundColor = bg;
        }

        protected static bool DrawProviderHeader(string icon, string name, bool enabled)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{icon}  {name}", HeaderStyle());
            GUILayout.FlexibleSpace();
            if (enabled) DrawBadge("ENABLED", new Color(0.2f, 0.8f, 0.4f));
            else          DrawBadge("OFF",    new Color(0.5f, 0.5f, 0.5f));
            EditorGUILayout.EndHorizontal();
            return enabled;
        }
    }
}
