//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    [CustomEditor(typeof(PoseEaser))]
    public class PoseEaserEditor : Editor
    {
        protected SerializedProperty scriptProp;
        protected SerializedProperty priorityProp;
        protected SerializedProperty durationProp;

        protected virtual void OnEnable()
        {
            if (target == null || serializedObject == null) return;

            scriptProp = serializedObject.FindProperty("m_Script");
            priorityProp = serializedObject.FindProperty("m_priority");
            durationProp = serializedObject.FindProperty("duration");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            var script = target as PoseEaser;
            Rect layoutRect;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(scriptProp);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(priorityProp);
            EditorGUILayout.PropertyField(durationProp);

            const float toggleLabelWidth = 12f;
            var fieldWidth = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth + 2f) / 3f;
            var toggleFieldWidth = fieldWidth - toggleLabelWidth;

            // ease position
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Ease Position");
            layoutRect.x += layoutRect.width + 2f;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "X");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easePositionX = EditorGUI.ToggleLeft(layoutRect, "", script.easePositionX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Y");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easePositionY = EditorGUI.ToggleLeft(layoutRect, "", script.easePositionY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Z");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easePositionZ = EditorGUI.ToggleLeft(layoutRect, "", script.easePositionZ);
            layoutRect.x += layoutRect.width;

            // ease rotation
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Ease Rotation");
            layoutRect.x += layoutRect.width + 2f;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "X");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easeRotationX = EditorGUI.ToggleLeft(layoutRect, "", script.easeRotationX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Y");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easeRotationY = EditorGUI.ToggleLeft(layoutRect, "", script.easeRotationY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleLabelWidth;
            EditorGUI.LabelField(layoutRect, "Z");
            layoutRect.x += layoutRect.width;

            layoutRect.width = toggleFieldWidth;
            script.easeRotationZ = EditorGUI.ToggleLeft(layoutRect, "", script.easeRotationZ);
            layoutRect.x += layoutRect.width;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Pose Easer Changed");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}