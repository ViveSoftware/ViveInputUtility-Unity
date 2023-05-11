//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEditor;
using UnityEngine;
using HTC.UnityPlugin.VRModuleManagement;

namespace HTC.UnityPlugin.Vive
{
    public static partial class VIUSettingsEditor
    {
        public static bool canSupportSimulator
        {
            get { return SimulatorSettings.instance.canSupport; }
        }

        public static bool supportSimulator
        {
            get { return SimulatorSettings.instance.support; }
            set { SimulatorSettings.instance.support = value; }
        }

        private class SimulatorSettings : VRPlatformSetting
        {
            private Foldouter m_foldouter = new Foldouter();

            public static SimulatorSettings instance { get; private set; }

            public SimulatorSettings() { instance = this; }

            public override int order { get { return 0; } }

            protected override BuildTargetGroup requirdPlatform { get { return BuildTargetGroup.Unknown; } }

            public override bool canSupport
            {
                get { return true; }
            }

            public override bool support
            {
                get { return canSupport && VIUSettings.activateSimulatorModule; }
                set { VIUSettings.activateSimulatorModule = value; }
            }

            public override void OnPreferenceGUI()
            {
                const string title = "Simulator";
                if (canSupport)
                {
                    var wasSupported = support;
                    support = m_foldouter.ShowFoldoutButtonOnToggleEnabled(new GUIContent(title, "If checked, the simulator will activated automatically if no other valid VR devices found."), wasSupported);
                    s_symbolChanged |= wasSupported != support;
                }
                else
                {
                    Foldouter.ShowFoldoutBlankWithDisbledToggle(new GUIContent(title));
                }

                if (support && m_foldouter.isExpended)
                {
                    if (support) { EditorGUI.BeginChangeCheck(); } else { GUI.enabled = false; }
                    {
                        EditorGUI.indentLevel += 2;
                        var origLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 195;
                        VIUSettings.simulatorAutoTrackMainCamera = EditorGUILayout.ToggleLeft(new GUIContent("Enable Auto Camera Tracking", "Main camera only"), VIUSettings.simulatorAutoTrackMainCamera);
                        VIUSettings.enableSimulatorKeyboardMouseControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Keyboard-Mouse Control", "You can also control Simulator devices by handling VRModule.Simulator.onUpdateDeviceState event."), VIUSettings.enableSimulatorKeyboardMouseControl);

                        if (!VIUSettings.enableSimulatorKeyboardMouseControl && support) { GUI.enabled = false; }
                        {
                            EditorGUI.indentLevel++;
                            VIUSettings.simulateTrackpadTouch = EditorGUILayout.Toggle(new GUIContent("Simulate Trackpad Touch", VIUSettings.SIMULATE_TRACKPAD_TOUCH_TOOLTIP), VIUSettings.simulateTrackpadTouch);
                            VIUSettings.simulatorKeyMoveSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Move Speed", VIUSettings.SIMULATOR_KEY_MOVE_SPEED_TOOLTIP), VIUSettings.simulatorKeyMoveSpeed);
                            VIUSettings.simulatorKeyRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Rotate Speed", VIUSettings.SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP), VIUSettings.simulatorKeyRotateSpeed);
                            VIUSettings.simulatorMouseRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Mouse Rotate Speed"), VIUSettings.simulatorMouseRotateSpeed);
                            EditorGUI.indentLevel--;
                        }
                        if (!VIUSettings.enableSimulatorKeyboardMouseControl && support) { GUI.enabled = true; }

                        VIUSettings.simulatorRightControllerModel = (VRModuleDeviceModel)EditorGUILayout.EnumPopup("Device Index 1 (Right)", VIUSettings.simulatorRightControllerModel);
                        VIUSettings.simulatorLeftControllerModel = (VRModuleDeviceModel)EditorGUILayout.EnumPopup("Device Index 2 (Left)", VIUSettings.simulatorLeftControllerModel);
                        VIUSettings.simulatorOtherModel = (VRModuleDeviceModel)EditorGUILayout.EnumPopup("Other Device Index", VIUSettings.simulatorOtherModel);

                        EditorGUIUtility.labelWidth = origLabelWidth;
                        EditorGUI.indentLevel -= 2;
                    }
                    if (support) { s_guiChanged |= EditorGUI.EndChangeCheck(); } else { GUI.enabled = true; }
                }
            }
        }
    }
}