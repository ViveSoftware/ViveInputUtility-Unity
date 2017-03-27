using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
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
    private string m_configPath = "./vive_external_camera.cfg";

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

    public ViveRoleProperty viveRole { get { return m_viveRole; } }
    public Transform origin { get { return m_origin; } set { m_origin = value; } }
    public SteamVR_ExternalCamera externalCamera { get { return m_externalCamera; } }
#if UNITY_EDITOR
    private void Reset()
    {
        var renders = FindObjectsOfType<SteamVR_Render>();
        if (renders == null || renders.Length == 0)
        {
            renders = new SteamVR_Render[] { gameObject.AddComponent<SteamVR_Render>() };
        }

        foreach(var render in renders)
        {
            m_configPath = render.externalCameraConfigPath;
            render.externalCameraConfigPath = string.Empty;
        }
    }
#endif
    protected virtual void Awake()
    {
        if (SteamVR_Render.instance.externalCamera != null)
        {
            Debug.LogWarning("External camera already exist");
        }
    }

    protected virtual void OnEnable()
    {
        VivePose.AddNewPosesListener(this);
        SetValid(m_isValid = VivePose.IsValidEx(viveRole.roleType, viveRole.roleValue));
    }

    protected virtual void OnDisable()
    {
        VivePose.RemoveNewPosesListener(this);
        SetValid(m_isValid = false);
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        var valid = VivePose.IsValidEx(viveRole.roleType, viveRole.roleValue);

        if (valid)
        {
            TrackPose(VivePose.GetPoseEx(viveRole.roleType, viveRole.roleValue), m_origin);
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
            var ctrlMgr = Instantiate(prefab);
            var extCam = ctrlMgr.transform.GetChild(0);
            extCam.gameObject.name = "External Camera";
            extCam.SetParent(transform, false);
            DestroyImmediate(extCam.GetComponent<SteamVR_TrackedObject>());
            DestroyImmediate(ctrlMgr);

            m_externalCamera = extCam.GetComponent<SteamVR_ExternalCamera>();
            SteamVR_Render.instance.externalCamera = m_externalCamera;
            m_externalCamera.configPath = m_configPath;
            m_externalCamera.ReadConfig();
        }

        if (m_externalCamera != null)
        {
            m_externalCamera.gameObject.SetActive(value);
        }
    }
}