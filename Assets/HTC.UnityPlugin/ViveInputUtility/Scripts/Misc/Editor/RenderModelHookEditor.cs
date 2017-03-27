using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RenderModelHook))]
[CanEditMultipleObjects]
public class RenderModelHookEditor : Editor
{
    protected SerializedProperty scriptProp;
    protected SerializedProperty modeProp;
    protected SerializedProperty viveRoleProp;
    protected SerializedProperty deviceIndexProp;
    protected SerializedProperty applyTrackingProp;
    protected SerializedProperty originProp;

    protected virtual void OnEnable()
    {
        if (target == null || serializedObject == null) return;

        scriptProp = serializedObject.FindProperty("m_Script");
        modeProp = serializedObject.FindProperty("m_mode");
        viveRoleProp = serializedObject.FindProperty("m_viveRole");
        deviceIndexProp = serializedObject.FindProperty("m_deviceIndex");
        applyTrackingProp = serializedObject.FindProperty("m_applyTracking");
        originProp = serializedObject.FindProperty("m_origin");
    }

    public override void OnInspectorGUI()
    {
        if (target == null || serializedObject == null) return;

        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(scriptProp);
        GUI.enabled = true;

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

        EditorGUILayout.PropertyField(applyTrackingProp);

        if (applyTrackingProp.boolValue)
        {
            EditorGUILayout.PropertyField(originProp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
