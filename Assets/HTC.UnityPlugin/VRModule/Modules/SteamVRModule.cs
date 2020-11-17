﻿//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
#if UNITY_STANDALONE
#if VIU_STEAMVR
using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Vive.SteamVRExtension;
using System.Text;
using UnityEngine;
using Valve.VR;
using System;
#elif VIU_XR_GENERAL_SETTINGS
using Valve.VR;
using System;
#endif
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_5_4_OR_NEWER
using XRSettings = UnityEngine.VR.VRSettings;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isSteamVRPluginDetected =
#if VIU_STEAMVR
            true;
#else
            false;
#endif
        public static readonly bool isOpenVRSupported =
#if VIU_OPENVR_SUPPORT
            true;
#else
            false;
#endif
    }

    public sealed partial class SteamVRModule : VRModule.ModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.SteamVR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.SteamVR; } }

#if VIU_OPENVR_API
        private class IndexMap
        {
            private uint[] tracked2module = new uint[OpenVR.k_unMaxTrackedDeviceCount];
            private uint[] module2tracked = new uint[VRModule.MAX_DEVICE_COUNT];
            public IndexMap() { Clear(); }

            private bool IsValidTracked(uint trackedIndex) { return trackedIndex < tracked2module.Length; }
            private bool IsValidModule(uint moduleIndex) { return moduleIndex < module2tracked.Length; }

            public uint Tracked2ModuleIndex(uint trackedIndex)
            {
                if (trackedIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) { return VRModule.HMD_DEVICE_INDEX; }
                if (!IsValidTracked(trackedIndex)) { return VRModule.INVALID_DEVICE_INDEX; }
                return tracked2module[trackedIndex];
            }

            public bool TryGetModuleIndex(uint trackedIndex, out uint moduleIndex)
            {
                moduleIndex = Tracked2ModuleIndex(trackedIndex);
                return IsValidModule(moduleIndex);
            }

            public uint Module2TrackedIndex(uint moduleIndex)
            {
                if (moduleIndex == VRModule.HMD_DEVICE_INDEX) { return OpenVR.k_unTrackedDeviceIndex_Hmd; }
                if (!IsValidModule(moduleIndex)) { return OpenVR.k_unTrackedDeviceIndexInvalid; }
                return module2tracked[moduleIndex];
            }

            public void Map(uint trackedIndex, uint moduleIndex)
            {
                if (trackedIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) { throw new ArgumentException("Value cannot be OpenVR.k_unTrackedDeviceIndex_Hmd(" + OpenVR.k_unTrackedDeviceIndex_Hmd + ")", "trackedIndex"); }
                if (moduleIndex == VRModule.HMD_DEVICE_INDEX) { throw new ArgumentException("Value cannot be VRModule.HMD_DEVICE_INDEX(" + VRModule.HMD_DEVICE_INDEX + ")", "moduleIndex"); }
                if (!IsValidTracked(trackedIndex)) { throw new ArgumentException("Invalid value (" + trackedIndex + ")", "trackedIndex"); }
                if (!IsValidModule(moduleIndex)) { throw new ArgumentException("Invalid value (" + moduleIndex + ")", "moduleIndex"); }
                if (IsValidTracked(tracked2module[trackedIndex])) { throw new Exception("tracked2module at [" + trackedIndex + "] is not empty(" + tracked2module[trackedIndex] + ")"); }
                if (IsValidModule(module2tracked[moduleIndex])) { throw new Exception("module2tracked at [" + moduleIndex + "] is not empty(" + module2tracked[moduleIndex] + ")"); }
                tracked2module[trackedIndex] = moduleIndex;
                module2tracked[moduleIndex] = trackedIndex;
            }

            public void UnmapTracked(uint trackedIndex)
            {
                if (trackedIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) { return; }
                if (!IsValidTracked(trackedIndex)) { throw new ArgumentException("Cannot unmap invalid trackedIndex(" + trackedIndex + ")", "trackedIndex"); }
                if (!IsValidModule(tracked2module[trackedIndex])) { return; }
                var moduleIndex = tracked2module[trackedIndex];
                if (module2tracked[moduleIndex] != trackedIndex) { throw new Exception("Unexpected mapping t2m[" + trackedIndex + "]=" + tracked2module[trackedIndex] + " m2t[" + moduleIndex + "]=" + module2tracked[moduleIndex]); }
                tracked2module[trackedIndex] = VRModule.INVALID_DEVICE_INDEX;
                module2tracked[moduleIndex] = OpenVR.k_unTrackedDeviceIndexInvalid;
            }

            public void Clear()
            {
                for (int i = tracked2module.Length - 1; i >= 0; --i) { tracked2module[i] = VRModule.INVALID_DEVICE_INDEX; }
                for (int i = module2tracked.Length - 1; i >= 0; --i) { module2tracked[i] = OpenVR.k_unTrackedDeviceIndexInvalid; }
            }
        }
#endif

#if VIU_STEAMVR && UNITY_STANDALONE
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
                if (hook.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>() == null)
                {
                    hook.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                }
#else
                if (hook.GetComponent<SteamVR_Camera>() == null)
                {
                    hook.gameObject.AddComponent<SteamVR_Camera>();
                }
#endif
            }
        }

        private class RenderModelCreator : RenderModelHook.RenderModelCreator
        {
            private uint m_index = INVALID_DEVICE_INDEX;
            private VIUSteamVRRenderModel m_model;

            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
                if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }

                if (VRModule.IsValidDeviceIndex(m_index))
                {
                    // create object for render model
                    if (m_model == null)
                    {
                        var go = new GameObject("Model");
                        go.transform.SetParent(hook.transform, false);
                        m_model = go.AddComponent<VIUSteamVRRenderModel>();
                    }

                    // set render model index
                    m_model.gameObject.SetActive(true);
                    m_model.shaderOverride = hook.overrideShader;
                    m_model.SetDeviceIndex(m_index);
                }
                else
                {
                    // deacitvate object for render model
                    if (m_model != null)
                    {
                        m_model.gameObject.SetActive(false);
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_model != null)
                {
                    UnityEngine.Object.Destroy(m_model.gameObject);
                    m_model = null;
                    m_index = INVALID_DEVICE_INDEX;
                }
            }
        }

        private static SteamVRModule s_moduleInstance;
#endif

#if VIU_STEAMVR && !VIU_STEAMVR_2_0_0_OR_NEWER
        private static readonly uint s_sizeOfControllerStats = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));

        private ETrackingUniverseOrigin m_prevTrackingSpace;
        private bool m_hasInputFocus = true;
        private IndexMap m_indexMap = new IndexMap();

        public override bool ShouldActiveModule()
        {
#if UNITY_5_4_OR_NEWER
            return VIUSettings.activateSteamVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR";
#else
            return VIUSettings.activateSteamVRModule && SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            // Make sure SteamVR_Render instance exist. It Polls New Poses Event
            if (SteamVR_Render.instance == null) { }

            // setup tracking space
            m_prevTrackingSpace = trackingSpace;
            UpdateTrackingSpaceType();

            EnsureDeviceStateLength(OpenVR.k_unMaxTrackedDeviceCount);

            m_hasInputFocus = inputFocus;

#if VIU_STEAMVR_1_2_1_OR_NEWER
            SteamVR_Events.NewPoses.AddListener(OnSteamVRNewPose);
            SteamVR_Events.InputFocus.AddListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).AddListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_2_0_OR_NEWER
            SteamVR_Events.NewPoses.AddListener(OnSteamVRNewPose);
            SteamVR_Events.InputFocus.AddListener(OnInputFocus);
            SteamVR_Events.System("TrackedDeviceRoleChanged").AddListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_1_1
            SteamVR_Utils.Event.Listen("new_poses", OnSteamVRNewPoseArgs);
            SteamVR_Utils.Event.Listen("input_focus", OnInputFocusArgs);
            SteamVR_Utils.Event.Listen("TrackedDeviceRoleChanged", OnTrackedDeviceRoleChangedArgs);
#endif
            s_moduleInstance = this;
        }

        public override void OnDeactivated()
        {
            trackingSpace = m_prevTrackingSpace;

#if VIU_STEAMVR_1_2_1_OR_NEWER
            SteamVR_Events.NewPoses.RemoveListener(OnSteamVRNewPose);
            SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).RemoveListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_2_0_OR_NEWER
            SteamVR_Events.NewPoses.RemoveListener(OnSteamVRNewPose);
            SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);
            SteamVR_Events.System("TrackedDeviceRoleChanged").RemoveListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_1_1
            SteamVR_Utils.Event.Remove("new_poses", OnSteamVRNewPoseArgs);
            SteamVR_Utils.Event.Remove("input_focus", OnInputFocusArgs);
            SteamVR_Utils.Event.Remove("TrackedDeviceRoleChanged", OnTrackedDeviceRoleChangedArgs);
#endif
            m_indexMap.Clear();
            s_moduleInstance = null;
        }

        public override void Update()
        {
            if (SteamVR.active)
            {
                SteamVR_Render.instance.lockPhysicsUpdateRateToRenderFrequency = VRModule.lockPhysicsUpdateRateToRenderFrequency;
            }

            UpdateDeviceInput();
            ProcessDeviceInputChanged();
        }

        private void UpdateConnectedDevice(TrackedDevicePose_t[] poses)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            var system = OpenVR.System;

            if (system == null)
            {
                for (uint i = 0, imax = GetDeviceStateLength(); i < imax; ++i)
                {
                    if (TryGetValidDeviceState(i, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                    }
                }

                return;
            }

            for (uint i = 0u, imax = (uint)poses.Length; i < imax; ++i)
            {
                if (!poses[i].bDeviceIsConnected)
                {
                    m_indexMap.UnmapTracked(i);

                    if (TryGetValidDeviceState(i, out prevState, out currState) && prevState.isConnected)
                    {
                        currState.Reset();
                    }
                }
                else
                {
                    uint moduleIndex;
                    if (!m_indexMap.TryGetModuleIndex(i, out moduleIndex))
                    {
                        moduleIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                        m_indexMap.Map(i, moduleIndex);
                    }
                    else
                    {
                        EnsureValidDeviceState(moduleIndex, out prevState, out currState);
                    }

                    EnsureValidDeviceState(moduleIndex, out prevState, out currState);

                    if (!prevState.isConnected)
                    {
                        currState.isConnected = true;
                        currState.deviceClass = (VRModuleDeviceClass)system.GetTrackedDeviceClass(i);
                        currState.serialNumber = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                        currState.modelNumber = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        currState.renderModelName = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_RenderModelName_String);

                        SetupKnownDeviceModel(currState);
                    }
                }
            }
        }

        private void UpdateDevicePose(TrackedDevicePose_t[] poses)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            for (uint i = 0u, imax = (uint)poses.Length; i < imax; ++i)
            {
                var moduleIndex = m_indexMap.Tracked2ModuleIndex(i);
                if (!TryGetValidDeviceState(moduleIndex, out prevState, out currState)) { continue; }
                if (!currState.isConnected) { continue; }

                // update device status
                currState.isPoseValid = poses[i].bPoseIsValid;
                currState.isOutOfRange = poses[i].eTrackingResult == ETrackingResult.Running_OutOfRange || poses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                currState.isCalibrating = poses[i].eTrackingResult == ETrackingResult.Calibrating_InProgress || poses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                currState.isUninitialized = poses[i].eTrackingResult == ETrackingResult.Uninitialized;
                currState.velocity = new Vector3(poses[i].vVelocity.v0, poses[i].vVelocity.v1, -poses[i].vVelocity.v2);
                currState.angularVelocity = new Vector3(-poses[i].vAngularVelocity.v0, -poses[i].vAngularVelocity.v1, poses[i].vAngularVelocity.v2);

                // update poses
                if (poses[i].bPoseIsValid)
                {
                    var rigidTransform = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);
                    currState.position = rigidTransform.pos;
                    currState.rotation = rigidTransform.rot;
                }
                else if (prevState.isPoseValid)
                {
                    currState.pose = RigidPose.identity;
                }
            }
        }

        private void UpdateDeviceInput()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            VRControllerState_t ctrlState;
            var system = OpenVR.System;

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; ++i)
            {
                var moduleIndex = m_indexMap.Tracked2ModuleIndex(i);
                if (!TryGetValidDeviceState(moduleIndex, out prevState, out currState)) { continue; }
                if (!currState.isConnected) { continue; }

                // get device state from openvr api
                GetConrollerState(system, i, out ctrlState);

                // update device input button
                currState.buttonPressed = ctrlState.ulButtonPressed;
                currState.buttonTouched = ctrlState.ulButtonTouched;

                // update device input axis
                currState.SetAxisValue(VRModuleRawAxis.Axis0X, ctrlState.rAxis0.x);
                currState.SetAxisValue(VRModuleRawAxis.Axis0Y, ctrlState.rAxis0.y);
                currState.SetAxisValue(VRModuleRawAxis.Axis1X, ctrlState.rAxis1.x);
                currState.SetAxisValue(VRModuleRawAxis.Axis1Y, ctrlState.rAxis1.y);
                currState.SetAxisValue(VRModuleRawAxis.Axis2X, ctrlState.rAxis2.x);
                currState.SetAxisValue(VRModuleRawAxis.Axis2Y, ctrlState.rAxis2.y);
                currState.SetAxisValue(VRModuleRawAxis.Axis3X, ctrlState.rAxis3.x);
                currState.SetAxisValue(VRModuleRawAxis.Axis3Y, ctrlState.rAxis3.y);
                currState.SetAxisValue(VRModuleRawAxis.Axis4X, ctrlState.rAxis4.x);
                currState.SetAxisValue(VRModuleRawAxis.Axis4Y, ctrlState.rAxis4.y);
            }
        }

        private void UpdateInputFocusState()
        {
            if (ChangeProp.Set(ref m_hasInputFocus, inputFocus))
            {
                InvokeInputFocusEvent(m_hasInputFocus);
            }
        }

        private static ETrackingUniverseOrigin trackingSpace
        {
            get
            {
                var compositor = OpenVR.Compositor;
                if (compositor == null) { return default(ETrackingUniverseOrigin); }

                return compositor.GetTrackingSpace();
            }
            set
            {
                var compositor = OpenVR.Compositor;
                if (compositor == null) { return; }

                compositor.SetTrackingSpace(value);
            }
        }

        private static bool inputFocus
        {
            get
            {
                var system = OpenVR.System;
                if (system == null) { return false; }

#if VIU_STEAMVR_1_2_3_OR_NEWER
                return system.IsInputAvailable();
#else
                return !system.IsInputFocusCapturedByAnotherProcess();
#endif
            }
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.RoomScale:
                    trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    break;
                case VRModuleTrackingSpaceType.Stationary:
                    trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
                    break;
            }
        }

        private static void GetConrollerState(CVRSystem system, uint index, out VRControllerState_t ctrlState)
        {
            ctrlState = default(VRControllerState_t);

            if (system != null)
            {
#if VIU_STEAMVR_1_2_0_OR_NEWER
                system.GetControllerState(index, ref ctrlState, s_sizeOfControllerStats);
#else
                system.GetControllerState(index, ref ctrlState);
#endif
            }
        }

        private void OnSteamVRNewPoseArgs(params object[] args) { OnSteamVRNewPose((TrackedDevicePose_t[])args[0]); }
        private void OnSteamVRNewPose(TrackedDevicePose_t[] poses)
        {
            FlushDeviceState();

            UpdateConnectedDevice(poses);
            ProcessConnectedDeviceChanged();

            UpdateDevicePose(poses);
            ProcessDevicePoseChanged();

            UpdateInputFocusState();
        }

        private void OnInputFocusArgs(params object[] args) { OnInputFocus((bool)args[0]); }
        private void OnInputFocus(bool value)
        {
            m_hasInputFocus = value;
            InvokeInputFocusEvent(value);
        }

        private void OnTrackedDeviceRoleChangedArgs(params object[] args) { OnTrackedDeviceRoleChanged((VREvent_t)args[0]); }
        private void OnTrackedDeviceRoleChanged(VREvent_t arg)
        {
            InvokeControllerRoleChangedEvent();
        }

        public override bool HasInputFocus()
        {
            return m_hasInputFocus;
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            var system = OpenVR.System;
            if (system == null) { return VRModule.INVALID_DEVICE_INDEX; }
            var trackedIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            return m_indexMap.Tracked2ModuleIndex(trackedIndex);
        }

        public override uint GetRightControllerDeviceIndex()
        {
            var system = OpenVR.System;
            if (system == null) { return VRModule.INVALID_DEVICE_INDEX; }
            var trackedIndex = system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
            return m_indexMap.Tracked2ModuleIndex(trackedIndex);
        }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            var system = OpenVR.System;
            if (system != null)
            {
                var trackedIndex = m_indexMap.Module2TrackedIndex(deviceIndex);
                system.TriggerHapticPulse(trackedIndex, (uint)EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)EVRButtonId.k_EButton_Axis0, (char)durationMicroSec);
            }
        }

        private StringBuilder m_sb;
        private string QueryDeviceStringProperty(CVRSystem system, uint deviceIndex, ETrackedDeviceProperty prop)
        {
            var error = default(ETrackedPropertyError);
            var capacity = (int)system.GetStringTrackedDeviceProperty(deviceIndex, prop, null, 0, ref error);
            if (capacity <= 1 || capacity > 128) { return string.Empty; }

            if (m_sb == null) { m_sb = new StringBuilder(capacity); }
            else { m_sb.EnsureCapacity(capacity); }

            system.GetStringTrackedDeviceProperty(deviceIndex, prop, m_sb, (uint)m_sb.Capacity, ref error);
            if (error != ETrackedPropertyError.TrackedProp_Success) { return string.Empty; }

            var result = m_sb.ToString();
            m_sb.Length = 0;

            return result;
        }
#endif
    }
}
