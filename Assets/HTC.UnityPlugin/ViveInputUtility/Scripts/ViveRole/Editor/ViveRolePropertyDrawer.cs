//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomPropertyDrawer(typeof(ViveRoleProperty))]
    public class ViveRolePropertyDrawer : PropertyDrawer
    {
        private static readonly string[] roleTypeNames;

        private static readonly Type defaultRoleType = ViveRoleProperty.DefaultRoleType;
        private static readonly int defaultRoleTypeIndex;

        static ViveRolePropertyDrawer()
        {
            defaultRoleTypeIndex = ViveRoleEnum.ValidViveRoleTable.IndexOf(defaultRoleType.FullName);

            roleTypeNames = new string[ViveRoleEnum.ValidViveRoleTable.Count];
            for (int i = 0; i < roleTypeNames.Length; ++i)
            {
                roleTypeNames[i] = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i).Name;
            }
        }

        public static ViveRoleProperty GetTarget(FieldInfo fieldInfo, SerializedProperty property)
        {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj == null) { return null; }

            ViveRoleProperty actualObject = null;
            if (obj.GetType().IsArray)
            {
                var index = Convert.ToInt32(new string(property.propertyPath.Where(c => char.IsDigit(c)).ToArray()));
                actualObject = ((ViveRoleProperty[])obj)[index];
            }
            else
            {
                actualObject = obj as ViveRoleProperty;
            }
            return actualObject;
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

            var roleTypeProp = property.FindPropertyRelative("m_roleTypeFullName");
            var roleValueProp = property.FindPropertyRelative("m_roleValueName");

            var roleTypeName = roleTypeProp.stringValue;
            var roleValueName = roleValueProp.stringValue;

            // find current role type / type index
            Type roleType;
            var roleTypeIndex = ViveRoleEnum.ValidViveRoleTable.IndexOf(roleTypeName);
            if (roleTypeIndex < 0)
            {
                // name not found
                roleType = defaultRoleType;
                roleTypeIndex = defaultRoleTypeIndex;
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
            var newRoleTypeIndex = EditorGUI.Popup(enumTypeRect, roleTypeIndex, roleTypeNames);
            var newRoleValueIndex = EditorGUI.Popup(enumValueRect, roleValueIndex, roleTypeInfo.RoleValueNames);

            // if new role index changed
            var newRoleType = roleType;
            var newRoleTypeInfo = roleTypeInfo;

            if (newRoleTypeIndex != roleTypeIndex)
            {
                newRoleType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(newRoleTypeIndex);
                newRoleTypeInfo = ViveRoleEnum.GetInfo(newRoleType);
                roleTypeProp.stringValue = ViveRoleEnum.ValidViveRoleTable.GetKeyByIndex(newRoleTypeIndex);
            }

            if (newRoleTypeIndex != roleTypeIndex || newRoleValueIndex != roleValueIndex)
            {
                if (newRoleValueIndex < 0 || newRoleValueIndex >= newRoleTypeInfo.ElementCount)
                {
                    newRoleValueIndex = newRoleTypeInfo.InvalidRoleValueIndex;
                }

                roleValueProp.stringValue = newRoleTypeInfo.GetNameByElementIndex(newRoleValueIndex);
            }

            property.serializedObject.ApplyModifiedProperties();

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();

            // update target
            if (newRoleTypeIndex != roleTypeIndex || newRoleValueIndex != roleValueIndex)
            {
                var target = GetTarget(fieldInfo, property);
                if (newRoleTypeIndex != roleTypeIndex) { target.SetTypeDirty(); }
                if (newRoleValueIndex != roleValueIndex) { target.SetValueDirty(); }
            }
        }
    }
}