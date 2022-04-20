//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        private static DeviceState s_defaultState;
        private static SimulatorVRModule s_simulator;
        private static Dictionary<string, uint> s_deviceSerialNumberTable;

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
        private NewInputEvent m_onNewInput = new NewInputEvent();
        [SerializeField]
        private ControllerRoleChangedEvent m_onControllerRoleChanged = new ControllerRoleChangedEvent();
        [SerializeField]
        private InputFocusEvent m_onInputFocus = new InputFocusEvent();
        [SerializeField]
        private DeviceConnectedEvent m_onDeviceConnected = new DeviceConnectedEvent();
        [SerializeField]
        private ActiveModuleChangedEvent m_onActiveModuleChanged = new ActiveModuleChangedEvent();

        private bool m_delayDeactivate = false;
        private bool m_isDestoryed = false;

        private ModuleBase[] m_modules;
        private ModuleBase[] m_modulesOrdered;
        private VRModuleActiveEnum m_activatedModule = VRModuleActiveEnum.Uninitialized;
        private ModuleBase m_activatedModuleBase;
        private DeviceState[] m_prevStates;
        private DeviceState[] m_currStates;

        [RuntimeInitializeOnLoadMethod]
        private static void TryInitializeOnStartup()
        {
            if (VRModuleSettings.initializeOnStartup)
            {
                Initialize();
            }
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

            SetDefaultInitGameObjectGetter(GetDefaultInitGameObject);

            s_defaultState = new DeviceState(INVALID_DEVICE_INDEX);
            s_simulator = new SimulatorVRModule();
            s_deviceSerialNumberTable = new Dictionary<string, uint>(16);

            m_activatedModule = VRModuleActiveEnum.Uninitialized;
            m_activatedModuleBase = null;

            try
            {
                var modules = new List<ModuleBase>();
                var modulesOrdered = new List<ModuleBase>();
                foreach (var type in Assembly.GetAssembly(typeof(ModuleBase)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ModuleBase))))
                {
                    var inst = type == typeof(SimulatorVRModule) ? s_simulator : (ModuleBase)Activator.CreateInstance(type);
                    var index = inst.moduleIndex;

                    if (index < 0)
                    {
                        Debug.LogWarning("Invalid module index, module will not be activated! module name=" + type.Name + " index=" + index);
                    }
                    else if (index < modules.Count && modules[index] != null)
                    {
                        Debug.LogWarning("Duplicated module index, module will not be activated! module name=" + type.Name + " index=" + index);
                    }
                    else
                    {
                        while (index >= modules.Count) { modules.Add(null); }
                        modules[index] = inst;
                    }

                    var order = inst.moduleOrder;

                    if (order < 0)
                    {
                        Debug.LogWarning("Invalid module order, module will not be activated! module name=" + type.Name + " order=" + order);
                    }
                    else if (order < modulesOrdered.Count && modulesOrdered[order] != null)
                    {
                        Debug.LogWarning("Duplicated module order, module will not be activated! module name=" + type.Name + " order=" + order);
                    }
                    else
                    {
                        while (order >= modulesOrdered.Count) { modulesOrdered.Add(null); }
                        modulesOrdered[order] = inst;
                    }
                }
                m_modules = modules.ToArray();
                m_modulesOrdered = modulesOrdered.ToArray();
            }
            catch (Exception e)
            {
                m_modules = new ModuleBase[] { new DefaultModule() };
                m_modulesOrdered = new ModuleBase[] { new DefaultModule() };
                Debug.LogError(e);
            }
        }

        private uint GetDeviceStateLength() { return m_currStates == null ? 0u : (uint)m_currStates.Length; }

        private void EnsureDeviceStateLength(uint capacity)
        {
            // NOTE: this will clear out the array
            var cap = (int)(capacity < MAX_DEVICE_COUNT ? capacity : MAX_DEVICE_COUNT);
            if (GetDeviceStateLength() < cap)
            {
                if (m_currStates == null) { m_currStates = new DeviceState[cap]; }
                else { Array.Resize(ref m_currStates, cap); }
                if (m_prevStates == null) { m_prevStates = new DeviceState[cap]; }
                else { Array.Resize(ref m_prevStates, cap); }
            }
        }

        private bool TryGetValidDeviceState(uint index, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState)
        {
            DeviceState prevRawState;
            DeviceState currRawState;
            if (TryGetValidDeviceState(index, out prevRawState, out currRawState))
            {
                prevState = prevRawState;
                currState = currRawState;
                return true;
            }
            else
            {
                prevState = null;
                currState = null;
                return false;
            }
        }

        private bool TryGetValidDeviceState(uint index, out DeviceState prevState, out DeviceState currState)
        {
            if (m_currStates == null || index >= m_currStates.Length || m_currStates[index] == null)
            {
                prevState = null;
                currState = null;
                return false;
            }
            else
            {
                prevState = m_prevStates[index];
                currState = m_currStates[index];
                return true;
            }
        }

        private void EnsureValidDeviceState(uint index, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState)
        {
            EnsureDeviceStateLength(index + 1);
            if (!TryGetValidDeviceState(index, out prevState, out currState))
            {
                prevState = m_prevStates[index] = new DeviceState(index);
                currState = m_currStates[index] = new DeviceState(index);
            }
        }
        // this function will skip VRModule.HMD_DEVICE_INDEX (preserved index for HMD)
        private uint FindAndEnsureUnusedNotHMDDeviceState(out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState)
        {
            var index = (m_activatedModuleBase == null ? VRModule.HMD_DEVICE_INDEX : m_activatedModuleBase.reservedDeviceIndex) + 1u;
            var len = GetDeviceStateLength();
            for (; index < len; ++index)
            {
                if (TryGetValidDeviceState(index, out prevState, out currState))
                {
                    if (prevState.isConnected) { continue; }
                    if (currState.isConnected) { continue; }
                    return index;
                }
                break;
            }

            EnsureValidDeviceState(index, out prevState, out currState);
            return index;
        }

        private void Update()
        {
            if (!IsInstance) { return; }

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

            if (m_activatedModuleBase != null)
            {
                m_activatedModuleBase.Update();
            }
        }

        private void FixedUpdate()
        {
            if (!IsInstance) { return; }

            if (m_activatedModuleBase != null)
            {
                m_activatedModuleBase.FixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (!IsInstance) { return; }

            if (m_activatedModuleBase != null)
            {
                m_activatedModuleBase.LateUpdate();
            }
        }

        protected override void OnDestroy()
        {
            if (IsInstance)
            {
                m_isDestoryed = true;

                if (!m_delayDeactivate)
                {
                    DeactivateModule();
                }
            }

            base.OnDestroy();
        }

        private VRModuleActiveEnum GetShouldActivateModule()
        {
            if (IsApplicationQuitting || m_isDestoryed) { return VRModuleActiveEnum.Uninitialized; }

            if (m_selectModule == VRModuleSelectEnum.Auto)
            {
                for (int i = m_modulesOrdered.Length - 1; i >= 0; --i)
                {
                    if (m_modulesOrdered[i] != null && m_modulesOrdered[i].ShouldActiveModule())
                    {
                        return (VRModuleActiveEnum)m_modulesOrdered[i].moduleIndex;
                    }
                }
            }
            else if (m_selectModule >= 0 && (int)m_selectModule < m_modules.Length)
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
            m_activatedModuleBase.Activated();

#if UNITY_2017_1_OR_NEWER
            Application.onBeforeRender += BeforeRenderUpdateModule;
#else
            Camera.onPreCull += OnCameraPreCull;
#endif

            InvokeActiveModuleChangedEvent(m_activatedModule);
        }

#if !UNITY_2017_1_OR_NEWER
        private int m_preCullOnceFrame = -1;
        private void OnCameraPreCull(Camera cam)
        {
            var thisFrame = Time.frameCount;
            if (m_preCullOnceFrame == thisFrame) { return; }
#if UNITY_5_5_OR_NEWER
            if ((cam.cameraType & (CameraType.Game | CameraType.VR)) == 0) { return; }
#else
            if ((cam.cameraType & CameraType.Game) == 0) { return; }
#endif
            m_preCullOnceFrame = thisFrame;
            BeforeRenderUpdateModule();
        }
#endif

        private void BeforeRenderUpdateModule()
        {
            if (m_activatedModuleBase != null)
            {
                m_activatedModuleBase.BeforeRenderUpdate();
            }
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

            m_delayDeactivate = false;

#if UNITY_2017_1_OR_NEWER
            Application.onBeforeRender -= BeforeRenderUpdateModule;
#else
            Camera.onPreCull -= OnCameraPreCull;
#endif

            DeviceState prevState;
            DeviceState currState;
            // copy status to from current state to previous state, and reset current state
            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                if (!TryGetValidDeviceState(i, out prevState, out currState)) { continue; }

                if (prevState.isConnected || currState.isConnected)
                {
                    prevState.CopyFrom(currState);
                    currState.Reset();
                }
            }

            s_deviceSerialNumberTable.Clear();

            // send disconnect event
            SendAllDeviceConnectedEvent();

            var deactivatedModuleBase = m_activatedModuleBase;
            m_activatedModule = VRModuleActiveEnum.Uninitialized;
            m_activatedModuleBase = null;
            deactivatedModuleBase.Deactivated();

            InvokeActiveModuleChangedEvent(VRModuleActiveEnum.Uninitialized);
        }

        private void ModuleFlushDeviceState()
        {
            DeviceState prevState;
            DeviceState currState;

            // copy status to from current state to previous state
            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                if (!TryGetValidDeviceState(i, out prevState, out currState)) { continue; }

                if (prevState.isConnected || currState.isConnected)
                {
                    prevState.CopyFrom(currState);
                }
            }
        }

        private void ModuleConnectedDeviceChanged()
        {
            DeviceState prevState;
            DeviceState currState;

            m_delayDeactivate = true;
            List<uint> connected = null;
            List<uint> disconnected = null;
            // send connect/disconnect event
            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                if (!TryGetValidDeviceState(i, out prevState, out currState)) { continue; }

                if (!prevState.isConnected)
                {
                    if (currState.isConnected)
                    {
                        if (connected == null) { connected = ListPool<uint>.Get(); }
                        connected.Add(i);
                    }
                }
                else
                {
                    if (!currState.isConnected)
                    {
                        if (disconnected == null) { disconnected = ListPool<uint>.Get(); }
                        disconnected.Add(i);
                    }
                }
            }

            if (disconnected != null)
            {
                for (int i = 0, imax = disconnected.Count; i < imax; ++i)
                {
                    var index = disconnected[i];
                    var state = m_prevStates[index];
                    s_deviceSerialNumberTable.Remove(state.serialNumber);
                }
                ListPool<uint>.Release(disconnected);
            }

            if (connected != null)
            {
                for (int i = 0, imax = connected.Count; i < imax; ++i)
                {
                    var index = connected[i];
                    var state = m_currStates[index];
                    if (string.IsNullOrEmpty(state.serialNumber))
                    {
                        Debug.LogError("Device connected with empty serialNumber. index:" + state.deviceIndex);
                    }
                    else if (s_deviceSerialNumberTable.ContainsKey(state.serialNumber))
                    {
                        Debug.LogError("Device connected with duplicate serialNumber: " + state.serialNumber + " index:" + state.deviceIndex + "(" + s_deviceSerialNumberTable[state.serialNumber] + ")");
                    }
                    else
                    {
                        s_deviceSerialNumberTable.Add(state.serialNumber, index);
                    }
                }
                ListPool<uint>.Release(connected);
            }

            SendAllDeviceConnectedEvent();

            m_delayDeactivate = false;
            if (m_isDestoryed)
            {
                DeactivateModule();
            }
        }

        private void SendAllDeviceConnectedEvent()
        {
            DeviceState prevState;
            DeviceState currState;

            for (uint i = 0u, imax = GetDeviceStateLength(); i < imax; ++i)
            {
                if (!TryGetValidDeviceState(i, out prevState, out currState)) { continue; }

                if (prevState.isConnected != currState.isConnected)
                {
                    InvokeDeviceConnectedEvent(i, currState.isConnected);
                }
            }
        }
    }
}