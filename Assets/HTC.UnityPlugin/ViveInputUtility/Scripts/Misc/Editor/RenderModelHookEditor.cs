﻿using UnityEditor;
using UnityEngine;

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

    protected virtual void OnEnable()
    {
        if (target == null || serializedObject == null) return;

        scriptProp = serializedObject.FindProperty("m_Script");
        modeProp = serializedObject.FindProperty("m_mode");
        viveRoleProp = serializedObject.FindProperty("m_viveRole");
        deviceIndexProp = serializedObject.FindProperty("m_deviceIndex");
        overrideModelProp = serializedObject.FindProperty("m_overrideModel");
		overrideShaderProp = serializedObject.FindProperty("m_overrideShader");
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

        serializedObject.ApplyModifiedProperties();
    }
}
