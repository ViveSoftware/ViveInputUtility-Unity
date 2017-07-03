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

    public enum OverrideModelEnum
    {
        DontOverride = VRModuleDeviceModel.Unknown,
        ViveController = VRModuleDeviceModel.ViveController,
        ViveTracker = VRModuleDeviceModel.ViveTracker,
        ViveBaseStation = VRModuleDeviceModel.ViveBaseStation,
        OculusTouchLeft = VRModuleDeviceModel.OculusTouchLeft,
        OculusTouchRight = VRModuleDeviceModel.OculusTouchRight,
        OculusSensor = VRModuleDeviceModel.OculusSensor,
    }

    [SerializeField]
    private Mode m_mode = Mode.ViveRole;
    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
    [SerializeField]
    private Transform m_origin;
    [SerializeField]
    private Index m_deviceIndex = Index.Hmd;
    [SerializeField]
    private OverrideModelEnum m_overrideModel = OverrideModelEnum.DontOverride;

    private uint m_currentDeviceIndex = ViveRole.INVALID_DEVICE_INDEX;
    private SupportedVRModule m_currentActiveModule;
    private VRModuleDeviceModel m_currentLoadedStaticModel;
    private OverrideModelEnum m_currentOverrideModel;
    private GameObject m_modelObj;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    public Transform origin { get { return m_origin; } set { m_origin = value; } }

    public bool applyTracking { get; set; }

    public OverrideModelEnum overrideModel { get { return m_overrideModel; } set { m_overrideModel = value; } }

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

        if (isActiveAndEnabled && applyTracking)
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
        var overrideModelChanged = ChangeProp.Set(ref m_currentOverrideModel, m_overrideModel);
        var activeModuleChanged = ChangeProp.Set(ref m_currentActiveModule, VRModule.activeModule);

        if (overrideModelChanged || (activeModuleChanged && m_currentOverrideModel == OverrideModelEnum.DontOverride))
        {
            if (m_modelObj != null)
            {
                m_currentDeviceIndex = ViveRole.INVALID_DEVICE_INDEX;
                DestroyImmediate(m_modelObj);
                m_modelObj = null;
            }
        }

        if (m_currentOverrideModel == OverrideModelEnum.DontOverride)
        {
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
        else if (overrideModelChanged)
        {
            ReloadedStaticModel((VRModuleDeviceModel)m_currentOverrideModel);
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

    private void UpdateDefaultModel()
    {
        if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
        {
            if (ViveRole.IsValidIndex(m_currentDeviceIndex))
            {
                if (ChangeProp.Set(ref m_currentLoadedStaticModel, VRModule.GetCurrentDeviceState(m_currentDeviceIndex).deviceModel) || m_modelObj == null)
                {
                    ReloadedStaticModel(m_currentLoadedStaticModel);
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

    private void ReloadedStaticModel(VRModuleDeviceModel model)
    {
        if (m_modelObj != null)
        {
            DestroyImmediate(m_modelObj);
            m_modelObj = null;
        }

        var prefab = Resources.Load<GameObject>("Models/VIUModel" + model.ToString());
        if (prefab != null)
        {
            m_modelObj = Instantiate(prefab);
            m_modelObj.transform.SetParent(transform, false);
            m_modelObj.gameObject.name = "VIUModel" + model.ToString();
        }
    }
}