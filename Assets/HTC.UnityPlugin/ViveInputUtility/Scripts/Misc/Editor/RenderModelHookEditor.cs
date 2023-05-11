//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CustomEditor(typeof(RenderModelHook))]
    [CanEditMultipleObjects]
    public class RenderModelHookEditor : Editor
    {
        protected SerializedProperty scriptProp;
        protected SerializedProperty modeProp;
        protected SerializedProperty viveRoleProp;
        protected SerializedProperty deviceIndexProp;
        protected SerializedProperty overrideModelProp;
        protected SerializedProperty overrideShaderProp;
        protected SerializedProperty customModelsProp;

        protected virtual void OnEnable()
        {
            if (target == null || serializedObject == null) return;

            scriptProp = serializedObject.FindProperty("m_Script");
            modeProp = serializedObject.FindProperty("m_mode");
            viveRoleProp = serializedObject.FindProperty("m_viveRole");
            deviceIndexProp = serializedObject.FindProperty("m_deviceIndex");
            overrideModelProp = serializedObject.FindProperty("m_overrideModel");
            overrideShaderProp = serializedObject.FindProperty("m_overrideShader");
            customModelsProp = serializedObject.FindProperty("m_customModels");
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(scriptProp);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(overrideModelProp);

            EditorGUILayout.PropertyField(overrideShaderProp);

            EditorGUILayout.PropertyField(modeProp);

            switch (modeProp.intValue)
            {
                case (int)RenderModelHook.Mode.ViveRole:
                    EditorGUILayout.PropertyField(viveRoleProp);
                    break;
                case (int)RenderModelHook.Mode.DeivceIndex:
                    EditorGUILayout.PropertyField(deviceIndexProp);
                    break;
                case (int)RenderModelHook.Mode.Disable:
                default:
                    break;
            }

            EditorGUILayout.PropertyField(customModelsProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}