//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [CustomPropertyDrawer(typeof(FlagsFromEnumAttribute))]
    public class FlagsFromEnumAttributeDrawer : PropertyDrawer
    {
        private struct EnumInfo
        {
            public string[] displayedName;
            public uint[] displayedMask;
            public uint[] realMask;

            public uint ToDisplayedMaskField(uint realMask)
            {
                var result = 0u;

                for (uint i = 0u, imax = (uint)this.realMask.Length; i < imax && realMask > 0u; ++i, realMask >>= 1)
                {
                    if ((realMask & 1u) == 0u) { continue; }

                    result |= this.realMask[i];
                }

                return result;
            }

            public uint ToRealMaskField(uint editorMask)
            {
                var result = 0u;

                for (uint i = 0u, imax = (uint)displayedMask.Length; i < imax && editorMask > 0u; ++i, editorMask >>= 1)
                {
                    if ((editorMask & 1u) == 0u) { continue; }

                    result |= displayedMask[i];
                }

                return result;
            }
        }

        private const int INT_BIT_COUNT = sizeof(int) * 8;
        private static Dictionary<Type, EnumInfo> s_enumTypeInfo = new Dictionary<Type, EnumInfo>();

        private static EnumInfo GetEnumEnfo(Type type)
        {
            EnumInfo info;

            if (!s_enumTypeInfo.TryGetValue(type, out info))
            {
                var names = Enum.GetNames(type);
                var values = Enum.GetValues(type) as int[];
                var validValueCount = 0;
                var bitNames = new string[INT_BIT_COUNT];

                for (int i = 0, imax = values.Length; i < imax; ++i)
                {
                    if (values[i] < 0 || values[i] >= INT_BIT_COUNT) { continue; }

                    if (!string.IsNullOrEmpty(bitNames[values[i]])) { continue; } // set already

                    bitNames[values[i]] = names[i];
                    ++validValueCount;
                }

                info.displayedName = new string[validValueCount];
                info.displayedMask = new uint[validValueCount];
                info.realMask = new uint[INT_BIT_COUNT];
                for (int i = 0, iValid = 0; i < bitNames.Length; ++i)
                {
                    if (string.IsNullOrEmpty(bitNames[i])) { continue; }

                    info.displayedName[iValid] = bitNames[i];
                    info.displayedMask[iValid] = 1u << i;
                    info.realMask[i] = 1u << iValid;

                    ++iValid;
                }

                s_enumTypeInfo[type] = info;
            }

            return info;
        }

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
                var enumInfo = GetEnumEnfo(ffeAttribute.EnumType);

                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.displayName));

                var realMask = (uint)property.intValue;
                var displayedMask = EditorGUI.MaskField(position, (int)enumInfo.ToDisplayedMaskField(realMask), enumInfo.displayedName);
                property.intValue = (int)enumInfo.ToRealMaskField((uint)displayedMask);
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}