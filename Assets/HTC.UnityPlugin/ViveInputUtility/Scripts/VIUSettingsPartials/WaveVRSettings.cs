//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string ENABLE_WAVE_XR_RENDER_MODEL_TOOLTIP = "Use render model proivded by Wave runtime";

        public const bool ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE = true;
        public const bool SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE = false;
        public const bool WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER = true;
        public static readonly Vector3 WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE = new Vector3(0.0f, -0.15f, 0.0f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE = new Vector3(0.195f, -0.5f, 0.005f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE = new Vector3(-0.13f, 0.14f, 0.08f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE = new Vector3(0.0f, 0.0f, 0.35f);
        public static readonly Vector3 WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE = new Vector3(0.0f, 0.0f, 0.05f);
        public const bool ENABLE_WAVE_XR_RENDER_MODEL_DEFAULT_VALUE = true;

        [SerializeField]
        private bool m_activateWaveVRModule = ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private bool m_simulateWaveVR6DoFController = SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE;
        [SerializeField]
        private bool m_waveVRAddVirtualArmTo3DoFController = WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER;
        [SerializeField]
        private Vector3 m_waveVRVirtualNeckPosition = WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualElbowRestPosition = WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualArmExtensionOffset = WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualWristRestPosition = WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private Vector3 m_waveVRVirtualHandRestPosition = WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE;
        [SerializeField]
        private string m_waveVRAndroidManifestPath = string.Empty;
        [SerializeField, Tooltip(ENABLE_WAVE_XR_RENDER_MODEL_TOOLTIP)]
        private bool m_enableWaveXRRenderModel = ENABLE_WAVE_XR_RENDER_MODEL_DEFAULT_VALUE;

        public static bool activateWaveVRModule { get { return Instance == null ? ACTIVATE_WAVE_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateWaveVRModule; } set { if (Instance != null) { Instance.m_activateWaveVRModule = value; } } }
        public static bool simulateWaveVR6DofController { get { return Instance == null ? SIMULATE_WAVE_VR_6DOF_CONTROLLER_DEFAULT_VALUE : s_instance.m_simulateWaveVR6DoFController; } set { if (Instance != null) { Instance.m_simulateWaveVR6DoFController = value; } } }
        public static bool waveVRAddVirtualArmTo3DoFController { get { return Instance == null ? WAVE_VR_ADD_VIRTUAL_ARM_TO_3DOF_CONTROLLER : s_instance.m_waveVRAddVirtualArmTo3DoFController; } set { if (Instance != null) { Instance.m_waveVRAddVirtualArmTo3DoFController = value; } } }
        public static Vector3 waveVRVirtualNeckPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_NECK_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualNeckPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualNeckPosition = value; } } }
        public static Vector3 waveVRVirtualElbowRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_ELBOW_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualElbowRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualElbowRestPosition = value; } } }
        public static Vector3 waveVRVirtualArmExtensionOffset { get { return Instance == null ? WAVE_VR_VIRTUAL_ARM_EXTENSION_OFFSET_DEFAULT_VALUE : s_instance.m_waveVRVirtualArmExtensionOffset; } set { if (Instance != null) { Instance.m_waveVRVirtualArmExtensionOffset = value; } } }
        public static Vector3 waveVRVirtualWristRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_WRIST_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualWristRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualWristRestPosition = value; } } }
        public static Vector3 waveVRVirtualHandRestPosition { get { return Instance == null ? WAVE_VR_VIRTUAL_HAND_REST_POSITION_DEFAULT_VALUE : s_instance.m_waveVRVirtualHandRestPosition; } set { if (Instance != null) { Instance.m_waveVRVirtualHandRestPosition = value; } } }
        public static string waveVRAndroidManifestPath { get { return Instance == null ? string.Empty : s_instance.m_waveVRAndroidManifestPath; } set { if (Instance != null) { Instance.m_waveVRAndroidManifestPath = value; } } }
        public static bool enableWaveXRRenderModel { get { return Instance == null ? ENABLE_WAVE_XR_RENDER_MODEL_DEFAULT_VALUE : s_instance.m_enableWaveXRRenderModel; } set { if (Instance != null) { Instance.m_enableWaveXRRenderModel = value; } } }
    }
}