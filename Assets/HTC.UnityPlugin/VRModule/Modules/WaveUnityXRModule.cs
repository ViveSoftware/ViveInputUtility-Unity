//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_XR_GENERAL_SETTINGS
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;
using System;

#if VIU_WAVEXR_ESSENCE_RENDERMODEL
using Wave.Essence;
#endif

#if VIU_WAVEXR_ESSENCE_HAND
using Object = UnityEngine.Object;
using Wave.Essence.Hand;
using Wave.Native;
using System.Linq;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class WaveUnityXRModule : UnityXRModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.WaveUnityXR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.WaveUnityXR; } }

        public const string WAVE_XR_LOADER_NAME = "Wave XR Loader";
        public const string WAVE_XR_LOADER_CLASS_NAME = "WaveXRLoader";

#if UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS
        private class CameraCreator : VRCameraHook.CameraCreator
        {
            public override bool shouldActive { get { return s_moduleInstance != null && s_moduleInstance.isActivated; } }

            public override void CreateCamera(VRCameraHook hook)
            {
                if (hook.GetComponent<TrackedPoseDriver>() == null)
                {
                    hook.gameObject.AddComponent<TrackedPoseDriver>();
                }
            }
        }

        [RenderModelHook.CreatorPriorityAttirbute(0)]
        private class RenderModelCreator : RenderModelHook.DefaultRenderModelCreator
        {
#if VIU_WAVEXR_ESSENCE_RENDERMODEL || VIU_WAVEXR_ESSENCE_HAND
            private uint m_index = INVALID_DEVICE_INDEX;
#endif
            public override bool shouldActive { get { return s_moduleInstance == null ? false : s_moduleInstance.isActivated; } }

            public override void UpdateRenderModel()
            {
#if VIU_WAVEXR_ESSENCE_RENDERMODEL
                if (HasActiveLoader(WAVE_XR_LOADER_NAME))
                {
                    if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }
                    if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetRightControllerDeviceIndex())
                    {
                        var go = new GameObject("Model");
                        go.transform.SetParent(hook.transform, false);
                        go.AddComponent<Wave.Essence.Controller.RenderModel>();
                        go.AddComponent<Wave.Essence.Controller.ButtonEffect>();
                        go.AddComponent<Wave.Essence.Controller.ShowIndicator>();
                    }
                    else if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetLeftControllerDeviceIndex())
                    {
                        var go = new GameObject("Model");
                        go.transform.SetParent(hook.transform, false);
                        var rm = go.AddComponent<Wave.Essence.Controller.RenderModel>();
                        rm.transform.gameObject.SetActive(false);
                        rm.WhichHand = XR_Hand.NonDominant;
                        rm.transform.gameObject.SetActive(true);
                        var be = go.AddComponent<Wave.Essence.Controller.ButtonEffect>();
                        be.transform.gameObject.SetActive(false);
                        be.HandType = XR_Hand.NonDominant;
                        be.transform.gameObject.SetActive(true);
                        go.AddComponent<Wave.Essence.Controller.ShowIndicator>();
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
                else
//#elif VIU_WAVEXR_ESSENCE_HAND
//                if (HasActiveLoader(WAVE_XR_LOADER_NAME))
//                {
//                    Debug.Log("WaveUnityXRModule UpdateRenderModel");
//                    if (!ChangeProp.Set(ref m_index, hook.GetModelDeviceIndex())) { return; }
//                    Debug.Log("WaveUnityXRModule UpdateRenderModel1 " + m_index + " right: " + VRModule.GetRightControllerDeviceIndex() + " left: " + VRModule.GetLeftControllerDeviceIndex());
//                    if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetRightControllerDeviceIndex())
//                    {
//                        Debug.Log("right hand: " + m_index + " " + VRModule.GetRightControllerDeviceIndex());
//                        var go = new GameObject("Model");
//                        go.transform.SetParent(hook.transform, false);
//                        int count = 21;
//                        List<HandJointPose> list = new List<HandJointPose>();
//                        VivePose.GetAllHandJoints(hook.viveRole, list, true);
//                        for (int i = 0; i < count; i++)
//                        {
//                            var go1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//                            go1.name = "point" + i;
//                            go1.transform.parent = hook.transform;
//                            go1.transform.localScale = Vector3.one * 0.012f;
//                            go1.SetActive(false);

//                            // handle layer
//                            go1.layer = go.gameObject.layer;

//                            //
//                            //
//                            //Debug.Log(i + " " + HandJointPose.NameToIndex(list.ElementAt(i).name) + " " + list.ElementAt(i).pose.pos);
//                            go1.transform.position = list.ElementAt(i).pose.pos;//IBonePose.Instance.GetBoneTransform(i, true).pos;// hand.points[i];
//                            go1.SetActive(IsValidGesturePoint(go.transform.position)/*go.transform.position.IsValidGesturePoint()*/);
//                        }
//                    }
//                    else if (VRModule.IsValidDeviceIndex(m_index) && m_index == VRModule.GetLeftControllerDeviceIndex())
//                    {
//                        Debug.Log("left hand: " + m_index + " " + VRModule.GetLeftControllerDeviceIndex());
//                        var go = new GameObject("Model");
//                        go.transform.SetParent(hook.transform, false);
//                        int count = 21;
//                        List<HandJointPose> list = new List<HandJointPose>();
//                        VivePose.GetAllHandJoints(hook.viveRole, list, true);
//                        for (int i = 0; i < count; i++)
//                        {
//                            var go1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//                            go1.name = "point" + i;
//                            go1.transform.parent = hook.transform;
//                            go1.transform.localScale = Vector3.one * 0.012f;
//                            go1.SetActive(false);

//                            // handle layer
//                            go1.layer = go.gameObject.layer;

//                            //
//                            //
//                            //Debug.Log(i + " " + HandJointPose.NameToIndex(list.ElementAt(i).name) + " " + list.ElementAt(i).pose.pos);
//                            go1.transform.position = list.ElementAt(i).pose.pos;//IBonePose.Instance.GetBoneTransform(i, true).pos;// hand.points[i];
//                            go1.SetActive(IsValidGesturePoint(go.transform.position)/*go.transform.position.IsValidGesturePoint()*/);
//                        }
//                        //List<HandJointPose> list = new List<HandJointPose>();
//                        //VivePose.GetAllHandJoints(hook.viveRole, list, true);
//                        //Debug.Log("list count: " + list.Count);
//                        //foreach (var joint in list)
//                        //{
//                        //    Debug.Log(joint.name + " " + HandJointPose.NameToIndex(joint.name));
//                        //}
//                        //for (int i = 0; i < points.Count; i++)
//                        //{
//                        //    var go = points[i];
//                        //    bool isLeft = ((HandManager.HandType)connectedDevice == HandManager.HandType.LEFT ? true : false);
//                        //    go.transform.position = IBonePose.Instance.GetBoneTransform(i, isLeft).pos;// hand.points[i];
//                        //    go.SetActive(IsValidGesturePoint(go.transform.position)/*go.transform.position.IsValidGesturePoint()*/);
//                        //}
//                    }
//                    else
//                    {
//                        // deacitvate object for render model
//                        if (m_model != null)
//                        {
//                            m_model.gameObject.SetActive(false);
//                        }
//                    }
//                }
//                else
#endif
                {
                    base.UpdateRenderModel();
                }
            }

            private static bool IsValidGesturePoint(Vector3 point)
            {
                return point.x != 0 || point.y != 0 || point.z != 0;
            }
        }

        private class HapticVibrationState
        {
            public uint deviceIndex;
            public float amplitude;
            public float remainingDuration;
            public float remainingDelay;

            public HapticVibrationState(uint index, float amp, float duration, float delay)
            {
                deviceIndex = index;
                amplitude = amp;
                remainingDuration = duration;
                remainingDelay = delay;
            }
        }

        private const uint DEVICE_STATE_LENGTH = 16;
        private static WaveUnityXRModule s_moduleInstance;

        private static uint m_rightHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private static uint m_leftHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private Dictionary<int, uint> m_deviceUidToIndex = new Dictionary<int, uint>();
        private List<InputDevice> m_indexToDevices = new List<InputDevice>();
        private List<InputDevice> m_connectedDevices = new List<InputDevice>();
        private List<HapticVibrationState> m_activeHapticVibrationStates = new List<HapticVibrationState>();
        private List<HandJointPose> m_handJointPose = new List<HandJointPose>();

        public override bool ShouldActiveModule()
        {
            //Debug.Log("WaveUnityXRModule ShouldActiveModule " + (VIUSettings.activateWaveUnityXRModule && HasActiveLoader()));
            
            return VIUSettings.activateWaveUnityXRModule && HasActiveLoader();
        }

        public override void OnActivated()
        {
            base.OnActivated();
#if VIU_WAVEXR_ESSENCE_HAND
            if (Object.FindObjectOfType<HandManager>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<HandManager>();
            }
#endif
            s_moduleInstance = this;

            Debug.Log("Activated XRLoader Name: " + XRGeneralSettings.Instance.Manager.activeLoader.name);
        }

        public override void OnDeactivated()
        {
            s_moduleInstance = null;
            m_deviceUidToIndex.Clear();
            m_indexToDevices.Clear();
            m_connectedDevices.Clear();
        }

        // NOTE: Frequency not supported
        public override void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85.0f, float amplitude = 0.125f, float startSecondsFromNow = 0.0f)
        {
            InputDevice device;
            if (TryGetDevice(deviceIndex, out device))
            {
                if (!device.isValid)
                {
                    return;
                }

                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        for (int i = m_activeHapticVibrationStates.Count - 1; i >= 0; i--)
                        {
                            if (m_activeHapticVibrationStates[i].deviceIndex == deviceIndex)
                            {
                                m_activeHapticVibrationStates.RemoveAt(i);
                            }
                        }

                        m_activeHapticVibrationStates.Add(new HapticVibrationState(deviceIndex, amplitude, durationSeconds, startSecondsFromNow));
                    }
                }
            }
        }

        protected override void UpdateInputDevicesControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            switch (state.deviceModel)
            {
                case VRModuleDeviceModel.ViveFocusChirp:
                    UpdateViveFocusChirpControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveFocusFinch:
                    UpdateViveFocusFinchControllerState(state, device);
                    break;
            }
        }

#if VIU_WAVEXR_ESSENCE_HAND
        private WVR_HandSkeletonData_t handSkeletonData = new WVR_HandSkeletonData_t();
        private WVR_HandPoseData_t handPoseData = new WVR_HandPoseData_t();
        private WVR_PoseOriginModel originModel = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead;
        private Dictionary<HandManager.HandType, uint> indexForInputDevices = new Dictionary<HandManager.HandType, uint>();
        private List<HandManager.HandType> indexedInputDevices = new List<HandManager.HandType>();
#endif

        protected override void UpdateCustomDevices()
        {
            //Debug.Log("WaveUnityXRModule UpdateCustomDevices");
#if VIU_WAVEXR_ESSENCE_HAND
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;
            uint deviceIndex;

            foreach (var connectedDevice in Enum.GetValues(typeof(HandManager.HandType)))
            {
                //Debug.Log("WaveUnityXRModule UpdateCustomDevices1 " + connectedDevice.ToString());
                if (!IBonePose.Instance.IsHandPoseValid((HandManager.HandType)connectedDevice)) continue;

                if (!indexForInputDevices.TryGetValue((HandManager.HandType)connectedDevice, out deviceIndex))
                {
                    deviceIndex = FindOrAllocateUnusedNotHMDIndex();
                    Debug.Log("New Hand connected: " + connectedDevice.ToString() + " index: " + deviceIndex);
                    // assign the index to the new connected device
                    indexForInputDevices.Add((HandManager.HandType)connectedDevice, deviceIndex);
                    while (deviceIndex >= indexedInputDevices.Count) { indexedInputDevices.Add(default(HandManager.HandType)); }
                    indexedInputDevices[(int)deviceIndex] = (HandManager.HandType)connectedDevice;

                    EnsureValidDeviceState(deviceIndex, out prevState, out currState); Debug.Assert(!prevState.isConnected);
                    currState.isConnected = true;

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.serialNumber = connectedDevice.ToString() + deviceIndex;
                    currState.modelNumber = "WVR_" + connectedDevice.ToString();
                    currState.renderModelName = "WVR_" + connectedDevice.ToString();

                    //UpdateNewConnectedInputDevice(currState, connectedDevice);
                    SetupKnownDeviceModel(currState);
                }
                else
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                    currState.isConnected = true;
                }

                int point = 0;
                //m_handJointPose.Clear();
                foreach (var joint in Enum.GetValues(typeof(HandJointName)))
                {
                    if (joint.ToString().Equals("None") || joint.ToString().Equals("Palm") ||  joint.ToString().Equals("IndexIntermediate")
                        || joint.ToString().Equals("MiddleIntermediate") || joint.ToString().Equals("RingIntermediate")
                        || joint.ToString().Equals("PinkyIntermediate"))
                    {
                        continue;
                    }
                    else
                    {
                        //Debug.Log("joint index: " + (int)joint + " joint name: " + joint.ToString());
                        bool isLeft = ((HandManager.HandType)connectedDevice == HandManager.HandType.LEFT ? true : false);
                        //m_handJointPose.Add(new HandJointPose((HandJointName)joint, IBonePose.Instance.GetBoneTransform(point, isLeft).pos, IBonePose.Instance.GetBoneTransform(point, isLeft).rot));
                        //Debug.Log("point: " + point);
                        currState.handJoints[HandJointPose.NameToIndex((HandJointName)joint)] = new HandJointPose((HandJointName)joint, IBonePose.Instance.GetBoneTransform(point, isLeft).pos, IBonePose.Instance.GetBoneTransform(point, isLeft).rot);
                        point++;
                    }
                }
            }

            //Debug.Log("UpdateCustomDevices: " + HandManager.Instance.GetHandTrackingStatus().ToString());
            
            //bool isHandTrackingEnabled = HandManager.Instance.GetHandTrackingStatus() == HandManager.HandTrackingStatus.AVAILABLE;
            //if (!isHandTrackingEnabled) return;

            ////Debug.Log("isHandTrackingEnabled: " + isHandTrackingEnabled);
            //var hasHandTracking = Interop.WVR_GetHandTrackingData(ref handSkeletonData, ref handPoseData, originModel) == WVR_Result.WVR_Success ? true : false;
            //Debug.Log("hasHandTracking: " + hasHandTracking);
            //Debug.Log("handSkeletonData left: " + handSkeletonData.left.confidence);
            //Debug.Log("handSkeletonData right: " + handSkeletonData.right.confidence);
#endif
        }

        protected override void OnCustomDeviceDisconnected(uint index)
        {
#if VIU_WAVEXR_ESSENCE_HAND
            if (indexedInputDevices.Count <= 0) return;

            HandManager.HandType found = HandManager.HandType.LEFT;
            foreach (KeyValuePair<HandManager.HandType, uint> kvp in indexForInputDevices)
            {
                if (index == kvp.Value)
                {
                    found = kvp.Key;
                    break;
                }
            }
            indexForInputDevices.Remove(found);
            indexedInputDevices.Remove(found);
#endif
        }

        protected override void BeforeHandRoleChanged()
        {

        }

        private void UpdateLockPhysicsUpdateRate()
        {
            if (VRModule.lockPhysicsUpdateRateToRenderFrequency && Time.timeScale > 0.0f)
            {
                List<XRDisplaySubsystem> displaySystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySystems);

                float minRefreshRate = float.MaxValue;
                foreach (XRDisplaySubsystem system in displaySystems)
                {
                    float rate = 60.0f;
                    if (system.TryGetDisplayRefreshRate(out rate))
                    {
                        if (rate < minRefreshRate)
                        {
                            minRefreshRate = rate;
                        }
                    }
                }

                if (minRefreshRate > 0 && minRefreshRate < float.MaxValue)
                {
                    Time.fixedDeltaTime = 1.0f / minRefreshRate;
                }
            }
        }

        private void UpdateHapticVibration()
        {
            for (int i = m_activeHapticVibrationStates.Count - 1; i >= 0; i--)
            {
                HapticVibrationState state = m_activeHapticVibrationStates[i];
                if (state.remainingDelay > 0.0f)
                {
                    state.remainingDelay -= Time.deltaTime;
                    continue;
                }

                InputDevice device;
                if (TryGetDevice(state.deviceIndex, out device))
                {
                    if (device.isValid)
                    {
                        device.SendHapticImpulse(0, state.amplitude);
                    }
                }

                state.remainingDuration -= Time.deltaTime;
                if (state.remainingDuration <= 0)
                {
                    m_activeHapticVibrationStates.RemoveAt(i);
                }
            }
        }

        private void UpdateTrackingState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            Vector3 position = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            {
                state.position = position;
            }

            Quaternion rotation = Quaternion.identity;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                state.rotation = rotation;
            }

            Vector3 velocity = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
            {
                state.velocity = velocity;
            }

            Vector3 angularVelocity = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angularVelocity))
            {
                state.angularVelocity = angularVelocity;
            }
        }

        private bool TryGetDevice(uint index, out InputDevice deviceOut)
        {
            deviceOut = default;
            if (index < m_indexToDevices.Count)
            {
                deviceOut = m_indexToDevices[(int)index];
                return true;
            }

            return false;
        }

        private void SetAllXRInputSubsystemTrackingOriginMode(TrackingOriginModeFlags mode)
        {
            List<XRInputSubsystem> systems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(systems);
            foreach (XRInputSubsystem system in systems)
            {
                if (!system.TrySetTrackingOriginMode(mode))
                {
                    Debug.LogWarning("Failed to set TrackingOriginModeFlags to XRInputSubsystem: " + system.SubsystemDescriptor.id);
                }
            }
        }

        private int GetDeviceUID(InputDevice device)
        {
#if CSHARP_7_OR_LATER
            return (device.name, device.serialNumber, device.characteristics).GetHashCode();
#else
            return new { device.name, device.serialNumber, device.characteristics }.GetHashCode();
#endif
        }

        private XRInputSubsystemType DetectCurrentInputSubsystemType()
        {
            List<XRInputSubsystem> systems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(systems);
            if (systems.Count == 0)
            {
                Debug.LogWarning("No XRInputSubsystem detected.");
                return XRInputSubsystemType.Unknown;
            }

            string id = systems[0].SubsystemDescriptor.id;
            Debug.Log("Activated XRInputSubsystem Name: " + id);

            if (Regex.IsMatch(id, @"openvr", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.OpenVR;
            }
            else if (Regex.IsMatch(id, @"oculus", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.Oculus;
            }
            else if (Regex.IsMatch(id, @"windows mixed reality", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.WMR;
            }
            else if (Regex.IsMatch(id, @"magicleap", RegexOptions.IgnoreCase))
            {
                return XRInputSubsystemType.MagicLeap;
            }

            return XRInputSubsystemType.Unknown;
        }

        private void UpdateViveFocusChirpControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // No data
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // No data
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // No data
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad"));

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.DPadUp, dPad.y > 0);
            state.SetButtonPress(VRModuleRawButton.DPadDown, dPad.y < 0);
            state.SetButtonPress(VRModuleRawButton.DPadLeft, dPad.x < 0);
            state.SetButtonPress(VRModuleRawButton.DPadRight, dPad.x > 0);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveFocusFinchControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick); // Touchpad
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch); // Touchpad
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // No data
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // No data
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton); // Trigger
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton); // No Data
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger); // No Data
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis); // Touchpad
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis); // No data
            Vector2 dPad = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<Vector2>("DPad")); // No Data

            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, gripButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.DPadUp, dPad.y > 0);
            state.SetButtonPress(VRModuleRawButton.DPadDown, dPad.y < 0);
            state.SetButtonPress(VRModuleRawButton.DPadLeft, dPad.x < 0);
            state.SetButtonPress(VRModuleRawButton.DPadRight, dPad.x > 0);

            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }
#endif
    }
}