//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.IO;
using UnityEngine;
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive
{
    // This script creates and handles SteamVR_ExternalCamera using viveRole property
    [AddComponentMenu("VIU/Hooks/External Camera Hook", 9)]
    [DisallowMultipleComponent]
    public class ExternalCameraHook : SingletonBehaviour<ExternalCameraHook>, INewPoseListener, IViveRoleComponent
    {
        [Obsolete("Use VIUSettings.EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE instead.")]
        public const string AUTO_LOAD_CONFIG_PATH = "externalcamera.cfg";

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.ExternalCamera);
        [SerializeField]
        private Transform m_origin;
        [SerializeField]
        private string m_configPath = string.Empty;

        private bool m_quadViewSwitch = false;
        private bool m_configInterfaceSwitch = true;
        private GameObject m_configUI = null;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public Transform origin { get { return m_origin; } set { m_origin = value; } }

        public bool isTrackingDevice { get { return isActiveAndEnabled && VRModule.IsValidDeviceIndex(m_viveRole.GetDeviceIndex()); } }

        public string configPath
        {
            get
            {
                return m_configPath;
            }
            set
            {
                m_configPath = value;
#if VIU_STEAMVR && UNITY_STANDALONE
                if (m_externalCamera != null && !string.IsNullOrEmpty(m_configPath) && File.Exists(m_configPath))
                {
                    m_externalCamera.configPath = m_configPath;
                    m_externalCamera.ReadConfig();
                }
#endif
            }
        }

        public bool quadViewEnabled
        {
            get { return m_quadViewSwitch; }
            set
            {
                if (IsInstance && m_quadViewSwitch != value)
                {
                    m_quadViewSwitch = value;
                    UpdateActivity();
                }
            }
        }

        public bool configInterfaceEnabled
        {
            get { return m_configInterfaceSwitch; }
            set
            {
                if (IsInstance && m_configInterfaceSwitch != value)
                {
                    m_configInterfaceSwitch = value;
                    UpdateActivity();
                }
            }
        }

        public bool isQuadViewActive
        {
            get
            {
#if VIU_STEAMVR && UNITY_STANDALONE
                return isActiveAndEnabled && m_externalCamera != null && m_externalCamera.isActiveAndEnabled;
#else
                return false;
#endif
            }
        }

        public bool isConfigInterfaceActive
        {
            get
            {
                return isActiveAndEnabled && m_configUI != null && m_configUI.activeSelf;
            }
        }

        static ExternalCameraHook()
        {
            SetDefaultInitGameObjectGetter(DefaultInitGameObject);
        }

        private static GameObject DefaultInitGameObject()
        {
            var go = new GameObject("[ExternalCamera]");
            go.transform.SetParent(VRModule.Instance.transform, false);
            return go;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_configPath = VIUSettings.EXTERNAL_CAMERA_CONFIG_FILE_PATH_DEFAULT_VALUE;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                UpdateActivity();
            }
        }
#endif

#if VIU_STEAMVR && UNITY_STANDALONE
        private SteamVR_ExternalCamera m_externalCamera;
        private RigidPose m_staticExCamPose = RigidPose.identity;

        public SteamVR_ExternalCamera externalCamera { get { return m_externalCamera; } }

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (VIUSettings.autoLoadExternalCameraConfigOnStart)
            {
                if (VRModule.Active && VRModule.activeModule != VRModuleActiveEnum.Uninitialized)
                {
                    AutoLoadConfig();
                }
                else
                {
                    VRModule.onActiveModuleChanged += OnActiveModuleChanged;
                }
            }
        }

        private static void OnActiveModuleChanged(VRModuleActiveEnum activatedModule)
        {
            if (activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged -= OnActiveModuleChanged;
                AutoLoadConfig();
            }
        }

        private static void AutoLoadConfig()
        {
            Initialize();

            if (string.IsNullOrEmpty(Instance.m_configPath))
            {
                Instance.m_configPath = VIUSettings.externalCameraConfigFilePath;
            }

            LoadConfigFromFile(Instance.m_configPath);
        }

        /// <summary>
        /// Load config form file if the file exist.
        /// Will create an ExternalCameraHook instance into scene if config is availabile and there was no instance.
        /// </summary>
        /// <param name="">The config file path.</param>
        /// <returns>true if config file loaded and external camera instance is created successfully.</returns>
        public static bool LoadConfigFromFile(string path)
        {
            if (!SteamVR.active || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            Instance.configPath = path;
            Instance.UpdateActivity();
            return true;
        }

        private static bool m_defaultExCamResolved;
        private static void ResolveDefaultExCam()
        {
            if (m_defaultExCamResolved || VRModule.activeModule != VRModuleActiveEnum.SteamVR || !SteamVR.active)
            {
                if (Active && (VRModule.activeModule != VRModuleActiveEnum.SteamVR || !SteamVR.active)) { Instance.m_quadViewSwitch = false; }
                return;
            }
            m_defaultExCamResolved = true;

            SteamVR_Render.instance.externalCameraConfigPath = string.Empty;

            var oldExternalCam = SteamVR_Render.instance.externalCamera;
            if (oldExternalCam != null)
            {
                SteamVR_Render.instance.externalCamera = null;
                // To prevent SteamVR_ExternalCamera from setting invalid(0f) sceneResolutionScale value in OnDisable()
                oldExternalCam.config.sceneResolutionScale = 0f;

#if !VIU_STEAMVR_2_0_0_OR_NEWER
                if (oldExternalCam.transform.parent != null && oldExternalCam.transform.parent.GetComponent<SteamVR_ControllerManager>() != null)
#else
                if (oldExternalCam.transform.parent != null && oldExternalCam.transform.parent.GetComponentInChildren<SteamVR_TrackedObject>() != null)
#endif
                {
                    Destroy(oldExternalCam.transform.parent.gameObject);
                }
                else
                {
                    Destroy(oldExternalCam.gameObject);
                }
            }
        }

        private void OnEnable()
        {
            if (IsInstance)
            {
                m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
                OnDeviceIndexChanged(m_viveRole.GetDeviceIndex());
            }
        }

        private void OnDisable()
        {
            if (IsInstance)
            {
                m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
                OnDeviceIndexChanged(VRModule.INVALID_DEVICE_INDEX);
            }
        }

        private void OnDeviceIndexChanged(uint deviceIndex)
        {
            if (IsInstance)
            {
                m_quadViewSwitch = isTrackingDevice;
                UpdateActivity();
            }
        }

        public virtual void BeforeNewPoses() { }

        public virtual void OnNewPoses()
        {
            var deviceIndex = m_viveRole.GetDeviceIndex();

            if (VRModule.IsValidDeviceIndex(deviceIndex))
            {
                m_staticExCamPose = VivePose.GetPose(deviceIndex);
            }

            if (isQuadViewActive)
            {
                RigidPose.SetPose(transform, m_staticExCamPose, m_origin);
            }
        }

        public virtual void AfterNewPoses() { }

        private void Update()
        {
            if (VIUSettings.enableExternalCameraSwitch && Input.GetKeyDown(VIUSettings.externalCameraSwitchKey) && (VIUSettings.externalCameraSwitchKeyModifier != KeyCode.None && Input.GetKey(VIUSettings.externalCameraSwitchKeyModifier)))
            {
                if (!isQuadViewActive)
                {
                    m_quadViewSwitch = true;
                    m_configInterfaceSwitch = true;
                }
                else
                {
                    if (m_configInterfaceSwitch)
                    {
                        m_configInterfaceSwitch = false;
                    }
                    else
                    {
                        m_quadViewSwitch = false;
                        m_configInterfaceSwitch = false;
                    }
                }

                UpdateActivity();
            }
        }

        private void UpdateActivity()
        {
            ResolveDefaultExCam();

            if (!isActiveAndEnabled)
            {
                InternalSetQuadViewActive(false);
                InternalSetConfigInterfaceActive(false);
            }
            else
            {
                InternalSetQuadViewActive(m_quadViewSwitch);
                InternalSetConfigInterfaceActive(isQuadViewActive && m_configInterfaceSwitch);
            }
        }

        private void InternalSetQuadViewActive(bool value)
        {
            if (value && m_externalCamera == null && !string.IsNullOrEmpty(m_configPath) && File.Exists(m_configPath))
            {
                // don't know why SteamVR_ExternalCamera must be instantiated from the prefab
                // when create SteamVR_ExternalCamera using AddComponent, errors came out when disabling
                var prefab = Resources.Load<GameObject>("SteamVR_ExternalCamera");
                if (prefab == null)
                {
                    Debug.LogError("SteamVR_ExternalCamera prefab not found!");
                }
                else
                {
                    var ctrlMgr = Instantiate(prefab);
                    var extCam = ctrlMgr.transform.GetChild(0);
                    extCam.gameObject.name = "SteamVR External Camera";
                    extCam.SetParent(transform, false);
                    DestroyImmediate(extCam.GetComponent<SteamVR_TrackedObject>());
                    DestroyImmediate(ctrlMgr);

                    m_externalCamera = extCam.GetComponent<SteamVR_ExternalCamera>();
                    SteamVR_Render.instance.externalCamera = m_externalCamera;

                    // resolve config file
                    m_externalCamera.enabled = false;
                    m_externalCamera.configPath = m_configPath;
                    m_externalCamera.ReadConfig();
                    m_externalCamera.enabled = true; // to preserve sceneResolutionScale on enabled

                    // resolve RenderTexture
                    m_externalCamera.AttachToCamera(SteamVR_Render.Top());
                    var w = Screen.width / 2;
                    var h = Screen.height / 2;
                    var cam = m_externalCamera.GetComponentInChildren<Camera>();
                    if (cam.targetTexture == null || cam.targetTexture.width != w || cam.targetTexture.height != h)
                    {
                        var tex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, QualitySettings.activeColorSpace == ColorSpace.Linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
                        tex.antiAliasing = QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing;
                        cam.targetTexture = tex;
                    }
                }
            }

            if (m_externalCamera != null)
            {
                m_externalCamera.gameObject.SetActive(value);

                if (value)
                {
                    VivePose.AddNewPosesListener(this);
                }
                else
                {
                    VivePose.RemoveNewPosesListener(this);
                }
            }
        }

        private void InternalSetConfigInterfaceActive(bool value)
        {
            if (value && m_configUI == null)
            {
                var prefab = Resources.Load<GameObject>("VIUExCamConfigInterface");
                if (prefab == null)
                {
                    Debug.LogError("VIUExCamConfigInterface prefab not found!");
                }
                else
                {
                    m_configUI = Instantiate(prefab);
                }
            }

            if (m_configUI != null)
            {
                m_configUI.SetActive(value);
            }
        }

        public void Recenter()
        {
            m_staticExCamPose = RigidPose.identity;
        }

#else
        protected virtual void Start()
        {
            Debug.LogWarning("SteamVR plugin not found! install it to enable ExternalCamera!");
        }

        private void UpdateActivity() { }

        public virtual void BeforeNewPoses() { }

        public virtual void OnNewPoses() { }

        public virtual void AfterNewPoses() { }

        public void Recenter() { }
#endif
    }
}