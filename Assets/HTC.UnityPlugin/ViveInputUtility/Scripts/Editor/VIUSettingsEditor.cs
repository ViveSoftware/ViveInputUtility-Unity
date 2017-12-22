//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public static class VIUSettingsEditor
    {
        private static GUIStyle s_boldStyle;

        public static class EnabledDevices
        {
            public class Device
            {
                public readonly string m_name;
                private readonly bool m_addLast;
                private bool m_enabled;
                public bool enabled
                {
                    get
                    {
                        Update();
                        return m_enabled;
                    }
                    set
                    {
                        Update();
                        if (m_enabled == value) { return; }
                        s_listDirty = true;
                        m_enabled = value;
                        if (value)
                        {
                            if (m_addLast) { s_devices.Add(m_name); }
                            else { s_devices.Insert(0, m_name); }
                        }
                        else
                        {
                            s_devices.Remove(m_name);
                        }
                    }
                }
                public Device(string name, bool addLast = false) { m_name = name; m_addLast = addLast; }
                public void Reset() { m_enabled = false; }
                public bool CheckSupport(string deviceName) { return !m_enabled && (m_enabled = m_name == deviceName); } // return true if confirmed
            }

            private static int s_updatedFrame = -1;
            private static bool s_listDirty;
            private static List<string> s_devices;

            public static readonly Device Oculus = new Device("Oculus");
            public static readonly Device OpenVR = new Device("OpenVR", true);

            private static void Update()
            {
                if (!ChangeProp.Set(ref s_updatedFrame, Time.frameCount)) { return; }
                UpdateDeviceList();

                Oculus.Reset();
                OpenVR.Reset();

                for (int i = s_devices.Count - 1; i >= 0; --i)
                {
                    if (Oculus.CheckSupport(s_devices[i])) { continue; }
                    if (OpenVR.CheckSupport(s_devices[i])) { continue; }
                }
            }

            private static void UpdateDeviceList()
            {
                if (s_devices == null) { s_devices = new List<string>(); }
                s_devices.Clear();
                if (!PlayerSettings.virtualRealitySupported) { return; }
#if UNITY_5_4
                s_devices.AddRange(UnityEditorInternal.VR.VREditor.GetVREnabledDevices(EditorUserBuildSettings.selectedBuildTargetGroup));
#elif UNITY_5_5_OR_NEWER
                s_devices.AddRange(UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
#endif
            }

            public static void ApplyChanges()
            {
                if (!s_listDirty) { return; }
                s_listDirty = false;
                if (s_devices.Count > 0 && !PlayerSettings.virtualRealitySupported)
                {
                    PlayerSettings.virtualRealitySupported = true;
                }
                else if (s_devices.Count == 0 && PlayerSettings.virtualRealitySupported)
                {
                    PlayerSettings.virtualRealitySupported = false;
                }
#if UNITY_5_4
                UnityEditorInternal.VR.VREditor.SetVREnabledDevices(EditorUserBuildSettings.selectedBuildTargetGroup, s_devices.ToArray());
#elif UNITY_5_5_OR_NEWER
                UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup, s_devices.ToArray());
#endif
            }
        }

        public static bool canSupportSimulatedDevice
        {
            get
            {
#if UNITY_STANDALONE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool supportSimulatedDevice
        {
            get
            {
                return canSupportSimulatedDevice && VIUSettings.activateSimulatorModule;
            }
            private set
            {
                VIUSettings.activateSimulatorModule = value;
            }
        }

        public static bool canSupportOpenVR
        {
            get
            {
#if UNITY_STANDALONE && (UNITY_5_3 || UNITY_5_4)
                return VRModule.isSteamVRPluginDetected;
#elif UNITY_STANDALONE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool supportOpenVR
        {
            get
            {
#if UNITY_5_3 || UNITY_5_4
                return canSupportOpenVR && VIUSettings.activateSteamVRModule;
#elif UNITY_5_5_OR_NEWER
                return canSupportOpenVR && (VIUSettings.activateSteamVRModule || VIUSettings.activateUnityNativeVRModule) && EnabledDevices.OpenVR.enabled;
#endif
            }
            set
            {
                if (VRModule.isSteamVRPluginDetected)
                {
                    VIUSettings.activateSteamVRModule = value;
                }
#if !(UNITY_5_3 || UNITY_5_4)
                EnabledDevices.OpenVR.enabled = value;
#endif
            }
        }

        private static string supportOpenVRWarnning
        {
            get
            {
                if (!VIUSettings.activateSteamVRModule || !VRModule.isSteamVRPluginDetected)
                {
#if UNITY_5_5 || UNITY_5_6
                    return "Vive Tracker not supported! Install SteamVR Plugin to get support.";
#elif UNITY_2017_1_OR_NEWER
                    return "Vive Tracker's input not supported! Install SteamVR Plugin to get support.";
#endif
                }
                return string.Empty;
            }
        }

        public static bool canSupportOculus
        {
            get
            {
#if UNITY_STANDALONE && UNITY_5_3 || UNITY_5_4
                return VRModule.isOculusVRPluginDetected;
#elif UNITY_STANDALONE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool supportOculus
        {
            get
            {
#if UNITY_5_3 || UNITY_5_4
                return VIUSettings.activateOculusVRModule && VRModule.isOculusVRPluginDetected;
#elif UNITY_5_5_OR_NEWER
                return canSupportOculus && (VIUSettings.activateOculusVRModule || VIUSettings.activateUnityNativeVRModule) && EnabledDevices.Oculus.enabled;
#endif
            }
            set
            {
                if (VRModule.isOculusVRPluginDetected)
                {
                    VIUSettings.activateOculusVRModule = value;
                }
#if !(UNITY_5_3 || UNITY_5_4)
                EnabledDevices.Oculus.enabled = value;
#endif
            }
        }

        [PreferenceItem("VIU Settings")]
        private static void OnVIUPreferenceGUI()
        {
            const float buttonIndent = 20f;

            if (s_boldStyle == null)
            {
                s_boldStyle = new GUIStyle();
                s_boldStyle.fontStyle = FontStyle.Bold;
            }

            EditorGUI.BeginChangeCheck();

            VIUSettings.enableBindingInterfaceSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Binding Interface Switch", VIUSettings.BIND_UI_SWITCH_TOOLTIP), VIUSettings.enableBindingInterfaceSwitch);
            VIUSettings.enableExternalCameraSwitch = EditorGUILayout.ToggleLeft(new GUIContent("Enable External Camera Switch", VIUSettings.EX_CAM_UI_SWITCH_TOOLTIP), VIUSettings.enableExternalCameraSwitch);

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Supported devices", s_boldStyle);

            const string supportSimulatorTitle = "Simulated Device";
            if (canSupportSimulatedDevice)
            {
                if (supportSimulatedDevice = EditorGUILayout.ToggleLeft(new GUIContent(supportSimulatorTitle, "If checked, the simulator will activated automatically if no other valid VR devices found."), supportSimulatedDevice))
                {
                    EditorGUI.indentLevel++;
                    VIUSettings.simulatorAutoTrackMainCamera = EditorGUILayout.ToggleLeft(new GUIContent("Auto Main Camera Tracking"), VIUSettings.simulatorAutoTrackMainCamera);
                    if (VIUSettings.enableSimulatorKeyboardMouseControl = EditorGUILayout.ToggleLeft(new GUIContent("Enable Keyboard-Mouse Control", "You can also handle VRModule.Simulator.onUpdateDeviceState by your self."), VIUSettings.enableSimulatorKeyboardMouseControl))
                    {
                        EditorGUI.indentLevel++;
                        VIUSettings.simulateTrackpadTouch = EditorGUILayout.Toggle(new GUIContent("Simulate Trackpad Touch", VIUSettings.SIMULATE_TRACKPAD_TOUCH_TOOLTIP), VIUSettings.simulateTrackpadTouch);
                        VIUSettings.simulatorKeyMoveSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Move Speed", VIUSettings.SIMULATOR_KEY_MOVE_SPEED_TOOLTIP), VIUSettings.simulatorKeyMoveSpeed);
                        VIUSettings.simulatorKeyRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Keyboard Rotate Speed", VIUSettings.SIMULATOR_KEY_ROTATE_SPEED_TOOLTIP), VIUSettings.simulatorKeyRotateSpeed);
                        VIUSettings.simulatorMouseRotateSpeed = EditorGUILayout.DelayedFloatField(new GUIContent("Mouse Rotate Speed"), VIUSettings.simulatorMouseRotateSpeed);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent(supportSimulatorTitle), false);
                GUI.enabled = true;
            }

            const string supportOpenVRTitle = "Vive (OpenVR compatible device)";
            if (canSupportOpenVR)
            {
                if (supportOpenVR = EditorGUILayout.ToggleLeft(new GUIContent(supportOpenVRTitle), supportOpenVR))
                {
                    var warnning = supportOpenVRWarnning;
                    if (!string.IsNullOrEmpty(warnning))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.HelpBox(warnning, MessageType.Warning);
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent(supportOpenVRTitle, "SteamVR Plugin not required."), false);
                GUI.enabled = true;
            }

            if (!canSupportOpenVR || (supportOpenVR && !VRModule.isSteamVRPluginDetected))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(buttonIndent);
                if (GUILayout.Button("Get SteamVR Plugin"))
                {
                    Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/32647");
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            const string supportOculusVRTitle = "Oculus Rift & Touch";
            if (canSupportOculus)
            {
                supportOculus = EditorGUILayout.ToggleLeft(new GUIContent(supportOculusVRTitle), supportOculus);
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft(new GUIContent(supportOculusVRTitle, "Oculus VR plugin required."), false);
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                GUILayout.Space(buttonIndent);
                if (GUILayout.Button("Get Oculus VR Plugin"))
                {
                    Application.OpenURL("https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/");
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EnabledDevices.ApplyChanges();

                var path = VIUSettings.loadedAssetPath;
                if (string.IsNullOrEmpty(path))
                {
                    VIUSettings.CreateAsset();
                }
            }

            //GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //if (GUILayout.Button("Reset"))
            //{
            //    var path = VIUSettings.loadedAssetPath;
            //    if (!string.IsNullOrEmpty(path))
            //    {
            //        AssetDatabase.DeleteAsset(path);
            //    }
            //}
            //GUILayout.EndHorizontal();
        }
    }
}