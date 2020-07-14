using Ricercar.Gravity;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

[CustomPropertyDrawer(typeof(GravityLayerAttribute))]
class GravityLayerAttributePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = EditorGUI.IntPopup(position, property.displayName, property.intValue, GravityInteraction.s_layerNames, GravityInteraction.s_layerIndices);
    }
}
