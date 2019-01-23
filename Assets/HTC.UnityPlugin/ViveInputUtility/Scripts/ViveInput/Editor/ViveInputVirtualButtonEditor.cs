//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ViveInputVirtualButton))]
    public class ViveInputVirtualButtonEditor : Editor
    {
        private SerializedProperty m_scriptProp;
        private SerializedProperty m_activateProp;
        private SerializedProperty m_logicGateProp;
        private SerializedProperty m_inputsProp;
        private SerializedProperty m_onPressProp;
        private SerializedProperty m_onDownProp;
        private SerializedProperty m_onUpProp;
        private SerializedProperty m_onClickProp;
        private SerializedProperty m_toggleGameObjProp;
        private SerializedProperty m_toggleCompProp;

        private static GUIContent s_iconRemoveInput;
        private static GUIContent s_iconAddInput;

        private static bool s_outputHandlersFoldoutState = true;

        static ViveInputVirtualButtonEditor()
        {
            // Have to create a copy since otherwise the tooltip will be overwritten.
            //s_iconToolbarMinus = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
            s_iconRemoveInput = new GUIContent("-");
            s_iconRemoveInput.tooltip = "Remove this input";
            s_iconAddInput = new GUIContent("+");
            s_iconAddInput.tooltip = "Add an input";
        }

        protected virtual void OnEnable()
        {
            m_scriptProp = serializedObject.FindProperty("m_Script");
            m_logicGateProp = serializedObject.FindProperty("m_combineInputsOperator");
            m_inputsProp = serializedObject.FindProperty("m_inputs");
            m_onPressProp = serializedObject.FindProperty("m_onVirtualPress");
            m_onDownProp = serializedObject.FindProperty("m_onVirtualClick");
            m_onUpProp = serializedObject.FindProperty("m_onVirtualPressDown");
            m_onClickProp = serializedObject.FindProperty("m_onVirtualPressUp");
            m_toggleGameObjProp = serializedObject.FindProperty("m_toggleGameObjectOnVirtualClick");
            m_toggleCompProp = serializedObject.FindProperty("m_toggleComponentOnVirtualClick");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var toBeRemovedEntry = -1;
            var prevLabelWidth = EditorGUIUtility.labelWidth;

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_scriptProp);
            GUI.enabled = true;

            if (m_inputsProp.arraySize > 1)
            {
                EditorGUILayout.PropertyField(m_logicGateProp);
            }

            EditorGUILayout.LabelField("Inputs");

            EditorGUIUtility.labelWidth = 1f;
            for (int i = 0, imax = m_inputsProp.arraySize; i < imax; ++i)
            {
                EditorGUILayout.BeginHorizontal();

                var inputProp = m_inputsProp.GetArrayElementAtIndex(i);
                var viveRoleProp = inputProp.FindPropertyRelative("viveRole");
                var buttonProp = inputProp.FindPropertyRelative("button");

                EditorGUILayout.PropertyField(viveRoleProp);
                EditorGUILayout.PropertyField(buttonProp);

                if (GUILayout.Button(s_iconRemoveInput, GUILayout.Height(13f), GUILayout.Width(20f)))
                {
                    toBeRemovedEntry = i;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;

            if (toBeRemovedEntry > -1)
            {
                m_inputsProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
                toBeRemovedEntry = -1;
            }

            if (GUILayout.Button(s_iconAddInput, GUILayout.Height(15f)))
            {
                m_inputsProp.arraySize += 1;
            }

            EditorGUILayout.LabelField("Toggle GameObjects on Virtual Click");

            EditorGUIUtility.labelWidth = 1f;
            for (int i = 0, imax = m_toggleGameObjProp.arraySize; i < imax; ++i)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(m_toggleGameObjProp.GetArrayElementAtIndex(i));

                if (GUILayout.Button(s_iconRemoveInput, GUILayout.Height(13f), GUILayout.Width(20f)))
                {
                    toBeRemovedEntry = i;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;

            if (toBeRemovedEntry > -1)
            {
                m_toggleGameObjProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
                toBeRemovedEntry = -1;
            }

            if (GUILayout.Button(s_iconAddInput, GUILayout.Height(15f)))
            {
                m_toggleGameObjProp.arraySize += 1;
            }

            EditorGUILayout.LabelField("Toggle Components on Virtual Click");

            EditorGUIUtility.labelWidth = 1f;
            for (int i = 0, imax = m_toggleCompProp.arraySize; i < imax; ++i)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(m_toggleCompProp.GetArrayElementAtIndex(i));

                if (GUILayout.Button(s_iconRemoveInput, GUILayout.Height(13f), GUILayout.Width(20f)))
                {
                    toBeRemovedEntry = i;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUIUtility.labelWidth = prevLabelWidth;

            if (toBeRemovedEntry > -1)
            {
                m_toggleCompProp.DeleteArrayElementAtIndex(toBeRemovedEntry);
                toBeRemovedEntry = -1;
            }

            if (GUILayout.Button(s_iconAddInput, GUILayout.Height(15f)))
            {
                m_toggleCompProp.arraySize += 1;
            }

            s_outputHandlersFoldoutState = EditorGUILayout.Foldout(s_outputHandlersFoldoutState, "Output Handlers");

            if (s_outputHandlersFoldoutState)
            {
                EditorGUILayout.PropertyField(m_onPressProp);
                EditorGUILayout.PropertyField(m_onDownProp);
                EditorGUILayout.PropertyField(m_onUpProp);
                EditorGUILayout.PropertyField(m_onClickProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}