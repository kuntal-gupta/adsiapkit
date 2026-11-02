using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdsIAPKit.Editor
{
    /// <summary>Dropdown of the catalog's product IDs for fields marked [IAPProductId].</summary>
    [CustomPropertyDrawer(typeof(IAPProductIdAttribute))]
    internal class IAPProductIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var catalog = Resources.Load<AdsIAPSettings>("AdsIAPKit/AdsIAPSettings")?.iapConfig;
            if (property.propertyType != SerializedPropertyType.String || catalog == null || catalog.products.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ids = catalog.products.Select(p => p.id).ToList();
            var names = catalog.products
                .Select(p => string.IsNullOrEmpty(p.displayName) ? p.id : $"{p.displayName}  ({p.id})")
                .ToList();

            var current = ids.IndexOf(property.stringValue);
            if (current < 0)
            {
                // Keep an ID that is not in the catalog visible instead of silently replacing it.
                names.Insert(0, string.IsNullOrEmpty(property.stringValue) ? "— Select a product —" : property.stringValue + "  (not in catalog)");
                ids.Insert(0, property.stringValue);
                current = 0;
            }

            EditorGUI.BeginProperty(position, label, property);
            var selected = EditorGUI.Popup(position, label.text, current, names.ToArray());
            if (selected != current) property.stringValue = ids[selected];
            EditorGUI.EndProperty();
        }
    }
}
