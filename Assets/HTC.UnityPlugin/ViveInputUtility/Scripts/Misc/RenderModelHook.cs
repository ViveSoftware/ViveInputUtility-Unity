//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
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
            OculusGoController = VRModuleDeviceModel.OculusGoController,
            OculusGearVrController = VRModuleDeviceModel.OculusGearVrController,
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
#if VIU_WAVEVR
                    case VRModuleActiveEnum.WaveVR:
                        UpdateWaveVRModel();
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

#if VIU_WAVEVR
        private bool m_waveVRModelLoaded;
        private wvr.WVR_DeviceType m_currentWaveVRHandType;

        private void UpdateWaveVRModel()
        {
            if (!ChangeProp.Set(ref m_currentDeviceIndex, GetCurrentDeviceIndex())) { return; }

            var hasValidModel = false;
            var handType = default(wvr.WVR_DeviceType);
            if (VRModule.IsValidDeviceIndex(m_currentDeviceIndex))
            {
                if (m_currentDeviceIndex == VRModule.GetRightControllerDeviceIndex())
                {
                    hasValidModel = true;
                    handType = wvr.WVR_DeviceType.WVR_DeviceType_Controller_Right;
                }
                else if (m_currentDeviceIndex == VRModule.GetLeftControllerDeviceIndex())
                {
                    hasValidModel = true;
                    handType = wvr.WVR_DeviceType.WVR_DeviceType_Controller_Left;
                }
            }

            // NOTE: load renderModel only if it hasen't been loaded or user changes handType
            if (hasValidModel)
            {
                if (m_modelObj != null)
                {
                    if (!m_waveVRModelLoaded)
                    {
                        // Clean up model that created by other module
                        CleanUpModelObj();
                    }
                    else if (m_currentWaveVRHandType != handType)
                    {
                        // Clean up model if changed to different hand
                        CleanUpModelObj();
                    }
                }

                m_currentWaveVRHandType = handType;

                if (!m_waveVRModelLoaded)
                {
                    // Create WaveVR_ControllerLoader silently (to avoid Start and OnEnable)
                    var loaderGO = new GameObject("Loader");
                    loaderGO.transform.SetParent(transform, false);
                    loaderGO.SetActive(false);
                    var loader = loaderGO.AddComponent<WaveVR_ControllerLoader>();
                    loader.enabled = false;
                    loader.TrackPosition = false;
                    loader.TrackRotation = false;
                    loader.showIndicator = false;
                    loaderGO.SetActive(true);
                    // Call onLoadController to create model (chould be Finch/Link/Pico/QIYIVR)
                    switch (handType)
                    {
                        case wvr.WVR_DeviceType.WVR_DeviceType_Controller_Right:
                            loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Right;
                            loaderGO.SendMessage("onLoadController", wvr.WVR_DeviceType.WVR_DeviceType_Controller_Right);
                            break;
                        case wvr.WVR_DeviceType.WVR_DeviceType_Controller_Left:
                            loader.WhichHand = WaveVR_ControllerLoader.ControllerHand.Controller_Left;
                            loaderGO.SendMessage("onLoadController", wvr.WVR_DeviceType.WVR_DeviceType_Controller_Left);
                            break;
                    }

                    // Find transform that only contains controller model (include animator, exclude PoseTracker/Beam/UIPointer)
                    // and remove other unnecessary objects
                    var ctrllerActions = FindWaveVRControllerActionsObjInChildren();
                    if (ctrllerActions != null)
                    {
                        ctrllerActions.transform.SetParent(transform, false);
                        ctrllerActions.transform.SetAsFirstSibling();
                        for (int i = transform.childCount - 1; i >= 1; --i)
                        {
                            Destroy(transform.GetChild(i).gameObject);
                        }
                        ctrllerActions.gameObject.SetActive(true);
                        m_modelObj = ctrllerActions.gameObject;
                    }
                    else
                    {
                        Debug.LogWarning("FindWaveVRControllerActionsObjInChildren failed");
                        for (int i = transform.childCount - 1; i >= 0; --i)
                        {
                            Destroy(transform.GetChild(i).gameObject);
                        }
                    }

                    m_waveVRModelLoaded = true;
                }
                else
                {
                    if (m_modelObj != null)
                    {
                        m_modelObj.SetActive(true);
                    }
                }
            }
            else
            {
                if (m_modelObj != null)
                {
                    m_modelObj.SetActive(false);
                }
            }
        }

        // FIXME: This is for finding Controller model with animator, is reliable?
        private Transform FindWaveVRControllerActionsObjInChildren()
        {
            var nodes = new List<Transform>();
            nodes.Add(transform);
            for (int i = 0; i < nodes.Count; ++i)
            {
                var parent = nodes[i];
                for (int j = 0, jmax = parent.childCount; j < jmax; ++j)
                {
                    var child = parent.GetChild(j);
                    nodes.Add(child);
                    if (child.GetComponent<WaveVR_PoseTrackerManager>() != null) { continue; }
                    if (child.GetComponent<WaveVR_Beam>() != null) { continue; }
                    if (child.GetComponent<WaveVR_ControllerPointer>() != null) { continue; }
                    if (child.GetComponent<WaveVR_ControllerLoader>() != null) { continue; }
                    return child;
                }
            }

            return null;
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
#if VIU_WAVEVR
                m_waveVRModelLoaded = false;
#endif
                Destroy(m_modelObj);
                m_modelObj = null;
            }
        }
    }
}