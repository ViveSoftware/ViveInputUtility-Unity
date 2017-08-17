//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEngine;

// This script creates and handles SteamVR_ExternalCamera using viveRole property
[DisallowMultipleComponent]
public class ExternalCameraHook : BasePoseTracker, INewPoseListener, IViveRoleComponent
{
    public const string AUTO_LOAD_CONFIG_PATH = "externalcamera.cfg";

    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.ExternalCamera);
    [SerializeField]
    private Transform m_origin;
    [SerializeField]
    private string m_configPath = AUTO_LOAD_CONFIG_PATH;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }
    public Transform origin { get { return m_origin; } set { m_origin = value; } }

#if VIU_STEAMVR
    private static bool s_isAutoLoaded;
    private static ExternalCameraHook s_hook;

    private SteamVR_ExternalCamera m_externalCamera;
    private bool m_isValid;

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

        if (s_hook != null && !string.IsNullOrEmpty(s_hook.m_configPath))
        {
            configPath = s_hook.m_configPath;
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

            if (s_hook == null)
            {
                var hookObj = new GameObject("[ExternalCamera]");
                s_hook = hookObj.AddComponent<ExternalCameraHook>();
                s_hook.m_configPath = configPath;

                // try find vr camera
                if (SteamVR_Render.Top() != null)
                {
                    s_hook.m_origin = SteamVR_Render.Top().transform.parent;
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
                        s_hook.m_origin = cam.transform.parent;
                    }
                }
            }
        }
    }

    protected virtual void Awake()
    {
        if (s_hook != null)
        {
            Debug.LogWarning("Duplicate ExternalCameraHook found");
        }
        else
        {
            s_hook = this;
            AutoLoadConfig();
        }
    }

    protected virtual void Start() { }

    protected virtual void OnEnable()
    {
        m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
        OnDeviceIndexChanged(m_viveRole.GetDeviceIndex());

        //VivePose.AddNewPosesListener(this);

        //SetValid(m_isValid = VRModule.IsValidDeviceIndex(viveRole.GetDeviceIndex()) && SteamVR_Render.Top() != null);
    }

    protected virtual void OnDisable()
    {
        m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
        OnDeviceIndexChanged(VRModule.INVALID_DEVICE_INDEX);
        //VivePose.RemoveNewPosesListener(this);
        //SetValid(m_isValid = false);
    }

    private void OnDeviceIndexChanged(uint deviceIndex)
    {
        SetValid(VRModule.IsValidDeviceIndex(deviceIndex));

        if (m_externalCamera != null && m_externalCamera.gameObject.activeSelf)
        {
            VivePose.AddNewPosesListener(this);
        }
        else
        {
            VivePose.RemoveNewPosesListener(this);
        }
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        if (VivePose.IsValid(m_viveRole.GetDeviceIndex()))
        {
            TrackPose(VivePose.GetPose(m_viveRole.GetDeviceIndex()), m_origin);
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

    private void Awake() { }

    protected virtual void Start()
    {
        Debug.LogWarning("SteamVR plugin not found! install it to enable ExternalCamera!");
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses() { }

    public virtual void AfterNewPoses() { }

#endif
}