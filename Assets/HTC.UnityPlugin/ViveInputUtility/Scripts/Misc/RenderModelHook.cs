using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
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

    [SerializeField]
    private Mode m_mode = Mode.ViveRole;
    [SerializeField]
    private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
    [SerializeField]
    private SteamVR_TrackedObject.EIndex m_deviceIndex = SteamVR_TrackedObject.EIndex.Hmd;
    [SerializeField]
    private bool m_applyTracking = true;
    [SerializeField]
    private Transform m_origin;

    private SteamVR_RenderModel m_renderModel;
    private uint m_currentDeviceIndex = ViveRole.INVALID_DEVICE_INDEX;

    public ViveRoleProperty viveRole { get { return m_viveRole; } }

    public Transform origin { get { return m_origin; } set { m_origin = value; } }

    public bool applyTracking { get { return m_applyTracking; } set { m_applyTracking = value; } }

    public SteamVR_RenderModel renderModel { get { return m_renderModel; } }

    public SteamVR_TrackedObject.EIndex deviceIndex
    {
        get
        {
            return m_deviceIndex;
        }
        set
        {
            m_deviceIndex = value;
            UpdateRenderModel();
        }
    }

    protected virtual void Awake()
    {
        // find SteamVR_RenderModel in child object
        for (int i = 0, imax = transform.childCount; i < imax; ++i)
        {
            if ((m_renderModel = GetComponentInChildren<SteamVR_RenderModel>()) != null)
            {
                break;
            }
        }
        // create SteamVR_RenderModel in child object if not found
        if (m_renderModel == null)
        {
            var go = new GameObject("Model");
            go.transform.SetParent(transform, false);
            m_renderModel = go.AddComponent<SteamVR_RenderModel>();
        }
    }

    protected virtual void OnEnable()
    {
        VivePose.AddNewPosesListener(this);
    }

    protected virtual void OnDisable()
    {
        VivePose.RemoveNewPosesListener(this);
    }

    public virtual void BeforeNewPoses() { }

    public virtual void OnNewPoses()
    {
        UpdateRenderModel();

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

    public void UpdateRenderModel()
    {
        if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
        {
            if (!ViveRole.IsValidIndex(m_currentDeviceIndex))
            {
                m_renderModel.gameObject.SetActive(false);
            }
            else
            {
                m_renderModel.gameObject.SetActive(true);
                m_renderModel.SetDeviceIndex((int)m_currentDeviceIndex);
            }
        }
    }

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
}