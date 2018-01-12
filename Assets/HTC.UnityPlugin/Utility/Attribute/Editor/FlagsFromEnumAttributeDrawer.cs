//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [CustomPropertyDrawer(typeof(FlagsFromEnumAttribute))]
    public class FlagsFromEnumAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // First get the attribute since it contains the range for the slider
            var ffeAttribute = attribute as FlagsFromEnumAttribute;

            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "Use FlagFromEnum with integer.");
            }
            else if (ffeAttribute.EnumType == null || !ffeAttribute.EnumType.IsEnum)
            {
                EditorGUI.LabelField(position, label.text, "Set FlagFromEnum argument with enum type.");
            }
            else
            {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.displayName));

                var enumInfo = EnumUtils.GetDisplayInfo(ffeAttribute.EnumType);
                var realMask = property.intValue;
                var oldDisplayedMask = enumInfo.RealToDisplayedMaskField(realMask);
                var newDisplayedMask = EditorGUI.MaskField(position, oldDisplayedMask, enumInfo.displayedMaskNames);
                property.intValue = enumInfo.DisplayedToRealMaskField(newDisplayedMask, (uint)newDisplayedMask > (uint)oldDisplayedMask);
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}