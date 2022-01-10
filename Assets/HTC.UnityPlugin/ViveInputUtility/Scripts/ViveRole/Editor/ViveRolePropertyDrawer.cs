//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomPropertyDrawer(typeof(ViveRoleProperty))]
    public class ViveRolePropertyDrawer : PropertyDrawer
    {
        private static readonly string[] s_roleTypeNames;

        private static readonly Type s_defaultRoleType = ViveRoleProperty.DefaultRoleType;
        private static readonly int s_defaultRoleTypeIndex;

        static ViveRolePropertyDrawer()
        {
            s_defaultRoleTypeIndex = ViveRoleEnum.ValidViveRoleTable.IndexOf(s_defaultRoleType.FullName);

            s_roleTypeNames = new string[ViveRoleEnum.ValidViveRoleTable.Count];
            for (int i = 0; i < s_roleTypeNames.Length; ++i)
            {
                s_roleTypeNames[i] = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i).Name;
            }
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) { return null; }

            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) { return f.GetValue(source); }

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null) { return p.GetValue(source, null); }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetFieldValue(object source, string name, int index)
        {
            var enumerable = GetFieldValue(source, name) as System.Collections.IEnumerable;

            if (enumerable == null) { return null; }

            var enm = enumerable.GetEnumerator();

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) { return null; }
            }

            return enm.Current;
        }

        private static Regex s_regArray = new Regex(@"^(\w+)\[(\d+)\]$");
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            var obj = (object)prop.serializedObject.targetObject;
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                var matche = s_regArray.Match(element);
                if (matche.Success)
                {
                    var elementName = matche.Groups[1].Value;
                    var index = int.Parse(matche.Groups[2].Value);

                    obj = GetFieldValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }

            return obj;
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(property.displayName));

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Caluculate rects
            var spacing = 5.0f;
            var rectWidth = Mathf.Round((position.width - spacing) * 0.5f);
            var enumTypeRect = new Rect(position.x, position.y, rectWidth, position.height);
            var enumValueRect = new Rect(position.x + rectWidth + spacing, position.y, rectWidth, position.height);

            var roleTypeNameProp = property.FindPropertyRelative("m_roleTypeFullName");
            var roleValueNameProp = property.FindPropertyRelative("m_roleValueName");
            var roleValueIntProp = property.FindPropertyRelative("m_roleValueInt");

            var roleTypeName = roleTypeNameProp.stringValue;
            var roleValueName = roleValueNameProp.stringValue;

            // find current role type / type index
            Type roleType;
            var roleTypeIndex = ViveRoleEnum.ValidViveRoleTable.IndexOf(roleTypeName);
            if (roleTypeIndex < 0)
            {
                // name not found
                roleType = s_defaultRoleType;
                roleTypeIndex = s_defaultRoleTypeIndex;
            }
            else
            {
                roleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(roleTypeIndex);
            }

            // find current role value index
            var roleTypeInfo = ViveRoleEnum.GetInfo(roleType);
            var roleValueIndex = roleTypeInfo.GetElementIndexByName(roleValueName);
            if (roleValueIndex < 0)
            {
                roleValueIndex = roleTypeInfo.InvalidRoleValueIndex;
            }

            // draw pupup box, get new role type index / value index
            var newRoleTypeIndex = EditorGUI.Popup(enumTypeRect, roleTypeIndex, s_roleTypeNames);
            var newRoleValueIndex = EditorGUI.Popup(enumValueRect, roleValueIndex, EnumUtils.GetDisplayInfo(roleType).displayedNames);

            // if new role index changed
            if (newRoleTypeIndex != roleTypeIndex || newRoleValueIndex != roleValueIndex)
            {
                var target = GetTargetObjectOfProperty(property) as ViveRoleProperty;

                if (newRoleTypeIndex != roleTypeIndex)
                {
                    roleTypeNameProp.stringValue = ViveRoleEnum.ValidViveRoleTable.GetKeyByIndex(newRoleTypeIndex);
                    roleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(newRoleTypeIndex);
                    roleTypeInfo = ViveRoleEnum.GetInfo(roleType);

                    target.SetTypeDirty();
                }

                if (newRoleValueIndex != roleValueIndex)
                {
                    roleValueNameProp.stringValue = roleTypeInfo.GetNameByElementIndex(newRoleValueIndex);
                    roleValueIntProp.intValue = roleTypeInfo.GetRoleValueByElementIndex(newRoleValueIndex);

                    target.SetValueDirty();
                }

                EditorApplication.delayCall += target.Update;
            }

            property.serializedObject.ApplyModifiedProperties(); // will call ViveRoleProperty.OnAfterDeserialize here

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}