//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

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

            const float toggleLabelWidth = 12f;
            var fieldWidth = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth + 2f) / 3f;
            var toggleFieldWidth = fieldWidth - toggleLabelWidth;

            // freeze position
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Freeze Position");
            layoutRect.x += layoutRect.width + 2f;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "X");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezePositionX = EditorGUI.ToggleLeft(layoutRect, "", script.freezePositionX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Y");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezePositionY = EditorGUI.ToggleLeft(layoutRect, "", script.freezePositionY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Z");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezePositionZ = EditorGUI.ToggleLeft(layoutRect, "", script.freezePositionZ);
            layoutRect.x += layoutRect.width;

            // freeze rotation
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Freeze Rotation");
            layoutRect.x += layoutRect.width + 2f;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "X");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezeRotationX = EditorGUI.ToggleLeft(layoutRect, "", script.freezeRotationX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Y");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezeRotationY = EditorGUI.ToggleLeft(layoutRect, "", script.freezeRotationY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Z");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.freezeRotationZ = EditorGUI.ToggleLeft(layoutRect, "", script.freezeRotationZ);
            layoutRect.x += layoutRect.width;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Pose Freezer Changed");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}