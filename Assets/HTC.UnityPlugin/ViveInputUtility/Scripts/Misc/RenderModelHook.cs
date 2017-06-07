//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

// This script creates and handles SteamVR_RenderModel using viveRole property or device index
[DisallowMultipleComponent]
public class RenderModelHook : BasePoseTracker, INewPoseListener, IViveRoleComponent
{
    public enum Mode
    {
        Disable,
        ViveRole,
        DeivceIndex,
    }

    public enum Index
    {
        None = -1,
        Hmd,
        Device1,
        Device2,
        Device3,
        Device4,
        Device5,
        Device6,
        Device7,
        Device8,
        Device9,
        Device10,
        Device11,
        Device12,
        Device13,
        Device14,
        Device15,
    }

    [SerializeField]
    private Mode m_mode = Mode.ViveRole;
    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
    [SerializeField]
    private bool m_applyTracking = false;
    [SerializeField]
    private Transform m_origin;
    [SerializeField]
    private Index m_deviceIndex = Index.Hmd;

    private uint m_currentDeviceIndex = ViveRole.INVALID_DEVICE_INDEX;
    private SupportedVRModule m_currentActiveModule;
    private GameObject m_modelObj;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    public Transform origin { get { return m_origin; } set { m_origin = value; } }

    public bool applyTracking { get { return m_applyTracking; } set { m_applyTracking = value; } }

    protected virtual void OnEnable()
    {
        VivePose.AddNewPosesListener(this);
        VRModule.onModuleActivatedEvent.AddListener(UpdateModel);
        VRModule.onModuleDeactivatedEvent.AddListener(UpdateModel);
    }

    protected virtual void OnDisable()
    {
        VivePose.RemoveNewPosesListener(this);
        VRModule.onModuleActivatedEvent.RemoveListener(UpdateModel);
        VRModule.onModuleDeactivatedEvent.RemoveListener(UpdateModel);
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        UpdateModel();

        if (isActiveAndEnabled && m_applyTracking)
        {
            var deviceIndex = GetCurrentDeviceIndex();
            if (VivePose.IsValid(deviceIndex))
            {
                TrackPose(VivePose.GetPose(deviceIndex), m_origin);
            }
        }
    }

    public virtual void AfterNewPoses() { }

    private uint GetCurrentDeviceIndex()
    {
        uint result;
        switch (m_mode)
        {
            case Mode.ViveRole:
                result = m_viveRole.GetDeviceIndex();
                break;
            case Mode.DeivceIndex:
                result = (uint)m_deviceIndex;
                break;
            case Mode.Disable:
            default:
                return ViveRole.INVALID_DEVICE_INDEX;
        }

        return VivePose.IsValid(result) ? result : ViveRole.INVALID_DEVICE_INDEX;
    }

    private void UpdateModel(SupportedVRModule module) { UpdateModel(); }

    private void UpdateModel()
    {
        if (ChangeProp.Set(ref m_currentActiveModule, VRModule.activeModule))
        {
            if (m_modelObj != null)
            {
                m_currentDeviceIndex = ViveRole.INVALID_DEVICE_INDEX;
                DestroyImmediate(m_modelObj);
                m_modelObj = null;
            }
        }

        switch (m_currentActiveModule)
        {
#if VIU_STEAMVR
            case SupportedVRModule.SteamVR:
                UpdateSteamVRModel();
                break;
#endif
            case SupportedVRModule.Uninitialized:
                if (m_modelObj != null)
                {
                    m_modelObj.SetActive(false);
                }
                break;
            default:
                UpdateDefaultModel();
                break;
        }
    }

#if VIU_STEAMVR
    private SteamVR_RenderModel m_renderModel;

    private void UpdateSteamVRModel()
    {
        if (m_modelObj == null)
        {
            // find SteamVR_RenderModel in child object
            for (int i = 0, imax = transform.childCount; i < imax; ++i)
            {
                if ((m_renderModel = GetComponentInChildren<SteamVR_RenderModel>()) != null)
                {
                    m_modelObj = m_renderModel.gameObject;
                    break;
                }
            }
            // create SteamVR_RenderModel in child object if not found
            if (m_renderModel == null)
            {
                m_modelObj = new GameObject("Model");
                m_modelObj.transform.SetParent(transform, false);
                m_renderModel = m_modelObj.AddComponent<SteamVR_RenderModel>();
            }
        }

        if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
        {
            if (ViveRole.IsValidIndex(m_currentDeviceIndex))
            {
                m_modelObj.SetActive(true);
                m_renderModel.SetDeviceIndex((int)m_currentDeviceIndex);
            }
            else
            {
                m_modelObj.SetActive(false);
            }
        }
    }
#endif

    private VRModuleDeviceModel m_currentDefaultModel;
    private void UpdateDefaultModel()
    {
        if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
        {
            if (ViveRole.IsValidIndex(m_currentDeviceIndex))
            {
                if (ChangeProp.Set(ref m_currentDefaultModel, VRModule.GetCurrentDeviceState(m_currentDeviceIndex).deviceModel) || m_modelObj == null)
                {
                    if (m_modelObj != null)
                    {
                        DestroyImmediate(m_modelObj);
                        m_modelObj = null;
                    }

                    if (m_currentDefaultModel != VRModuleDeviceModel.Unknown)
                    {
                        var prefab = Resources.Load<GameObject>("Models/VIUModel" + m_currentDefaultModel.ToString());
                        if (prefab != null)
                        {
                            m_modelObj = Instantiate(prefab);
                            m_modelObj.transform.SetParent(transform, false);
                            m_modelObj.gameObject.name = "VIUModel" + m_currentDefaultModel.ToString();
                        }
                    }
                }
                else
                {
                    m_modelObj.SetActive(true);
                }
            }
            else
            {
                m_modelObj.SetActive(false);
            }
        }
    }
}