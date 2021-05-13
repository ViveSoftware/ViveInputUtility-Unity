//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomEditor(typeof(TooltipRenderDataAssetBase), true)]
    [CanEditMultipleObjects]
    public class TooltipRenderDataAssetBaseEditor : Editor
    {
        private sealed class VisibleInListArray : EnumArray<ControllerButton, bool> { }

        private VisibleInListArray visibleInList;
        private GUIContent rmButtonContent;
        private GUIContent addButtonContent;

        private EnumUtils.EnumDisplayInfo btnEnumInfo;
        private SerializedProperty scriptProp;
        private SerializedProperty buttonListProp;
        private SerializedProperty dataListProp;

        protected virtual void OnEnable()
        {
            visibleInList = new VisibleInListArray();

            btnEnumInfo = EnumUtils.GetDisplayInfo(typeof(ControllerButton));

            addButtonContent = new GUIContent("Add Button Tooltip");
            rmButtonContent = new GUIContent("Remove");

            scriptProp = serializedObject.FindProperty("m_Script");
            buttonListProp = serializedObject.FindProperty("m_buttonList");
            dataListProp = serializedObject.FindProperty("m_dataList");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(scriptProp);

            visibleInList.Clear();

            var vSpacing = EditorGUIUtility.standardVerticalSpacing;

            var rmBtnStyle = new GUIStyle(GUI.skin.button);
            rmBtnStyle.alignment = TextAnchor.MiddleCenter;
            rmBtnStyle.stretchWidth = false;
            var rmBtnSize = rmBtnStyle.CalcSize(rmButtonContent);

            var toBeRemovedEntry = -1;
            var entryLen = Mathf.Min(buttonListProp.arraySize, dataListProp.arraySize);
            for (int i = 0, imax = entryLen; i < imax; ++i)
            {
                var buttonEnumValue = buttonListProp.GetArrayElementAtIndex(i).intValue;
                if (!visibleInList[buttonEnumValue])
                {
                    int enumIndex;
                    if (btnEnumInfo.value2displayedIndex.TryGetValue(buttonEnumValue, out enumIndex))
                    {
                        var lastRect = GUILayoutUtility.GetLastRect();
                        var removeButtonPos = new Rect(lastRect.xMax - rmBtnSize.x, lastRect.yMax + vSpacing, rmBtnSize.x, rmBtnSize.y - 2f * vSpacing);
                        if (GUI.Button(removeButtonPos, rmButtonContent, rmBtnStyle))
                        {
                            toBeRemovedEntry = i;
                        }
                        else
                        {
                            visibleInList[buttonEnumValue] = true;
                        }

                        EditorGUILayout.PropertyField(dataListProp.GetArrayElementAtIndex(i), new GUIContent(btnEnumInfo.displayedNames[enumIndex]), true);
                    }
                }
            }

            if (toBeRemovedEntry >= 0)
            {
                // remove entry
                buttonListProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
                dataListProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
            }

            EditorGUILayout.Space();

            Rect btPosition = GUILayoutUtility.GetRect(addButtonContent, GUI.skin.button);
            const float addButonWidth = 200f;
            btPosition.x = btPosition.x + (btPosition.width - addButonWidth) / 2;
            btPosition.width = addButonWidth;
            if (GUI.Button(btPosition, addButtonContent))
            {
                ShowAddTriggerMenu();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowAddTriggerMenu()
        {
            var menu = new GenericMenu();
            foreach(var ev in visibleInList.EnumValues)
            {
                var button = ev.Key;
                var visible = ev.Value;
                var name = visibleInList.EnumNameWithAlias((int)button);
                if (visible)
                {
                    menu.AddDisabledItem(new GUIContent(name));
                }
                else
                {
                    menu.AddItem(new GUIContent(name), false, OnAddNewSelected, (int)button);
                }
            }
            menu.ShowAsContext();
            Event.current.Use();
        }

        private void OnAddNewSelected(object index)
        {
            var selected = (int)index;
            int insertIndex = buttonListProp.arraySize;
            dataListProp.InsertArrayElementAtIndex(insertIndex);
            buttonListProp.InsertArrayElementAtIndex(insertIndex);
            buttonListProp.GetArrayElementAtIndex(insertIndex).intValue = selected;

            serializedObject.ApplyModifiedProperties();
        }
    }
}