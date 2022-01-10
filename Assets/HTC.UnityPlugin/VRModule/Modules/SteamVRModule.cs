//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using Object = UnityEngine.Object;
#if UNITY_STANDALONE

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Vive.SteamVRExtension;
using UnityEngine;
using System;
using System.Text;

#if VIU_OPENVR_API
using Valve.VR;
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

#if VIU_OPENVR_API && UNITY_STANDALONE
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

            public bool UnmapTracked(uint trackedIndex, out uint moduleIndex)
            {
                if (trackedIndex == OpenVR.k_unTrackedDeviceIndex_Hmd) { moduleIndex = VRModule.INVALID_DEVICE_INDEX; return false; }
                if (!IsValidTracked(trackedIndex)) { throw new ArgumentException("Cannot unmap invalid trackedIndex(" + trackedIndex + ")", "trackedIndex"); }
                if (!IsValidModule(tracked2module[trackedIndex])) { moduleIndex = VRModule.INVALID_DEVICE_INDEX; return false; }
                moduleIndex = tracked2module[trackedIndex];
                if (module2tracked[moduleIndex] != trackedIndex) { throw new Exception("Unexpected mapping t2m[" + trackedIndex + "]=" + tracked2module[trackedIndex] + " m2t[" + moduleIndex + "]=" + module2tracked[moduleIndex]); }
                tracked2module[trackedIndex] = VRModule.INVALID_DEVICE_INDEX;
                module2tracked[moduleIndex] = OpenVR.k_unTrackedDeviceIndexInvalid;
                return true;
            }

            public void Clear()
            {
                for (int i = tracked2module.Length - 1; i >= 0; --i) { tracked2module[i] = VRModule.INVALID_DEVICE_INDEX; }
                for (int i = module2tracked.Length - 1; i >= 0; --i) { module2tracked[i] = OpenVR.k_unTrackedDeviceIndexInvalid; }
            }
        }

        public static VRModuleDeviceClass ToVRModuleDeviceClass(ETrackedDeviceClass deviceClass)
        {
            switch (deviceClass)
            {
                case ETrackedDeviceClass.HMD: return VRModuleDeviceClass.HMD;
                case ETrackedDeviceClass.Controller: return VRModuleDeviceClass.Controller;
                case ETrackedDeviceClass.GenericTracker: return VRModuleDeviceClass.GenericTracker;
                case ETrackedDeviceClass.TrackingReference: return VRModuleDeviceClass.TrackingReference;
                default: return VRModuleDeviceClass.Invalid;
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

        [RenderModelHook.CreatorPriorityAttirbute(0)]
        private class RenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
            private const string SKELETON_PREFAB_BASE_PATH = "Models/VIUModelSteamVRSkeleton";

            private uint m_index = INVALID_DEVICE_INDEX;
            private VIUSteamVRRenderModel m_renderModelComp;
            private GameObject m_skeletonObj;

            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
                m_index = hook.GetModelDeviceIndex();
                if (hook.enabled && VRModule.IsValidDeviceIndex(m_index))
                {
                    if (VRModule.GetDeviceState(m_index).deviceClass == VRModuleDeviceClass.TrackedHand)
                    {
                        // VIUSteamVRRenderModel currently doesn't support tracked hand
                        // Fallback to default model instead
                        UpdateDefaultRenderModel(true);

                        if (m_renderModelComp != null)
                        {
                            m_renderModelComp.gameObject.SetActive(false);
                        }

                        if (m_skeletonObj != null)
                        {
                            m_skeletonObj.SetActive(false);
                        }
                    }
                    else
                    {
                        UpdateDefaultRenderModel(false);

                        bool isLeft;
                        SteamVRSkeletonMode skeletonMode = GetDeviceSkeletonMode(out isLeft);
                        bool shouldShowHand = skeletonMode != SteamVRSkeletonMode.Disabled && IsSkeletonValid();
                        bool shouldShowController = !shouldShowHand || skeletonMode != SteamVRSkeletonMode.WithoutController;

                        if (shouldShowHand && m_skeletonObj == null)
                        {
                            // Create skeleton object
                            GameObject prefab = Resources.Load<GameObject>(GetSkeletonPrefabPath(isLeft));
                            m_skeletonObj = Object.Instantiate(prefab);
                            m_skeletonObj.transform.SetParent(hook.transform);
                            m_skeletonObj.transform.localPosition = Vector3.zero;
                            m_skeletonObj.transform.localRotation = Quaternion.identity;
                        }

                        if (m_skeletonObj != null)
                        {
                            m_skeletonObj.SetActive(shouldShowHand);
                        }

                        if (shouldShowController && m_renderModelComp == null)
                        {
                            // create object for render model
                            var go = new GameObject("Model");
                            go.SetActive(false);
                            go.transform.SetParent(hook.transform, false);
                            m_renderModelComp = go.AddComponent<VIUSteamVRRenderModel>();
                        }

                        if (m_renderModelComp != null)
                        {
                            m_renderModelComp.gameObject.SetActive(shouldShowController);

                            if (shouldShowController)
                            {
                                // set render model index
                                m_renderModelComp.shaderOverride = hook.overrideShader;
                                m_renderModelComp.SetDeviceIndex(m_index);
                            }
                        }
                    }
                }
                else
                {
                    UpdateDefaultRenderModel(false);
                    // deacitvate object for render model
                    if (m_renderModelComp != null)
                    {
                        m_renderModelComp.gameObject.SetActive(false);
                    }

                    if (m_skeletonObj != null)
                    {
                        m_skeletonObj.SetActive(false);
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                base.CleanUpRenderModel();

                if (m_renderModelComp != null)
                {
                    m_renderModelComp.gameObject.SetActive(false);
                }

                if (m_skeletonObj != null)
                {
                    m_skeletonObj.SetActive(false);
                }
            }

            private bool IsLeft()
            {
                if (s_moduleInstance == null)
                {
                    Debug.LogWarning("s_moduleInstance is null.");
                    return false;
                }

                return m_index == s_moduleInstance.GetLeftControllerDeviceIndex();
            }

            private static string GetSkeletonPrefabPath(bool isLeft)
            {
                return SKELETON_PREFAB_BASE_PATH + (isLeft ? "Left" : "Right");
            }

            private SteamVRSkeletonMode GetDeviceSkeletonMode(out bool isLeft)
            {
                if (s_moduleInstance == null)
                {
                    Debug.LogWarning("s_moduleInstance is null.");
                    isLeft = false;
                    return SteamVRSkeletonMode.Disabled;
                }

                if (VRModule.GetDeviceState(m_index).deviceClass == VRModuleDeviceClass.Controller)
                {
                    if (m_index == s_moduleInstance.GetLeftControllerDeviceIndex()) { isLeft = true; return VIUSettings.steamVRLeftSkeletonMode; }
                    if (m_index == s_moduleInstance.GetRightControllerDeviceIndex()) { isLeft = false; return VIUSettings.steamVRRightSkeletonMode; }
                }

                isLeft = false;
                return SteamVRSkeletonMode.Disabled;
            }

            private bool IsSkeletonValid()
            {
                return VRModule.GetDeviceState(m_index).GetValidHandJointCount() > 0;
            }
        }

        private static SteamVRModule s_moduleInstance;
#endif

#if VIU_STEAMVR && !VIU_STEAMVR_2_0_0_OR_NEWER
        private static readonly uint s_sizeOfControllerStats = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));

        private ETrackingUniverseOrigin m_prevTrackingSpace;
        private bool m_hasInputFocus = true;
        private IndexMap m_indexMap = new IndexMap();
        private VRModule.SubmoduleBase.Collection m_submodules = new VRModule.SubmoduleBase.Collection(new ViveHandTrackingSubmodule());
        private bool m_openvrCtrlRoleDirty;
        private uint m_openvrRightIndex;
        private uint m_openvrLeftIndex;
        private uint m_moduleRightIndex;
        private uint m_moduleLeftIndex;

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
            m_submodules.ActivateAllModules();

            s_moduleInstance = this;
        }

        public override void OnDeactivated()
        {
            m_submodules.DeactivateAllModules();

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

            m_submodules.UpdateAllModulesActivity();
            m_submodules.UpdateModulesDeviceInput();

            ProcessDeviceInputChanged();
        }

        private void UpdateConnectedDevice(TrackedDevicePose_t[] poses)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            var vrSystem = OpenVR.System;

            if (vrSystem == null)
            {
                m_openvrCtrlRoleDirty = true;
                for (uint moduleIndex = 0, imax = GetDeviceStateLength(); moduleIndex < imax; ++moduleIndex)
                {
                    if (TryGetValidDeviceState(moduleIndex, out prevState, out currState) && currState.isConnected)
                    {
                        currState.Reset();
                    }
                }
            }
            else
            {
                for (uint trackedIndex = 0u, imax = (uint)poses.Length; trackedIndex < imax; ++trackedIndex)
                {
                    if (!poses[trackedIndex].bDeviceIsConnected)
                    {
                        uint moduleIndex;
                        if (m_indexMap.UnmapTracked(trackedIndex, out moduleIndex))
                        {
                            m_openvrCtrlRoleDirty = true;
                            if (TryGetValidDeviceState(moduleIndex, out prevState, out currState) && prevState.isConnected)
                            {
                                currState.Reset();
                            }
                        }
                    }
                    else
                    {
                        uint moduleIndex;
                        if (!m_indexMap.TryGetModuleIndex(trackedIndex, out moduleIndex))
                        {
                            moduleIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);
                            m_indexMap.Map(trackedIndex, moduleIndex);
                        }
                        else
                        {
                            EnsureValidDeviceState(moduleIndex, out prevState, out currState);
                        }

                        if (!prevState.isConnected)
                        {
                            m_openvrCtrlRoleDirty = true;
                            currState.isConnected = true;
                            currState.deviceClass = ToVRModuleDeviceClass(vrSystem.GetTrackedDeviceClass(trackedIndex));
                            currState.serialNumber = QueryDeviceStringProperty(vrSystem, trackedIndex, ETrackedDeviceProperty.Prop_SerialNumber_String);
                            currState.modelNumber = QueryDeviceStringProperty(vrSystem, trackedIndex, ETrackedDeviceProperty.Prop_ModelNumber_String);
                            currState.renderModelName = QueryDeviceStringProperty(vrSystem, trackedIndex, ETrackedDeviceProperty.Prop_RenderModelName_String);

                            SetupKnownDeviceModel(currState);
                        }
                    }
                }
            }
        }

        private void UpdateDevicePose(TrackedDevicePose_t[] poses)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            for (uint trackedIndex = 0u, imax = (uint)poses.Length; trackedIndex < imax; ++trackedIndex)
            {
                var moduleIndex = m_indexMap.Tracked2ModuleIndex(trackedIndex);
                if (!TryGetValidDeviceState(moduleIndex, out prevState, out currState)) { continue; }
                if (!currState.isConnected) { continue; }

                // update device status
                currState.isPoseValid = poses[trackedIndex].bPoseIsValid;
                currState.isOutOfRange = poses[trackedIndex].eTrackingResult == ETrackingResult.Running_OutOfRange || poses[trackedIndex].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                currState.isCalibrating = poses[trackedIndex].eTrackingResult == ETrackingResult.Calibrating_InProgress || poses[trackedIndex].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                currState.isUninitialized = poses[trackedIndex].eTrackingResult == ETrackingResult.Uninitialized;
                currState.velocity = new Vector3(poses[trackedIndex].vVelocity.v0, poses[trackedIndex].vVelocity.v1, -poses[trackedIndex].vVelocity.v2);
                currState.angularVelocity = new Vector3(-poses[trackedIndex].vAngularVelocity.v0, -poses[trackedIndex].vAngularVelocity.v1, poses[trackedIndex].vAngularVelocity.v2);

                // update poses
                if (poses[trackedIndex].bPoseIsValid)
                {
                    var rigidTransform = new SteamVR_Utils.RigidTransform(poses[trackedIndex].mDeviceToAbsoluteTracking);
                    currState.position = rigidTransform.pos;
                    currState.rotation = rigidTransform.rot;
                }
                else if (prevState.isPoseValid)
                {
                    currState.pose = RigidPose.identity;
                }
            }
        }

        private bool UpdateControllerRole()
        {
            // process hand role
            if (m_openvrCtrlRoleDirty)
            {
                m_openvrCtrlRoleDirty = false;

                var vrSystem = OpenVR.System;
                if (vrSystem == null)
                {
                    m_openvrRightIndex = INVALID_DEVICE_INDEX;
                    m_openvrLeftIndex = INVALID_DEVICE_INDEX;
                }
                else
                {
                    var right = vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
                    var left = vrSystem.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
                    m_indexMap.TryGetModuleIndex(right, out m_openvrRightIndex);
                    m_indexMap.TryGetModuleIndex(left, out m_openvrLeftIndex);
                }
            }

            var moduleRight = m_openvrRightIndex != INVALID_DEVICE_INDEX ? m_openvrRightIndex : m_submodules.GetFirstRightHandedIndex();
            var moduleLeft = m_openvrLeftIndex != INVALID_DEVICE_INDEX ? m_openvrLeftIndex : m_submodules.GetFirstLeftHandedIndex();
            var roleChanged = ChangeProp.Set(ref m_moduleRightIndex, moduleRight);
            roleChanged |= ChangeProp.Set(ref m_moduleLeftIndex, moduleLeft);

            return roleChanged;
        }

        private void UpdateDeviceInput()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            VRControllerState_t ctrlState;
            var system = OpenVR.System;

            for (uint trackedIndex = 0; trackedIndex < OpenVR.k_unMaxTrackedDeviceCount; ++trackedIndex)
            {
                var moduleIndex = m_indexMap.Tracked2ModuleIndex(trackedIndex);
                if (!TryGetValidDeviceState(moduleIndex, out prevState, out currState)) { continue; }
                if (!currState.isConnected) { continue; }

                // get device state from openvr api
                GetConrollerState(system, trackedIndex, out ctrlState);

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
            UpdateDevicePose(poses);

            m_submodules.UpdateAllModulesActivity();
            m_submodules.UpdateModulesDeviceConnectionAndPoses();

            var roleChanged = UpdateControllerRole();

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();

            if (roleChanged)
            {
                InvokeControllerRoleChangedEvent();
            }

            UpdateInputFocusState();
        }

        private void OnInputFocusArgs(params object[] args) { OnInputFocus((bool)args[0]); }
        private void OnInputFocus(bool value)
        {
            m_hasInputFocus = value;
            InvokeInputFocusEvent(value);
        }

        private void OnTrackedDeviceRoleChangedArgs(params object[] args) { OnTrackedDeviceRoleChanged((VREvent_t)args[0]); }
        private void OnTrackedDeviceRoleChanged(VREvent_t arg) { m_openvrCtrlRoleDirty = true; }

        public override bool HasInputFocus()
        {
            return m_hasInputFocus;
        }

        public override uint GetLeftControllerDeviceIndex() { return m_moduleLeftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_moduleRightIndex; }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            var system = OpenVR.System;
            if (system != null)
            {
                var trackedIndex = m_indexMap.Module2TrackedIndex(deviceIndex);
                system.TriggerHapticPulse(trackedIndex, (uint)EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)EVRButtonId.k_EButton_Axis0, (char)durationMicroSec);
            }
        }

        public static uint GetTrackedIndexByModuleIndex(uint deviceIndex)
        {
            if (s_moduleInstance == null) { return OpenVR.k_unTrackedDeviceIndexInvalid; }
            return s_moduleInstance.m_indexMap.Module2TrackedIndex(deviceIndex);
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
