//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public partial class VIUSettings : ScriptableObject
    {
        public const string ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_TOOLTIP = "Use render model proivded by Oculus runtime. If disabled, use VIU fallback default model instead";
        public const string ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_TOOLTIP = "Use render model proivded by Oculus runtime. If disabled, use VIU fallback default model instead";
        public const string ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP = "Enable hand model attached to controller render model";

        public const bool ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE = true;
        public const bool ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_DEFAULT_VALUE = true;
        public const bool ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_DEFAULT_VALUE = true;
        public const bool ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_DEFAULT_VALUE = false;

        [SerializeField]
        private bool m_activateOculusVRModule = ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE;
        [SerializeField]
        private string m_oculusVRAndroidManifestPath = string.Empty;
        [SerializeField, Tooltip(ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_TOOLTIP)]
        private bool m_enableOculusSDKHandRenderModel = ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_DEFAULT_VALUE;
        [SerializeField, Tooltip(ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_TOOLTIP)]
        private bool m_enableOculusSDKControllerRenderModel = ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_DEFAULT_VALUE;
        [SerializeField, Tooltip(ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_TOOLTIP)]
        private bool m_enableOculusSDKControllerRenderModelSkeleton = ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_DEFAULT_VALUE;

        [SerializeField]
        private GameObject m_oculusVRControllerPrefab;

        public static bool activateOculusVRModule { get { return Instance == null ? ACTIVATE_OCULUS_VR_MODULE_DEFAULT_VALUE : s_instance.m_activateOculusVRModule; } set { if (Instance != null) { Instance.m_activateOculusVRModule = value; } } }
        public static string oculusVRAndroidManifestPath { get { return Instance == null ? "" : s_instance.m_oculusVRAndroidManifestPath; } set { if (Instance != null) { Instance.m_oculusVRAndroidManifestPath = value; } } }
        public static bool EnableOculusSDKHandRenderModel { get { return Instance == null ? ENABLE_OCULUS_SDK_HAND_RENDER_MODEL_DEFAULT_VALUE : s_instance.m_enableOculusSDKHandRenderModel; } set { if (Instance != null) { Instance.m_enableOculusSDKHandRenderModel = value; } } }
        public static bool EnableOculusSDKControllerRenderModel { get { return Instance == null ? ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_DEFAULT_VALUE : s_instance.m_enableOculusSDKControllerRenderModel; } set { if (Instance != null) { Instance.m_enableOculusSDKControllerRenderModel = value; } } }
        public static bool EnableOculusSDKControllerRenderModelSkeleton { get { return Instance == null ? ENABLE_OCULUS_SDK_CONTROLLER_RENDER_MODEL_SKELETON_DEFAULT_VALUE : s_instance.m_enableOculusSDKControllerRenderModelSkeleton; } set { if (Instance != null) { Instance.m_enableOculusSDKControllerRenderModelSkeleton = value; } } }

        public static GameObject oculusVRControllerPrefab { get { return Instance == null ? null : Instance.m_oculusVRControllerPrefab; } set { if (Instance != null) { Instance.m_oculusVRControllerPrefab = value; } } }
    }
}