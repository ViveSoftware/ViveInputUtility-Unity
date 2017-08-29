//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEngine;

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
    [SerializeField]
    private bool m_quadViewEnabled = true;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    public Transform origin { get { return m_origin; } set { m_origin = value; } }

    public bool quadViewEnabled
    {
        get { return m_quadViewEnabled; }
        set
        {
            if (m_quadViewEnabled != value)
            {
                m_quadViewEnabled = value;
                UpdateExCamActivity();
            }
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
        if (Application.isPlaying && enabled)
        {
            UpdateExCamActivity();
        }
    }
#endif

#if VIU_STEAMVR
    private static bool s_isAutoLoaded;

    private SteamVR_ExternalCamera m_externalCamera;
    private bool m_staticExCamEnabled = false;
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

        if (Active && !string.IsNullOrEmpty(Instance.m_configPath))
        {
            configPath = Instance.m_configPath;
        }

        if (File.Exists(configPath))
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
            m_quadViewEnabled = true;
            UpdateExCamActivity();
        }
    }

    private void OnDisable()
    {
        if (IsInstance)
        {
            m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
            UpdateExCamActivity();
        }
    }

#if VIU_EXTERNAL_CAMERA_SWITCH
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.RightShift))
        {
            if (!m_staticExCamEnabled && !VRModule.IsValidDeviceIndex(m_viveRole.GetDeviceIndex()))
            {
                m_quadViewEnabled = false;
                m_staticExCamEnabled = true;
            }

            m_quadViewEnabled = !m_quadViewEnabled;

            UpdateExCamActivity();
        }
    }
#endif

    private void OnDeviceIndexChanged(uint deviceIndex)
    {
        UpdateExCamActivity();
    }

    private void UpdateExCamActivity()
    {
        if (!enabled)
        {
            SetValid(false);
            VivePose.RemoveNewPosesListener(this);
        }
        else if (VRModule.IsValidDeviceIndex(m_viveRole.GetDeviceIndex()))
        {
            SetValid(m_quadViewEnabled);
            VivePose.AddNewPosesListener(this);
        }
        else if (m_staticExCamEnabled)
        {
            SetValid(m_quadViewEnabled);
            VivePose.AddNewPosesListener(this);
        }
        else
        {
            SetValid(false);
            VivePose.RemoveNewPosesListener(this);
        }
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        var deviceIndex = m_viveRole.GetDeviceIndex();
        var deviceValid = VRModule.IsValidDeviceIndex(deviceIndex);

        if (deviceValid)
        {
            m_staticExCamPose = VivePose.GetPose(deviceIndex);
        }

        if (deviceValid || m_staticExCamEnabled)
        {
            Pose.SetPose(transform, m_staticExCamPose, m_origin);
        }
    }

    public virtual void AfterNewPoses() { }

    private void SetValid(bool value)
    {
        if (value && m_externalCamera == null && SteamVR_Render.instance.externalCamera == null && File.Exists(m_configPath))
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

#else
    public string configPath { get { return m_configPath; } set { m_configPath = value; } }

    protected virtual void Start()
    {
        Debug.LogWarning("SteamVR plugin not found! install it to enable ExternalCamera!");
    }

    private void UpdateExCamActivity() { }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses() { }

    public virtual void AfterNewPoses() { }
#endif
}