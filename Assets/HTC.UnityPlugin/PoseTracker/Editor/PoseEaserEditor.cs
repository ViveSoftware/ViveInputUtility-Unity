//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

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

            var fieldWidth = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth) / 3f;

            // ease position
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Ease Position");
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easePositionX = EditorGUI.ToggleLeft(layoutRect, "X", script.easePositionX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easePositionY = EditorGUI.ToggleLeft(layoutRect, "Y", script.easePositionY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easePositionZ = EditorGUI.ToggleLeft(layoutRect, "Z", script.easePositionZ);

            // ease rotation
            layoutRect = EditorGUILayout.GetControlRect();

            layoutRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(layoutRect, "Ease Rotation");
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easeRotationX = EditorGUI.ToggleLeft(layoutRect, "X", script.easeRotationX);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easeRotationY = EditorGUI.ToggleLeft(layoutRect, "Y", script.easeRotationY);
            layoutRect.x += layoutRect.width;

            layoutRect.width = fieldWidth;
            script.easeRotationZ = EditorGUI.ToggleLeft(layoutRect, "Z", script.easeRotationZ);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Pose Easer Changed");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}