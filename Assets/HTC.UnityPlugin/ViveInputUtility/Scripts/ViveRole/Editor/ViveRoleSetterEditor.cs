//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ViveRoleSetter))]
    public class ViveRoleSetterEditor : Editor
    {
        private static List<IViveRoleComponent> s_comps;

        private SerializedProperty m_scriptProp;
        private SerializedProperty m_viveRoleProp;

        protected virtual void OnEnable()
        {
            m_scriptProp = serializedObject.FindProperty("m_Script");
            m_viveRoleProp = serializedObject.FindProperty("m_viveRole");
        }

        public override void OnInspectorGUI()
        {
            var setter = target as ViveRoleSetter;

            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_scriptProp);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_viveRoleProp);

            var dirtyCompCount = 0;
            if (s_comps == null) { s_comps = new List<IViveRoleComponent>(); }
            setter.GetComponentsInChildren(s_comps);
            for (var i = s_comps.Count - 1; i >= 0; --i)
            {
                if (s_comps[i].viveRole != setter.viveRole)
                {
                    ++dirtyCompCount;
                }
            }
            s_comps.Clear();

            if (dirtyCompCount > 0 && GUILayout.Button("Refresh(" + dirtyCompCount + ")"))
            {
                setter.UpdateChildrenViveRole();
                serializedObject.Update();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}