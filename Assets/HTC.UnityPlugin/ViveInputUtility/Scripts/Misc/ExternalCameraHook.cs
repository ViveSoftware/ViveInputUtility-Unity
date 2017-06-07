//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using System.IO;
using UnityEngine;

// This script creates and handles SteamVR_ExternalCamera using viveRole property
[DisallowMultipleComponent]
public class ExternalCameraHook : BasePoseTracker, INewPoseListener
{
    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(TrackerRole.Tracker1);
    [SerializeField]
    private Transform m_origin;
    [SerializeField]
    private string m_configPath = "externalcamera.cfg";

    public ViveRoleProperty viveRole { get { return m_viveRole; } }
    public Transform origin { get { return m_origin; } set { m_origin = value; } }

#if VIU_STEAMVR
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

    protected virtual void Awake()
    {
        var oldExternalCam = SteamVR_Render.instance.externalCamera;
        if (oldExternalCam != null)
        {
            if (string.IsNullOrEmpty(m_configPath))
            {
                m_configPath = SteamVR_Render.instance.externalCameraConfigPath;
            }

            SteamVR_Render.instance.externalCameraConfigPath = string.Empty;

            if (oldExternalCam.transform.parent != null && oldExternalCam.transform.parent.GetComponent<SteamVR_ControllerManager>() != null)
            {
                DestroyImmediate(oldExternalCam.transform.parent.gameObject);
                SteamVR_Render.instance.externalCamera = null;
            }
        }
    }

    protected virtual void Start() { }

    protected virtual void OnEnable()
    {
        VivePose.AddNewPosesListener(this);

        SetValid(m_isValid = VRModule.GetCurrentDeviceState(viveRole.GetDeviceIndex()).isPoseValid && SteamVR_Render.Top() != null);
    }

    protected virtual void OnDisable()
    {
        VivePose.RemoveNewPosesListener(this);
        SetValid(m_isValid = false);
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        var valid = VRModule.GetCurrentDeviceState(viveRole.GetDeviceIndex()).isPoseValid && SteamVR_Render.Top() != null;

        if (valid)
        {
            TrackPose(VivePose.GetPose(viveRole.GetDeviceIndex()), m_origin);
        }

        if (ChangeProp.Set(ref m_isValid, valid))
        {
            SetValid(m_isValid);
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