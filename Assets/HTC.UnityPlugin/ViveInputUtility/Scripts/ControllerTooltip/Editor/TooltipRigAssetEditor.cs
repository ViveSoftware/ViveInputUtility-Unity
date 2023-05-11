//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomEditor(typeof(TooltipRigAsset), true)]
    [CanEditMultipleObjects]
    public class TooltipRigAssetEditor : Editor
    {
        private sealed class VisibleInListArray : EnumArray<ControllerButton, bool> { }

        private VisibleInListArray visibleInList;
        private GUIContent rmButtonContent;
        private GUIContent addButtonContent;

        private EnumUtils.EnumDisplayInfo btnEnumInfo;
        private SerializedProperty scriptProp;
        private SerializedProperty entriesProp;

        protected virtual void OnEnable()
        {
            visibleInList = new VisibleInListArray();

            btnEnumInfo = EnumUtils.GetDisplayInfo(typeof(ControllerButton));

            addButtonContent = new GUIContent("Add Button Rig");
            rmButtonContent = new GUIContent("Remove");

            scriptProp = serializedObject.FindProperty("m_Script");
            entriesProp = serializedObject.FindProperty("m_rigEntries");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(scriptProp);

            visibleInList.Clear();

            var rmBtnStyle = new GUIStyle(GUI.skin.button);
            rmBtnStyle.alignment = TextAnchor.MiddleCenter;
            rmBtnStyle.stretchWidth = false;
            var rmBtnSize = rmBtnStyle.CalcSize(rmButtonContent);
            var vSpacing = EditorGUIUtility.standardVerticalSpacing;

            var toBeRemovedEntry = -1;
            for (int i = 0, imax = entriesProp.arraySize; i < imax; ++i)
            {
                var entryProp = entriesProp.GetArrayElementAtIndex(i);
                var buttonEnumValue = entryProp.FindPropertyRelative("button").intValue;
                if (!visibleInList[buttonEnumValue])
                {
                    int enumIndex;
                    if (btnEnumInfo.value2displayedIndex.TryGetValue(buttonEnumValue, out enumIndex))
                    {
                        SerializedProperty rigProp;

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

                        var objFieldWidth = (lastRect.width - EditorGUIUtility.labelWidth - removeButtonPos.width) * 0.5f;
                        var objFieldPos = new Rect(lastRect.x + EditorGUIUtility.labelWidth, removeButtonPos.y, objFieldWidth, EditorGUIUtility.singleLineHeight);
                        var droppedObj = EditorGUI.ObjectField(objFieldPos, null, typeof(Transform), true) as Transform;
                        if (droppedObj != null)
                        {
                            rigProp = entryProp.FindPropertyRelative("tooltipRig");
                            rigProp.FindPropertyRelative("buttonPosition").vector3Value = droppedObj.localPosition;
                            rigProp.FindPropertyRelative("buttonNormal").vector3Value = droppedObj.localRotation * Vector3.forward;
                        }

                        objFieldPos = new Rect(objFieldPos.x + objFieldWidth, removeButtonPos.y, objFieldWidth, EditorGUIUtility.singleLineHeight);
                        droppedObj = EditorGUI.ObjectField(objFieldPos, null, typeof(Transform), true) as Transform;
                        if (droppedObj != null)
                        {
                            rigProp = entryProp.FindPropertyRelative("tooltipRig");
                            rigProp.FindPropertyRelative("labelPosition").vector3Value = droppedObj.localPosition;
                            rigProp.FindPropertyRelative("labelNormal").vector3Value = droppedObj.localRotation * Vector3.forward;
                            rigProp.FindPropertyRelative("labelUp").vector3Value = droppedObj.localRotation * Vector3.up;
                            if (droppedObj.childCount > 0)
                            {
                                rigProp.FindPropertyRelative("labelAnchor").intValue = (int)VectorXYToAnchor(droppedObj.GetChild(0).localPosition);
                            }
                        }

                        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("tooltipRig"), new GUIContent(btnEnumInfo.displayedNames[enumIndex]), true);
                    }
                }
            }

            if (toBeRemovedEntry >= 0)
            {
                // remove entry
                entriesProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
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
            foreach (var ev in visibleInList.EnumValues)
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
            int insertIndex = entriesProp.arraySize;
            entriesProp.InsertArrayElementAtIndex(insertIndex);
            entriesProp.GetArrayElementAtIndex(insertIndex).FindPropertyRelative("button").intValue = selected;

            serializedObject.ApplyModifiedProperties();
        }

        private static TextAnchor VectorXYToAnchor(Vector3 v)
        {
            if (v.y < 0f)
            {
                if (v.x > 0f)
                {
                    return TextAnchor.UpperLeft;
                }
                else if (v.x == 0f)
                {
                    return TextAnchor.UpperCenter;
                }
                else
                {
                    return TextAnchor.UpperRight;
                }
            }
            else if (v.y == 0f)
            {
                if (v.x > 0f)
                {
                    return TextAnchor.MiddleLeft;
                }
                else if (v.x == 0f)
                {
                    return TextAnchor.MiddleCenter;
                }
                else
                {
                    return TextAnchor.MiddleRight;
                }
            }
            else
            {
                if (v.x > 0f)
                {
                    return TextAnchor.LowerLeft;
                }
                else if (v.x == 0f)
                {
                    return TextAnchor.LowerCenter;
                }
                else
                {
                    return TextAnchor.LowerRight;
                }
            }
        }
    }
}