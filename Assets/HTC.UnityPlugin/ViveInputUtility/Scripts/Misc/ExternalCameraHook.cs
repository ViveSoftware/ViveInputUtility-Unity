//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEngine;
using Pose = HTC.UnityPlugin.PoseTracker.Pose;

// This script creates and handles SteamVR_ExternalCamera using viveRole property
[DisallowMultipleComponent]
public class ExternalCameraHook : SingletonBehaviour<ExternalCameraHook>, INewPoseListener, IViveRoleComponent
{
    public const string AUTO_LOAD_CONFIG_PATH = "externalcamera.cfg";

    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.ExternalCamera);
    [SerializeField]
    private Transform m_origin;
    [SerializeField]
    private string m_configPath = AUTO_LOAD_CONFIG_PATH;

    private bool m_quadViewSwitch = false;
    private bool m_configInterfaceSwitch = true;
    private GameObject m_configUI = null;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    public Transform origin { get { return m_origin; } set { m_origin = value; } }

    public bool isTrackingDevice { get { return isActiveAndEnabled && VRModule.IsValidDeviceIndex(m_viveRole.GetDeviceIndex()); } }

    public bool quadViewEnabled
    {
        get { return m_quadViewSwitch; }
        set
        {
            if (m_quadViewSwitch != value)
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
            if (m_configInterfaceSwitch != value)
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
#if VIU_STEAMVR
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
    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            UpdateActivity();
        }
    }
#endif

#if VIU_STEAMVR
    private static bool s_isAutoLoaded;

    private SteamVR_ExternalCamera m_externalCamera;
    private Pose m_staticExCamPose = Pose.identity;

    public string configPath
    {
        get
        {
            return m_configPath;
        }
        set
        {
            m_configPath = value;
            if (m_externalCamera != null && File.Exists(m_configPath))
            {
                m_externalCamera.configPath = m_configPath;
                m_externalCamera.ReadConfig();
            }
        }
    }

    public SteamVR_ExternalCamera externalCamera { get { return m_externalCamera; } }

    [RuntimeInitializeOnLoadMethod]
    private static void AutoLoadConfig()
    {
        if (s_isAutoLoaded) { return; }
        s_isAutoLoaded = true;

        var configPath = AUTO_LOAD_CONFIG_PATH;

        if (Active)
        {
            configPath = Instance.m_configPath;
        }

        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            SteamVR_Render.instance.externalCameraConfigPath = string.Empty;

            var oldExternalCam = SteamVR_Render.instance.externalCamera;
            if (oldExternalCam != null)
            {
                if (oldExternalCam.transform.parent != null && oldExternalCam.transform.parent.GetComponent<SteamVR_ControllerManager>() != null)
                {
                    Destroy(oldExternalCam.transform.parent.gameObject);
                    SteamVR_Render.instance.externalCamera = null;
                }
            }

            Initialize();
        }
    }

    protected override void OnSingletonBehaviourInitialized()
    {
        if (Instance.m_origin == null)
        {
            // try find vr camera
            if (SteamVR_Render.Top() != null)
            {
                Instance.m_origin = SteamVR_Render.Top().transform.parent;
            }
            else
            {
                foreach (var cam in Camera.allCameras)
                {
                    if (!cam.enabled) { continue; }
#if UNITY_5_4_OR_NEWER
                    // try find vr camera eye
                    if (cam.stereoTargetEye != StereoTargetEyeMask.Both) { continue; }
#endif
                    Instance.m_origin = cam.transform.parent;
                }
            }
        }
    }

    private void OnEnable()
    {
        if (IsInstance)
        {
            m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
            VivePose.AddNewPosesListener(this);

            OnDeviceIndexChanged(m_viveRole.GetDeviceIndex());
        }
    }

    private void OnDisable()
    {
        if (IsInstance)
        {
            m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
            VivePose.RemoveNewPosesListener(this);

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
            Pose.SetPose(transform, m_staticExCamPose, m_origin);
        }
    }

    public virtual void AfterNewPoses() { }

#if VIU_EXTERNAL_CAMERA_SWITCH
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.RightShift))
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
#endif

    private void OnDeviceIndexChanged(uint deviceIndex)
    {
        m_quadViewSwitch = isTrackingDevice;

        UpdateActivity();
    }

    private void UpdateActivity()
    {
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
                m_externalCamera.configPath = m_configPath;
                m_externalCamera.ReadConfig();
            }
        }

        if (m_externalCamera != null)
        {
            m_externalCamera.gameObject.SetActive(value);
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
        m_staticExCamPose = Pose.identity;
    }

#else
    public string configPath { get { return m_configPath; } set { m_configPath = value; } }

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