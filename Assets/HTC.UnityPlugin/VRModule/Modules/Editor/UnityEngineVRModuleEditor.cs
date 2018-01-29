//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public static class UnityVRModuleEditor
    {
#if UNITY_5_5_OR_NEWER
        // Add joystick axis input bindings to InputManager
        // See OpenVR/Oculus left/right controllers mapping at
        // https://docs.unity3d.com/Manual/OpenVRControllers.html
        [InitializeOnLoadMethod]
        private static void EnforceInputManagerBindings()
        {
            try
            {
                var axisObj = new Axis();
                for (int i = 0; i < UnityEngineVRModule.ButtonAxisID.Count; ++i)
                {
                    axisObj.name = UnityEngineVRModule.ButtonAxisName.Index(i);
                    axisObj.axis = UnityEngineVRModule.ButtonAxisID.Index(i);
                    axisObj.invert = axisObj.axis == UnityEngineVRModule.ButtonAxisID.LPadY || axisObj.axis == UnityEngineVRModule.ButtonAxisID.RPadY;
                    BindAxis(axisObj);
                }
            }
            catch
            {
                Debug.LogError("Failed to apply Vive Input Utility input manager bindings.");
            }
        }

        private class Axis
        {
            public string name = string.Empty;
            public string descriptiveName = string.Empty;
            public string descriptiveNegativeName = string.Empty;
            public string negativeButton = string.Empty;
            public string positiveButton = string.Empty;
            public string altNegativeButton = string.Empty;
            public string altPositiveButton = string.Empty;
            public float gravity = 0.0f;
            public float dead = 0.001f;
            public float sensitivity = 1.0f;
            public bool snap = false;
            public bool invert = false;
            public int type = 2;
            public int axis = 0;
            public int joyNum = 0;
        }

        private static void BindAxis(Axis axis)
        {
            var serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            var axesProperty = serializedObject.FindProperty("m_Axes");

            var axisIter = axesProperty.Copy();
            axisIter.Next(true);
            axisIter.Next(true);
            while (axisIter.Next(false))
            {
                if (axisIter.FindPropertyRelative("m_Name").stringValue == axis.name)
                {
                    // Axis already exists. Don't create binding.
                    return;
                }
            }

            axesProperty.arraySize++;
            serializedObject.ApplyModifiedProperties();

            SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex(axesProperty.arraySize - 1);
            axisProperty.FindPropertyRelative("m_Name").stringValue = axis.name;
            axisProperty.FindPropertyRelative("descriptiveName").stringValue = axis.descriptiveName;
            axisProperty.FindPropertyRelative("descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
            axisProperty.FindPropertyRelative("negativeButton").stringValue = axis.negativeButton;
            axisProperty.FindPropertyRelative("positiveButton").stringValue = axis.positiveButton;
            axisProperty.FindPropertyRelative("altNegativeButton").stringValue = axis.altNegativeButton;
            axisProperty.FindPropertyRelative("altPositiveButton").stringValue = axis.altPositiveButton;
            axisProperty.FindPropertyRelative("gravity").floatValue = axis.gravity;
            axisProperty.FindPropertyRelative("dead").floatValue = axis.dead;
            axisProperty.FindPropertyRelative("sensitivity").floatValue = axis.sensitivity;
            axisProperty.FindPropertyRelative("snap").boolValue = axis.snap;
            axisProperty.FindPropertyRelative("invert").boolValue = axis.invert;
            axisProperty.FindPropertyRelative("type").intValue = axis.type;
            axisProperty.FindPropertyRelative("axis").intValue = axis.axis;
            axisProperty.FindPropertyRelative("joyNum").intValue = axis.joyNum;
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}