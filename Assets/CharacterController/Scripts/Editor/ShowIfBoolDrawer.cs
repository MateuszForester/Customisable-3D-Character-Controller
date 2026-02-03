#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfBoolAttribute))]
public class ShowIfBoolDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var att = (ShowIfBoolAttribute)attribute;
        bool show = ShouldShow(property, att);
        return show ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var att = (ShowIfBoolAttribute)attribute;
        if (!ShouldShow(property, att)) return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    private bool ShouldShow(SerializedProperty property, ShowIfBoolAttribute att)
    {
        var boolProp = property.serializedObject.FindProperty(att.boolName);

        // Fail-safe: if property not found / not bool, show it so you notice the issue.
        if (boolProp == null || boolProp.propertyType != SerializedPropertyType.Boolean)
            return true;

        bool value = boolProp.boolValue;
        return att.invert ? !value : value;
    }
}
#endif
