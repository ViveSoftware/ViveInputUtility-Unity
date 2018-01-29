//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    // This script creates and handles SteamVR_RenderModel using viveRole property or device index
    [DisallowMultipleComponent]
    [AddComponentMenu("HTC/VIU/Hooks/Render Model Hook", 10)]
    public class RenderModelHook : MonoBehaviour, IViveRoleComponent
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
            KnucklesLeft = VRModuleDeviceModel.KnucklesLeft,
            KnucklesRight = VRModuleDeviceModel.KnucklesRight,
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
        [SerializeField]
        private Shader m_overrideShader = null;

        private uint m_currentDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private VRModuleDeviceModel m_currentLoadedStaticModel;
        private OverrideModelEnum m_currentOverrideModel;
        private GameObject m_modelObj;
        private bool m_isQuiting;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public Transform origin { get { return m_origin; } set { m_origin = value; } }

        public bool applyTracking { get; set; }

        public OverrideModelEnum overrideModel { get { return m_overrideModel; } set { m_overrideModel = value; } }

        public Shader overrideShader { get { return m_overrideShader; } set { m_overrideShader = value; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled && Application.isPlaying && VRModule.Active)
            {
                UpdateModel();
            }
        }
#endif

        protected virtual void OnEnable()
        {
            VRModule.onActiveModuleChanged += UpdateModel;
            m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;

            UpdateModel();
        }

        protected virtual void OnDisable()
        {
            VRModule.onActiveModuleChanged -= UpdateModel;
            m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;

            if (!m_isQuiting)
            {
                UpdateModel();
            }
        }

        private void OnApplicationQuit()
        {
            m_isQuiting = true;
        }

        private uint GetCurrentDeviceIndex()
        {
            if (!enabled) { return VRModule.INVALID_DEVICE_INDEX; }

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
                    return VRModule.INVALID_DEVICE_INDEX;
            }

            return result;
        }

        private void OnDeviceIndexChanged(uint deviceIndex)
        {
            UpdateModel();
        }

        private void UpdateModel(VRModuleActiveEnum module) { UpdateModel(); }

        private void UpdateModel()
        {
            var overrideModelChanged = ChangeProp.Set(ref m_currentOverrideModel, m_overrideModel);

            if (m_currentOverrideModel == OverrideModelEnum.DontOverride)
            {
                switch (VRModule.activeModule)
                {
#if VIU_STEAMVR
                    case VRModuleActiveEnum.SteamVR:
                        UpdateSteamVRModel();
                        break;
#endif
                    case VRModuleActiveEnum.Uninitialized:
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
            else
            {
                if (overrideModelChanged)
                {
                    ReloadedStaticModel((VRModuleDeviceModel)m_currentOverrideModel);
                }

                if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()) && m_modelObj != null)
                {
                    m_modelObj.SetActive(VRModule.IsValidDeviceIndex(m_currentDeviceIndex));
                }
            }
        }

#if VIU_STEAMVR
        private SteamVR_RenderModel m_renderModel;

        private void UpdateSteamVRModel()
        {
            if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
            {
                if (VRModule.IsValidDeviceIndex(m_currentDeviceIndex))
                {
                    if (m_modelObj != null && m_renderModel == null)
                    {
                        CleanUpModelObj();
                    }

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

                        if (m_overrideShader != null)
                        {
                            m_renderModel.shader = m_overrideShader;
                        }
                    }

                    m_modelObj.SetActive(true);
                    m_renderModel.SetDeviceIndex((int)m_currentDeviceIndex);
                }
                else
                {
                    if (m_modelObj != null)
                    {
                        m_modelObj.SetActive(false);
                    }
                }
            }
        }
#endif

        private void UpdateDefaultModel()
        {
            if (ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex()))
            {
                if (VRModule.IsValidDeviceIndex(m_currentDeviceIndex))
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
            CleanUpModelObj();

            var prefab = Resources.Load<GameObject>("Models/VIUModel" + model.ToString());
            if (prefab != null)
            {
                m_modelObj = Instantiate(prefab);
                m_modelObj.transform.SetParent(transform, false);
                m_modelObj.gameObject.name = "VIUModel" + model.ToString();

                if (m_overrideShader != null)
                {
                    var renderer = m_modelObj.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.shader = m_overrideShader;
                    }
                }
            }
        }

        public void CleanUpModelObj()
        {
            if (m_modelObj != null)
            {
#if VIU_STEAMVR
                m_renderModel = null;
#endif
                Destroy(m_modelObj);
                m_modelObj = null;
            }
        }
    }
}