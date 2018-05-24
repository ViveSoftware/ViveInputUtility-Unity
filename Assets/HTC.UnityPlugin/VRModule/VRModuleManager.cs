//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        private static readonly DeviceState s_defaultState;
        private static readonly SimulatorVRModule s_simulator;
        private static readonly Dictionary<string, uint> s_deviceSerialNumberTable = new Dictionary<string, uint>((int)MAX_DEVICE_COUNT);

        [SerializeField]
        private bool m_dontDestroyOnLoad = true;
        [SerializeField]
        private bool m_lockPhysicsUpdateRateToRenderFrequency = true;
        [SerializeField]
        private VRModuleSelectEnum m_selectModule = VRModuleSelectEnum.Auto;
        [SerializeField]
        private VRModuleTrackingSpaceType m_trackingSpaceType = VRModuleTrackingSpaceType.RoomScale;

        [SerializeField]
        private NewPosesEvent m_onNewPoses = new NewPosesEvent();
        [SerializeField]
        private ControllerRoleChangedEvent m_onControllerRoleChanged = new ControllerRoleChangedEvent();
        [SerializeField]
        private InputFocusEvent m_onInputFocus = new InputFocusEvent();
        [SerializeField]
        private DeviceConnectedEvent m_onDeviceConnected = new DeviceConnectedEvent();
        [SerializeField]
        private ActiveModuleChangedEvent m_onActiveModuleChanged = new ActiveModuleChangedEvent();

        private bool m_isUpdating = false;
        private bool m_isDestoryed = false;

        private ModuleBase[] m_modules;
        private VRModuleActiveEnum m_activatedModule = VRModuleActiveEnum.Uninitialized;
        private ModuleBase m_activatedModuleBase;
        private DeviceState[] m_prevStates;
        private DeviceState[] m_currStates;

        static VRModule()
        {
            SetDefaultInitGameObjectGetter(GetDefaultInitGameObject);

            s_defaultState = new DeviceState(INVALID_DEVICE_INDEX);
            s_simulator = new SimulatorVRModule();
        }

        private static GameObject GetDefaultInitGameObject()
        {
            return new GameObject("[ViveInputUtility]");
        }

        public static GameObject GetInstanceGameObject()
        {
            return Instance.gameObject;
        }

        protected override void OnSingletonBehaviourInitialized()
        {
            if (m_dontDestroyOnLoad && transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            m_activatedModule = VRModuleActiveEnum.Uninitialized;
            m_activatedModuleBase = null;

            m_modules = new ModuleBase[EnumUtils.GetMaxValue(typeof(VRModuleActiveEnum)) + 1];
            m_modules[(int)VRModuleActiveEnum.None] = new DefaultModule();
            m_modules[(int)VRModuleActiveEnum.Simulator] = s_simulator;
            m_modules[(int)VRModuleActiveEnum.UnityNativeVR] = new UnityEngineVRModule();
            m_modules[(int)VRModuleActiveEnum.SteamVR] = new SteamVRModule();
            m_modules[(int)VRModuleActiveEnum.OculusVR] = new OculusVRModule();
            m_modules[(int)VRModuleActiveEnum.DayDream] = new GoogleVRModule();
            m_modules[(int)VRModuleActiveEnum.WaveVR] = new WaveVRModule();

            s_deviceSerialNumberTable.Clear();

            m_currStates = new DeviceState[MAX_DEVICE_COUNT];
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i) { m_currStates[i] = new DeviceState(i); }

            m_prevStates = new DeviceState[MAX_DEVICE_COUNT];
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i) { m_prevStates[i] = new DeviceState(i); }
        }

        private void Update()
        {
            if (!IsInstance) { return; }

            m_isUpdating = true;

            // Get should activate module
            var shouldActivateModule = GetShouldActivateModule();

            // Update module activity
            if (m_activatedModule != shouldActivateModule)
            {
                // Do clean up
                if (m_activatedModule != VRModuleActiveEnum.Uninitialized)
                {
                    DeactivateModule();
                }

                if (shouldActivateModule != VRModuleActiveEnum.Uninitialized)
                {
                    ActivateModule(shouldActivateModule);
                }
            }

            if (m_activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                m_activatedModuleBase.Update();
            }

            if (m_isDestoryed)
            {
                DeactivateModule();
            }

            m_isUpdating = false;
        }

        protected override void OnDestroy()
        {
            if (IsInstance)
            {
                m_isDestoryed = true;

                if (!m_isUpdating)
                {
                    DeactivateModule();
                }
            }

            base.OnDestroy();
        }

        private VRModuleActiveEnum GetShouldActivateModule()
        {
            if (m_selectModule == VRModuleSelectEnum.Auto)
            {
                for (int i = m_modules.Length - 1; i >= 0; --i)
                {
                    if (m_modules[i] != null && m_modules[i].ShouldActiveModule())
                    {
                        return (VRModuleActiveEnum)i;
                    }
                }
            }
            else if ((int)m_selectModule >= 0 && (int)m_selectModule < m_modules.Length)
            {
                return (VRModuleActiveEnum)m_selectModule;
            }
            else
            {
                return VRModuleActiveEnum.None;
            }

            return VRModuleActiveEnum.Uninitialized;
        }

        private void ActivateModule(VRModuleActiveEnum module)
        {
            if (m_activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                Debug.LogError("Must deactivate before activate module! Current activatedModule:" + m_activatedModule);
                return;
            }

            if (module == VRModuleActiveEnum.Uninitialized)
            {
                Debug.LogError("Activate module cannot be Uninitialized! Use DeactivateModule instead");
                return;
            }

            m_activatedModule = module;

            m_activatedModuleBase = m_modules[(int)module];
            m_activatedModuleBase.OnActivated();

            VRModule.InvokeActiveModuleChangedEvent(m_activatedModule);

            switch (m_activatedModule)
            {
#if VIU_STEAMVR
                case VRModuleActiveEnum.SteamVR:
#if VIU_STEAMVR_1_2_3_OR_NEWER && !UNITY_2017_1_OR_NEWER && !UNITY_5_3
                    Camera.onPreCull += OnCameraPreCull;
#elif VIU_STEAMVR_1_2_0_OR_NEWER
                    SteamVR_Events.NewPoses.AddListener(OnSteamVRNewPose);
#else
                    SteamVR_Utils.Event.Listen("new_poses", OnSteamVRNewPoseArgs);
#endif
                    break;
#endif
#if VIU_WAVEVR
                case VRModuleActiveEnum.WaveVR:
                    WaveVR_Utils.Event.Listen(WaveVR_Utils.Event.NEW_POSES, OnWaveVRNewPoseArgs);
                    break;
#endif
                default:
#if UNITY_2017_1_OR_NEWER
                    Application.onBeforeRender += UpdateActiveModuleDeviceState;
#else
                    Camera.onPreCull += OnCameraPreCull;
#endif
                    break;
            }
        }

#if VIU_STEAMVR
#if VIU_STEAMVR_1_1_1
        private void OnSteamVRNewPoseArgs(params object[] args) { OnSteamVRNewPose((Valve.VR.TrackedDevicePose_t[])args[0]); }
#endif
        private void OnSteamVRNewPose(Valve.VR.TrackedDevicePose_t[] poses) { UpdateActiveModuleDeviceState(); }
#endif

#if VIU_WAVEVR
        private void OnWaveVRNewPoseArgs(params object[] args) { UpdateActiveModuleDeviceState(); }
#endif

#if !UNITY_2017_1_OR_NEWER
        private int m_poseUpdatedFrame = -1;
        private void OnCameraPreCull(Camera cam)
        {
            var thisFrame = Time.frameCount;
            if (m_poseUpdatedFrame == thisFrame) { return; }

#if UNITY_5_5_OR_NEWER
            if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.VR) { return; }
#else
            if (cam.cameraType != CameraType.Game) { return; }
#endif

            m_poseUpdatedFrame = thisFrame;
            UpdateActiveModuleDeviceState();
        }
#endif

        private void UpdateActiveModuleDeviceState()
        {
            m_isUpdating = true;

            // copy status to from current state to previous state
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i)
            {
                if (m_prevStates[i].isConnected || m_currStates[i].isConnected)
                {
                    m_prevStates[i].CopyFrom(m_currStates[i]);
                }
            }

            // update status
            m_activatedModuleBase.UpdateDeviceState(m_prevStates, m_currStates);

            // send connect/disconnect event
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i)
            {
                if (m_prevStates[i].isConnected != m_currStates[i].isConnected)
                {
                    if (m_currStates[i].isConnected)
                    {
                        try
                        {
                            s_deviceSerialNumberTable.Add(m_currStates[i].serialNumber, i);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError(m_currStates[i].serialNumber + ":" + e.ToString());
                        }
                    }
                    else
                    {
                        s_deviceSerialNumberTable.Remove(m_prevStates[i].serialNumber);
                    }

                    VRModule.InvokeDeviceConnectedEvent(i, m_currStates[i].isConnected);
                }
            }

            // send new poses event
            VRModule.InvokeNewPosesEvent();

            if (m_isDestoryed)
            {
                DeactivateModule();
            }

            m_isUpdating = false;
        }

        private void DeactivateModule()
        {
            if (m_activatedModule == VRModuleActiveEnum.Uninitialized)
            {
                return;
            }

            if (m_activatedModuleBase == null)
            {
                return;
            }

            switch (m_activatedModule)
            {
#if VIU_STEAMVR
                case VRModuleActiveEnum.SteamVR:
#if VIU_STEAMVR_1_2_3_OR_NEWER && !UNITY_2017_1_OR_NEWER && !UNITY_5_3
                    Camera.onPreCull -= OnCameraPreCull;
#elif VIU_STEAMVR_1_2_0_OR_NEWER
                    SteamVR_Events.NewPoses.RemoveListener(OnSteamVRNewPose);
#else
                    SteamVR_Utils.Event.Remove("new_poses", OnSteamVRNewPoseArgs);
#endif
                    break;
#endif
#if VIU_WAVEVR
                case VRModuleActiveEnum.WaveVR:
                    WaveVR_Utils.Event.Remove(WaveVR_Utils.Event.NEW_POSES, OnWaveVRNewPoseArgs);
                    break;
#endif
                default:
#if UNITY_2017_1_OR_NEWER
                    Application.onBeforeRender -= UpdateActiveModuleDeviceState;
#else
                    Camera.onPreCull -= OnCameraPreCull;
#endif
                    break;
            }

            // copy status to from current state to previous state, and reset current state
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i)
            {
                if (m_prevStates[i].isConnected || m_currStates[i].isConnected)
                {
                    m_prevStates[i].CopyFrom(m_currStates[i]);
                    m_currStates[i].Reset();
                }
            }

            // send disconnect event
            for (var i = 0u; i < MAX_DEVICE_COUNT; ++i)
            {
                if (m_prevStates[i].isConnected)
                {
                    VRModule.InvokeDeviceConnectedEvent(i, false);
                }
            }

            var deactivatedModuleBase = m_activatedModuleBase;

            m_activatedModule = VRModuleActiveEnum.Uninitialized;
            m_activatedModuleBase = null;

            deactivatedModuleBase.OnDeactivated();

            VRModule.InvokeActiveModuleChangedEvent(VRModuleActiveEnum.Uninitialized);
        }
    }
}