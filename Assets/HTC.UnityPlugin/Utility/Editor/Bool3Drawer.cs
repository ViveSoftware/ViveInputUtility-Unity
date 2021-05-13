//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(Bool3))]
    public class Bool3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            const float toggleLabelWidth = 12f;
            var fieldWidth = (position.width - EditorGUIUtility.labelWidth + 2f) / 3f;
            var toggleFieldWidth = fieldWidth - toggleLabelWidth;

            position.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(position, label);
            position.x += position.width + 2f;

            var xProp = property.FindPropertyRelative("x");
            position.width = toggleLabelWidth;
            EditorGUI.LabelField(position, "X");
            position.x += position.width;

            position.width = toggleFieldWidth;
            xProp.boolValue = EditorGUI.ToggleLeft(position, "", xProp.boolValue);
            position.x += position.width;

            var yProp = property.FindPropertyRelative("y");
            position.width = toggleLabelWidth;
            EditorGUI.LabelField(position, "Y");
            position.x += position.width;

            position.width = toggleFieldWidth;
            yProp.boolValue = EditorGUI.ToggleLeft(position, "", yProp.boolValue);
            position.x += position.width;

            var zProp = property.FindPropertyRelative("z");
            position.width = toggleLabelWidth;
            EditorGUI.LabelField(position, "Z");
            position.x += position.width;

            position.width = toggleFieldWidth;
            zProp.boolValue = EditorGUI.ToggleLeft(position, "", zProp.boolValue);
            position.x += position.width;

            EditorGUI.EndProperty();
        }
    }
}