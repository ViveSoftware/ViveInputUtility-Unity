//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [CanEditMultipleObjects]
    [CustomPropertyDrawer(typeof(EnumArrayBase), true)]
    public class EnumArrayDrawer : PropertyDrawer
    {
        private const float LINE_SPACE = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            var arrayProp = property.FindPropertyRelative("m_array");
            var arrayLen = arrayProp != null && arrayProp.isArray ? arrayProp.arraySize : 0;

            var newExpanded = false;
            if (!property.isExpanded || arrayLen == 0)
            {
                if (arrayProp != null)
                {
                    newExpanded = EditorGUI.Foldout(position, false, label, true);
                }
                else
                {
                    var target = GetTargetObjectOfProperty(property) as EnumArrayBase;
                    EditorGUI.LabelField(position, label, new GUIContent(target.ElementType.Name + " cannot be serialized"));
                }
            }
            else
            {
                var target = GetTargetObjectOfProperty(property) as EnumArrayBase;
                position.height = EditorGUIUtility.singleLineHeight;

                const float btnPadding = 2f;
                var btnRect = new Rect()
                {
                    x = position.x + EditorGUIUtility.labelWidth + LINE_SPACE + btnPadding,
                    y = position.y + btnPadding,
                    width = position.width - EditorGUIUtility.labelWidth - LINE_SPACE - btnPadding * 2f,
                    height = position.height - btnPadding * 2f,
                };

                // paint trim button 
                if (arrayLen > target.Length)
                {
                    btnRect.width = (btnRect.width - LINE_SPACE - btnPadding * 2f) * 0.5f;
                    if (GUI.Button(btnRect, "Trim"))
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "EnumArray.Trim");
                        arrayProp.arraySize = arrayLen = target.Length;
                    }
                    btnRect.x += btnRect.width + LINE_SPACE + btnPadding * 2f;
                }

                if (GUI.Button(btnRect, "Clear"))
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "EnumArray.Clear");
                    target.Clear();
                }

                // paint foldout title
                newExpanded = EditorGUI.Foldout(position, true, label, true);
                position.y += position.height + LINE_SPACE;

                // paint elements
                EditorGUI.indentLevel += 1;
                {
                    // suppose arrayLen == target.Length
                    target.EnsureLength();
                    for (int i = 0, imax = arrayLen, e = target.MinInt, emax = target.Length; i < imax; ++i, ++e)
                    {
                        var element = arrayProp.GetArrayElementAtIndex(i);
                        var enumName = e < emax ? target.EnumName(e) : (target.EnumType.Name + "(" + e.ToString() + ")");

                        position.height = EditorGUI.GetPropertyHeight(element, true);
                        EditorGUI.PropertyField(position, element, new GUIContent(enumName), true);
                        position.y += position.height + LINE_SPACE;
                    }
                }
                EditorGUI.indentLevel -= 1;
            }

            if (property.isExpanded != newExpanded)
            {
                property.isExpanded = newExpanded;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var result = EditorGUIUtility.singleLineHeight;
            var arrayProp = property.FindPropertyRelative("m_array");
            var arrayLen = arrayProp != null && arrayProp.isArray ? arrayProp.arraySize : 0;

            if (property.isExpanded && arrayLen > 0)
            {
                for (int i = 0, imax = arrayLen; i < imax; ++i)
                {
                    result += LINE_SPACE + EditorGUI.GetPropertyHeight(arrayProp.GetArrayElementAtIndex(i), true);
                }
            }

            return result;
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
    }
}