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
#if VIU_VIVE_HANDTRACKING
using ViveHandTracking;
#endif
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class OpenVRUnityXRModule : UnityXRModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.OpenVRUnityXR; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.OpenVRUnityXR; } }

        public const string OPENVR_XR_LOADER_NAME = "Open VR Loader";
        public const string OPENVR_XR_LOADER_CLASS_NAME = "OpenVRLoader";

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
        private static OpenVRUnityXRModule s_moduleInstance;

        private static uint m_rightHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private static uint m_leftHandedDeviceIndex = INVALID_DEVICE_INDEX;
        private Dictionary<int, uint> m_deviceUidToIndex = new Dictionary<int, uint>();
        private List<InputDevice> m_indexToDevices = new List<InputDevice>();
        private List<InputDevice> m_connectedDevices = new List<InputDevice>();
        private List<HapticVibrationState> m_activeHapticVibrationStates = new List<HapticVibrationState>();
        private List<HandJointPose> m_handJointPose = new List<HandJointPose>();

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateOpenVRUnityXRModule && HasActiveLoader();
        }

        public override void OnActivated()
        {
            base.OnActivated();

            s_moduleInstance = this;

#if VIU_VIVE_HANDTRACKING
            if (GameObject.FindObjectOfType<GestureProvider>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<GestureProvider>();
            }
#endif

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
                case VRModuleDeviceModel.ViveController:
                    UpdateViveControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                    UpdateViveCosmosControllerState(state, device);
                    break;
                case VRModuleDeviceModel.ViveTracker:
                    UpdateViveTrackerState(state, device);
                    break;
                case VRModuleDeviceModel.KnucklesLeft:
                case VRModuleDeviceModel.KnucklesRight:
                case VRModuleDeviceModel.IndexControllerLeft:
                case VRModuleDeviceModel.IndexControllerRight:
                    UpdateIndexControllerState(state, device);
                    break;
            }
        }

        private uint rightTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint leftTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
//        protected override void UpdateCustomDevices()
//        {
//            IVRModuleDeviceState prevState;
//            IVRModuleDeviceStateRW currState;

//#if VIU_VIVE_HANDTRACKING
//            if (GestureProvider.RightHand != null)
//            {
//                if (!VRModule.IsValidDeviceIndex(rightTrackedHandIndex))
//                {
//                    rightTrackedHandIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.serialNumber = "RIGHT" + rightTrackedHandIndex;
//                    currState.modelNumber = "OPENVR_RIGHT";
//                    currState.renderModelName = "OPENVR_RIGHT";

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.deviceModel = VRModuleDeviceModel.OpenVRTrackedHandRight;
//                    currState.input2DType = VRModuleInput2DType.None;
//                }
//                else
//                {
//                    EnsureValidDeviceState(rightTrackedHandIndex, out prevState, out currState);
//                    currState.isConnected = true;
//                }

//                currState.isPoseValid = true;

//                UpdateTrackedHandJointState(currState, false);
//                UpdateTrackedHandGestureState(currState, false);
//            }

//            if (GestureProvider.LeftHand != null)
//            {
//                if (!VRModule.IsValidDeviceIndex(leftTrackedHandIndex))
//                {
//                    leftTrackedHandIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.serialNumber = "LEFT" + leftTrackedHandIndex;
//                    currState.modelNumber = "OPENVR_LEFT";
//                    currState.renderModelName = "OPENVR_LEFT";

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.deviceModel = VRModuleDeviceModel.OpenVRTrackedHandLeft;
//                    currState.input2DType = VRModuleInput2DType.None;
//                }
//                else
//                {
//                    EnsureValidDeviceState(leftTrackedHandIndex, out prevState, out currState);
//                    currState.isConnected = true;
//                }

//                currState.isPoseValid = true;

//                UpdateTrackedHandJointState(currState, true);
//                UpdateTrackedHandGestureState(currState, true);
//            }
//#endif
//        }

        private void UpdateTrackedHandJointState(IVRModuleDeviceStateRW state, bool isLeft)
        {
#if VIU_VIVE_HANDTRACKING
            var skeleton = isLeft ? GestureProvider.LeftHand : GestureProvider.RightHand;

            SetWrist(state, HandJointName.Wrist, skeleton);
            SetJoint(state, HandJointName.ThumbMetacarpal, skeleton.points[1], skeleton.points[2], skeleton.rotation);
            SetJoint(state, HandJointName.ThumbProximal, skeleton.points[2], skeleton.points[3], skeleton.rotation);
            SetJoint(state, HandJointName.ThumbDistal, skeleton.points[3], skeleton.points[4], skeleton.rotation);
            SetJoint(state, HandJointName.ThumbTip, skeleton.points[4], null, skeleton.rotation);
            SetJoint(state, HandJointName.IndexProximal, skeleton.points[5], skeleton.points[6], skeleton.rotation);
            SetJoint(state, HandJointName.IndexIntermediate, skeleton.points[6], skeleton.points[7], skeleton.rotation);
            SetJoint(state, HandJointName.IndexDistal, skeleton.points[7], skeleton.points[8], skeleton.rotation);
            SetJoint(state, HandJointName.IndexTip, skeleton.points[8], null, skeleton.rotation);
            SetJoint(state, HandJointName.MiddleProximal, skeleton.points[9], skeleton.points[10], skeleton.rotation);
            SetJoint(state, HandJointName.MiddleIntermediate, skeleton.points[10], skeleton.points[11], skeleton.rotation);
            SetJoint(state, HandJointName.MiddleDistal, skeleton.points[11], skeleton.points[12], skeleton.rotation);
            SetJoint(state, HandJointName.MiddleTip, skeleton.points[12], null, skeleton.rotation);
            SetJoint(state, HandJointName.RingProximal, skeleton.points[13], skeleton.points[14], skeleton.rotation);
            SetJoint(state, HandJointName.RingIntermediate, skeleton.points[14], skeleton.points[15], skeleton.rotation);
            SetJoint(state, HandJointName.RingDistal, skeleton.points[15], skeleton.points[16], skeleton.rotation);
            SetJoint(state, HandJointName.RingTip, skeleton.points[16], null, skeleton.rotation);
            SetJoint(state, HandJointName.PinkyProximal, skeleton.points[17], skeleton.points[18], skeleton.rotation);
            SetJoint(state, HandJointName.PinkyIntermediate, skeleton.points[18], skeleton.points[19], skeleton.rotation);
            SetJoint(state, HandJointName.PinkyDistal, skeleton.points[19], skeleton.points[20], skeleton.rotation);
            SetJoint(state, HandJointName.PinkyTip, skeleton.points[20], null, skeleton.rotation);
#endif
        }

#if VIU_VIVE_HANDTRACKING
        private static void SetWrist(IVRModuleDeviceStateRW state, HandJointName joint, GestureResult pose)
        {
            var pos = pose.points[0];
            var rot = pose.rotation * Quaternion.Euler(90, 0, 180);
            state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, pos, rot);
            state.position = pos;
            state.rotation = rot;
        }

        private static void SetJoint(IVRModuleDeviceStateRW state, HandJointName joint, Vector3 currPose, Vector3? nextPose, Quaternion wrist_rot)
        {
            if (nextPose != null)
            {
                //var currPos = new Vector3(currPose.v0, currPose.v1, -currPose.v2);
                //var nextPos = new Vector3(nextPose.Value.v0, nextPose.Value.v1, -nextPose.Value.v2);
                var normalized_pos = (nextPose.Value - currPose).normalized * -1;
                var up = Vector3.Cross(normalized_pos, wrist_rot * Quaternion.Euler(90, 0, 180) * Vector3.right);
                if (joint.Equals(HandJointName.ThumbMetacarpal) || joint.Equals(HandJointName.ThumbProximal)
                    || joint.Equals(HandJointName.ThumbDistal))
                {
                    switch (state.deviceModel)
                    {
                        case VRModuleDeviceModel.OpenVRTrackedHandLeft:
                            {
                                var rot = Quaternion.LookRotation(normalized_pos, up) * Quaternion.Euler(0, 0, 50);
                                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPose, rot);
                            }
                            break;
                        case VRModuleDeviceModel.OpenVRTrackedHandRight:
                            {
                                var rot = Quaternion.LookRotation(normalized_pos, up) * Quaternion.Euler(0, 0, -50);
                                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPose, rot);
                            }
                            break;
                    }
                }
                else
                {
                    var rot = Quaternion.LookRotation(normalized_pos, up);
                    state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPose, rot);
                }
            }
            else
            {
                var rot = Quaternion.identity;
                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPose, rot);
            }
        }
#endif

        //protected override void OnCustomDeviceDisconnected(uint index)
        //{
        //    if (rightTrackedHandIndex == index)
        //    {
        //        rightTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
        //    }

        //    if (leftTrackedHandIndex == index)
        //    {
        //        leftTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
        //    }
        //}

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

        private int GetDeviceUID(InputDevice device)
        {
#if CSHARP_7_OR_LATER
            return (device.name, device.serialNumber, device.characteristics).GetHashCode();
#else
            return new { device.name, device.serialNumber, device.characteristics }.GetHashCode();
#endif
        }

        private void UpdateViveControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool primaryAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primaryAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);

            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveCosmosControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton); // X/A
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryTouch); // X/A

            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // Y/B
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryTouch); // Y/B

            bool primaryAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);

            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));

            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool bumperButton = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("BumperButton"));

            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primaryAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Bumper, bumperButton);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripButton);
            state.SetButtonTouch(VRModuleRawButton.Bumper, bumperButton);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateViveTrackerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            bool menuButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.menuButton);
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton);
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton);
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);

            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, menuButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);

            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
        }

        private void UpdateIndexControllerState(IVRModuleDeviceStateRW state, InputDevice device)
        {
            // TODO: Get finger curl values once OpenVR XR Plugin supports
            bool primaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.primaryButton);
            bool secondaryButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondaryButton); // B
            bool primary2DAxisClick = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisClick);
            bool secondary2DAxisClick = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisClick")); // Joystick
            bool triggerButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.triggerButton); // trigger >= 0.5
            bool gripButton = GetDeviceFeatureValueOrDefault(device, CommonUsages.gripButton); // grip force >= 0.5
            bool primaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("PrimaryTouch"));
            bool secondaryTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("SecondaryTouch"));
            bool triggerTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("TriggerTouch"));
            bool gripTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripTouch"));
            bool gripGrab = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("GripGrab")); // gripCapacitive >= 0.7
            bool primary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxisTouch);
            bool secondary2DAxisTouch = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<bool>("Secondary2DAxisTouch")); // Joystick
            float trigger = GetDeviceFeatureValueOrDefault(device, CommonUsages.trigger);
            float grip = GetDeviceFeatureValueOrDefault(device, CommonUsages.grip); // grip force
            float gripCapacitive = GetDeviceFeatureValueOrDefault(device, new InputFeatureUsage<float>("GripCapacitive")); // touch area on grip
            Vector2 primary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.primary2DAxis);
            Vector2 secondary2DAxis = GetDeviceFeatureValueOrDefault(device, CommonUsages.secondary2DAxis);

            state.SetButtonPress(VRModuleRawButton.A, primaryButton);
            state.SetButtonPress(VRModuleRawButton.ApplicationMenu, secondaryButton);
            state.SetButtonPress(VRModuleRawButton.Touchpad, primary2DAxisClick);
            state.SetButtonPress(VRModuleRawButton.Trigger, triggerButton);
            state.SetButtonPress(VRModuleRawButton.Grip, gripButton);
            state.SetButtonPress(VRModuleRawButton.Axis0, secondary2DAxisClick);

            state.SetButtonTouch(VRModuleRawButton.A, primaryTouch);
            state.SetButtonTouch(VRModuleRawButton.ApplicationMenu, secondaryTouch);
            state.SetButtonTouch(VRModuleRawButton.Trigger, triggerTouch);
            state.SetButtonTouch(VRModuleRawButton.Grip, gripTouch);
            state.SetButtonTouch(VRModuleRawButton.Touchpad, primary2DAxisTouch);
            state.SetButtonTouch(VRModuleRawButton.Axis0, secondary2DAxisTouch);

            state.SetAxisValue(VRModuleRawAxis.TouchpadX, primary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.TouchpadY, primary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.JoystickX, secondary2DAxis.x);
            state.SetAxisValue(VRModuleRawAxis.JoystickY, secondary2DAxis.y);
            state.SetAxisValue(VRModuleRawAxis.Trigger, trigger);
            // conflict with JoystickX
            //state.SetAxisValue(VRModuleRawAxis.CapSenseGrip, grip);
        }

        private void UpdateTrackedHandGestureState(IVRModuleDeviceStateRW state, bool isLeft)
        {
#if VIU_VIVE_HANDTRACKING
            var skeleton = isLeft ? GestureProvider.LeftHand : GestureProvider.RightHand;
            state.SetButtonPress(VRModuleRawButton.Trigger, skeleton.pinch.isPinching);
            state.SetAxisValue(VRModuleRawAxis.Trigger, skeleton.pinch.pinchLevel);
#endif
        }
#endif
    }
}