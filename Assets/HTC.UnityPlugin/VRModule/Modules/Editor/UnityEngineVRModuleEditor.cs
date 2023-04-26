//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using HTC.UnityPlugin.Vive;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public static class UnityVRModuleEditor
    {
#if UNITY_5_5_OR_NEWER && !UNITY_2020_1_OR_NEWER
        [InitializeOnLoadMethod]
        private static void StartCheckEnforceInputManagerBindings()
        {
            EditorApplication.update += EnforceInputManagerBindings;
        }

        // Add joystick axis input bindings to InputManager
        // See OpenVR/Oculus left/right controllers mapping at
        // https://docs.unity3d.com/Manual/OpenVRControllers.html
        private static void EnforceInputManagerBindings()
        {
            try
            {
                var inputSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
                if (inputSettings == null || inputSettings.Length <= 0) { return; }

                var serializedInputSettings = new SerializedObject(inputSettings);

                var axisObj = new Axis();
                for (int i = 0, imax = UnityEngineVRModule.GetUnityAxisCount(); i < imax; ++i)
                {
                    axisObj.name = UnityEngineVRModule.GetUnityAxisNameByIndex(i);
                    axisObj.axis = UnityEngineVRModule.GetUnityAxisIdByIndex(i) - 1;
                    BindAxis(serializedInputSettings, axisObj);
                }

                EditorApplication.update -= EnforceInputManagerBindings;
            }
            catch (Exception e)
            {
                Debug.LogError(e + " Failed to apply Vive Input Utility input manager bindings.");
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
            public float sensitivity = 5.0f;
            public bool snap = false;
            public bool invert = false;
            public int type = 2;
            public int axis = 0;
            public int joyNum = 0;
        }

        private static void BindAxis(SerializedObject serializedInputSettings, Axis axis)
        {
            var axesProperty = serializedInputSettings.FindProperty("m_Axes");

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
            serializedInputSettings.ApplyModifiedProperties();

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
            serializedInputSettings.ApplyModifiedProperties();
        }
#endif
    }

    public class UnityEngineVRSymbolRequirementCollection : VRModuleManagerEditor.SymbolRequirementCollection
    {
        public UnityEngineVRSymbolRequirementCollection()
        {
            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_XR_GENERAL_SETTINGS",
                reqTypeNames = new string[] { "UnityEngine.XR.Management.XRGeneralSettings" },
                reqFileNames = new string[] { "XRGeneralSettings.cs" },
            });

            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_XR_PACKAGE_METADATA_STORE",
                reqTypeNames = new string[] { "UnityEditor.XR.Management.Metadata.XRPackageMetadataStore" },
                reqFileNames = new string[] { "XRPackageMetadata.cs" },
            });

            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_OPENXR",
                reqTypeNames = new string[]
                {
                    "UnityEditor.XR.OpenXR.OpenXRProjectValidation",
                    "UnityEditor.XR.OpenXR.OpenXRProjectValidationRulesSetup",
                    "UnityEngine.XR.OpenXR.OpenXRSettings",
                    "UnityEngine.XR.OpenXR.Features.OpenXRFeature",
                },
                reqFileNames = new string[]
                {
                    "OpenXRProjectValidation.cs",
                    "OpenXRProjectValidationRulesSetup.cs",
                    "OpenXRFeature.cs",
                },
            });

            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_OPENXR_PLUGIN_POSE_CONTROL",
                reqTypeNames = new string[]
                {
                    "UnityEngine.XR.OpenXR.Input.PoseControl",
                },
                reqFileNames = new string[]
                {
                    "PoseControl.cs",
                },
            });

            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_UIS_POSE_CONTROL",
                reqTypeNames = new string[]
                {
                    "UnityEngine.InputSystem.XR.PoseControl",
                },
                reqFileNames = new string[]
                {
                    "PoseControl.cs",
                },
            });
        }
    }
}