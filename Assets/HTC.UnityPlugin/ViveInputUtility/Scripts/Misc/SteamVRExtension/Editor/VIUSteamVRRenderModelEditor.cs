//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System.Text;
using UnityEditor;
using UnityEngine;
#if VIU_STEAMVR
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    [CustomEditor(typeof(VIUSteamVRRenderModel)), CanEditMultipleObjects]
    public class VIUSteamVRRenderModelEditr : Editor
    {
        private static GUIContent[] s_renderModelNames;

        private SerializedProperty m_scriptProp;
        private SerializedProperty m_modelOverrideProp;
        private SerializedProperty m_shaderOverrideProp;
        private SerializedProperty m_updateDynamicallyProp;

        private int m_selectedModelIndex;

        protected virtual void OnEnable()
        {
            m_scriptProp = serializedObject.FindProperty("m_Script");
            m_modelOverrideProp = serializedObject.FindProperty("m_modelOverride");
            m_shaderOverrideProp = serializedObject.FindProperty("m_shaderOverride");
            m_updateDynamicallyProp = serializedObject.FindProperty("m_updateDynamically");

            // Load render model names if necessary.
            if (s_renderModelNames == null)
            {
                s_renderModelNames = LoadRenderModelNames();
            }

            // Update renderModelIndex based on current modelOverride value.
            m_selectedModelIndex = 0;
            var selectedModelName = m_modelOverrideProp.stringValue;
            if (!string.IsNullOrEmpty(selectedModelName))
            {
                for (int i = 1, imax = s_renderModelNames.Length; i < imax; i++)
                {
                    if (selectedModelName == s_renderModelNames[i].text)
                    {
                        m_selectedModelIndex = i;
                        break;
                    }
                }
            }
        }

        private static GUIContent[] LoadRenderModelNames()
        {
            var results = default(GUIContent[]);
#if VIU_STEAMVR
            var needsShutdown = false;
            var vrRenderModels = OpenVR.RenderModels;
            if (vrRenderModels == null)
            {
                var error = EVRInitError.None;
                if (!SteamVR.active && !SteamVR.usingNativeSupport)
                {
                    OpenVR.Init(ref error, EVRApplicationType.VRApplication_Utility);
                    vrRenderModels = OpenVR.RenderModels;
                    needsShutdown = true;
                }
            }

            if (vrRenderModels != null)
            {
                var strBuilder = new StringBuilder();
                var count = vrRenderModels.GetRenderModelCount();
                results = new GUIContent[count + 1];
                results[0] = new GUIContent("None");

                for (uint i = 0; i < count; i++)
                {
                    var strLen = vrRenderModels.GetRenderModelName(i, strBuilder, 0);
                    if (strLen == 0) { continue; }

                    strBuilder.EnsureCapacity((int)strLen);
                    vrRenderModels.GetRenderModelName(i, strBuilder, strLen);
                    results[i + 1] = new GUIContent(strBuilder.ToString());
                }
            }

            if (needsShutdown)
            {
                OpenVR.Shutdown();
            }
#endif
            return results == null ? new GUIContent[] { new GUIContent("None") } : results;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_scriptProp);
            GUI.enabled = true;

            var selectedIndex = EditorGUILayout.Popup(new GUIContent("Model Override", VIUSteamVRRenderModel.MODEL_OVERRIDE_WARNNING), m_selectedModelIndex, s_renderModelNames);
            if (selectedIndex != m_selectedModelIndex)
            {
                m_selectedModelIndex = selectedIndex;
                m_modelOverrideProp.stringValue = selectedIndex == 0 ? string.Empty : s_renderModelNames[selectedIndex].text;
            }

            EditorGUILayout.PropertyField(m_shaderOverrideProp);
            EditorGUILayout.PropertyField(m_updateDynamicallyProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}