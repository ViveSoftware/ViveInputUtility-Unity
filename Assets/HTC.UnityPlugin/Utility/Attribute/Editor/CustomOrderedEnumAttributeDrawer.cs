//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [CustomPropertyDrawer(typeof(CustomOrderedEnumAttribute))]
    public class CusromOrderedEnumAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // First get the attribute since it contains the range for the slider
            var attr = attribute as CustomOrderedEnumAttribute;

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.displayName));

            // determine which enum type to display
            Type displayedEnumType = null;
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                if (attr.overrideEnumType != null && attr.overrideEnumType.IsEnum)
                {
                    displayedEnumType = attr.overrideEnumType;
                }
                else
                {
                    displayedEnumType = fieldInfo.FieldType;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (attr.overrideEnumType != null && attr.overrideEnumType.IsEnum)
                {
                    displayedEnumType = attr.overrideEnumType;
                }
            }

            // display enum popup if displayedEnumType is determined, otherwise, display the default property field
            if (displayedEnumType == null)
            {
                EditorGUI.PropertyField(position, property);
            }
            else
            {
                var enumInfo = EnumUtils.GetDisplayInfo(displayedEnumType);
                var displayedNames = enumInfo.displayedNames;
                var displayedValues = enumInfo.displayedValues;

                if (!enumInfo.value2displayedIndex.ContainsKey(property.intValue))
                {
                    displayedNames = displayedNames.Concat(new string[] { property.intValue.ToString() }).ToArray();
                    displayedValues = displayedValues.Concat(new int[] { property.intValue }).ToArray();
                }

                property.intValue = EditorGUI.IntPopup(position, property.intValue, displayedNames, displayedValues);
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}