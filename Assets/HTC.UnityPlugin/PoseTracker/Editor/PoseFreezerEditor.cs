//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    [CustomEditor(typeof(PoseFreezer))]
    public class PoseFreezerEditor : Editor
    {
        protected SerializedProperty scriptProp;
        protected SerializedProperty priorityProp;

        protected virtual void OnEnable()
        {
            if (target == null || serializedObject == null) return;

            scriptProp = serializedObject.FindProperty("m_Script");
            priorityProp = serializedObject.FindProperty("m_priority");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            var script = target as PoseFreezer;
            Rect layoutRect;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(scriptProp);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(priorityProp);

            var fieldWidth = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth) / 3f;

            // freeze position
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Freeze Position");
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezePositionX = EditorGUI.ToggleLeft(layoutRect, "X", script.freezePositionX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezePositionY = EditorGUI.ToggleLeft(layoutRect, "Y", script.freezePositionY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezePositionZ = EditorGUI.ToggleLeft(layoutRect, "Z", script.freezePositionZ);

            // freeze rotation
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Freeze Rotation");
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezeRotationX = EditorGUI.ToggleLeft(layoutRect, "X", script.freezeRotationX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezeRotationY = EditorGUI.ToggleLeft(layoutRect, "Y", script.freezeRotationY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.freezeRotationZ = EditorGUI.ToggleLeft(layoutRect, "Z", script.freezeRotationZ);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Pose Freezer Changed");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}