//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomPropertyDrawer(typeof(ControllerButtonMask))]
    public class ControllerButtonMaskDrawer : PropertyDrawer
    {
        private static GUIStyle s_popup;
        private static GUIContent s_tempContent;
        private static List<bool> s_displayedMask;
        private static EnumUtils.EnumDisplayInfo s_enumInfo = EnumUtils.GetDisplayInfo(typeof(ControllerButton));

        private bool m_foldoutOpen = false;

        static ControllerButtonMaskDrawer()
        {
            s_popup = new GUIStyle(EditorStyles.popup);
            s_tempContent = new GUIContent();
            s_displayedMask = new List<bool>();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!m_foldoutOpen || s_enumInfo == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight * (s_enumInfo.displayedMaskNames.Length + 2);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName));

            // get display mask value
            s_displayedMask.Clear();
            var maskProperty = property.FindPropertyRelative("raw");
            var enumDisplayLength = s_enumInfo.displayedMaskLength;
            var realMask = (ulong)maskProperty.longValue;
            var firstSelected = string.Empty;
            for (int i = 0; i < enumDisplayLength; ++i)
            {
                if (EnumUtils.GetFlag(realMask, s_enumInfo.displayedMaskValues[i]))
                {
                    s_displayedMask.Add(true);
                    if (string.IsNullOrEmpty(firstSelected)) { firstSelected = s_enumInfo.displayedMaskNames[i]; }
                }
                else
                {
                    s_displayedMask.Add(false);
                }
            }

            var flagsCount = 0;
            for (var i = 0; i < EnumUtils.ULONG_MASK_FIELD_LENGTH; ++i)
            {
                if (EnumUtils.GetFlag(realMask, i)) { ++flagsCount; }
            }

            if (EditorGUI.showMixedValue)
            {
                s_tempContent.text = " - ";
            }
            else if (flagsCount == 0)
            {
                s_tempContent.text = "None";
            }
            else if (flagsCount == 1)
            {
                s_tempContent.text = firstSelected;
            }
            else if (flagsCount < enumDisplayLength)
            {
                s_tempContent.text = "Mixed...";
            }
            else
            {
                s_tempContent.text = "All";
            }

            var controlPos = position;
            controlPos.height = EditorGUIUtility.singleLineHeight;
            var id = GUIUtility.GetControlID(FocusType.Passive, controlPos);

            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (controlPos.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        Event.current.Use();
                        m_foldoutOpen = !m_foldoutOpen;
                    }
                    break;
                case EventType.Repaint:
                    s_popup.Draw(position, s_tempContent, id, false);
                    break;
            }

            if (m_foldoutOpen)
            {
                position.y += EditorGUIUtility.singleLineHeight;

                var halfWidth = position.width * 0.5f;
                if (GUI.Button(new Rect(position.x, position.y, halfWidth - 1, EditorGUIUtility.singleLineHeight), "All"))
                {
                    realMask = ~0ul;
                    //m_foldoutOpen = false;
                }

                //Draw the None button
                if (GUI.Button(new Rect(position.x + halfWidth + 1, position.y, halfWidth - 1, EditorGUIUtility.singleLineHeight), "None"))
                {
                    realMask = 0ul;
                    //m_foldoutOpen = false;
                }

                for (int i = 0; i < enumDisplayLength; ++i)
                {
                    position.y += EditorGUIUtility.singleLineHeight;
                    var toggled = EditorGUI.ToggleLeft(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), s_enumInfo.displayedMaskNames[i], s_displayedMask[i]);
                    if (s_displayedMask[i] != toggled)
                    {
                        s_displayedMask[i] = toggled;
                        EnumUtils.SetFlag(ref realMask, s_enumInfo.displayedMaskValues[i], toggled);
                        //m_foldoutOpen = false;
                    }
                }

                maskProperty.longValue = (long)realMask;
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}