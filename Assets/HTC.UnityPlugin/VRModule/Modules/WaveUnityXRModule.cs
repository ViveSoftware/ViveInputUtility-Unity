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

        public override void UpdateTrackingSpaceType()
        {
            base.UpdateTrackingSpaceType();
            switch (VRModule.trackingSpaceType)
            {
#if VIU_WAVEXR_ESSENCE_HAND
                case VRModuleTrackingSpaceType.Stationary: originFlag = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead; break;
                case VRModuleTrackingSpaceType.RoomScale: originFlag = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnGround; break;
#endif
                default: return;
            }
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
        private WVR_HandSkeletonData_t handJointData = new WVR_HandSkeletonData_t();
        private WVR_HandPoseData_t handPinchData = new WVR_HandPoseData_t();
        private WVR_PoseOriginModel originFlag = WVR_PoseOriginModel.WVR_PoseOriginModel_OriginOnHead;
#endif

        private uint rightTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint leftTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
//        protected override void UpdateCustomDevices()
//        {
//#if VIU_WAVEXR_ESSENCE_HAND
//            IVRModuleDeviceState prevState;
//            IVRModuleDeviceStateRW currState;

//            if (!(Interop.WVR_GetHandTrackingData(ref handJointData, ref handPinchData, originFlag) == WVR_Result.WVR_Success)) return;

//            if (handJointData.right.confidence > 0.1f)
//            {
//                if (!VRModule.IsValidDeviceIndex(rightTrackedHandIndex))
//                {
//                    rightTrackedHandIndex = FindOrAllocateUnusedNotHMDIndex();
//                    EnsureValidDeviceState(rightTrackedHandIndex, out prevState, out currState);

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.serialNumber = "RIGHT" + rightTrackedHandIndex;
//                    currState.modelNumber = "WVR_RIGHT";
//                    currState.renderModelName = "WVR_RIGHT";

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.deviceModel = VRModuleDeviceModel.WaveTrackedHandRight;
//                    currState.input2DType = VRModuleInput2DType.None;
//                }
//                else
//                {
//                    EnsureValidDeviceState(rightTrackedHandIndex, out prevState, out currState);
//                    currState.isConnected = true;
//                }

//                currState.isPoseValid = true;

//                UpdateTrackedHandJointState(currState, false);
//                //UpdateTrackedHandGestureState(currState, false);
//            }

//            if (handJointData.left.confidence > 0.1f)
//            {
//                if (!VRModule.IsValidDeviceIndex(leftTrackedHandIndex))
//                {
//                    leftTrackedHandIndex = FindOrAllocateUnusedNotHMDIndex();
//                    EnsureValidDeviceState(leftTrackedHandIndex, out prevState, out currState);

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.serialNumber = "LEFT" + leftTrackedHandIndex;
//                    currState.modelNumber = "WVR_LEFT";
//                    currState.renderModelName = "WVR_LEFT";

//                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
//                    currState.deviceModel = VRModuleDeviceModel.WaveTrackedHandLeft;
//                    currState.input2DType = VRModuleInput2DType.None;
//                }
//                else
//                {
//                    EnsureValidDeviceState(leftTrackedHandIndex, out prevState, out currState);
//                    currState.isConnected = true;
//                }

//                currState.isPoseValid = true;

//                UpdateTrackedHandJointState(currState, true);
//                //UpdateTrackedHandGestureState(currState, true);
//            }
//#endif
//        }

        private void UpdateTrackedHandJointState(IVRModuleDeviceStateRW state, bool isLeft)
        {
#if VIU_WAVEXR_ESSENCE_HAND
            var skeleton = isLeft ? handJointData.left : handJointData.right;

            RigidTransform rigidTransform = RigidTransform.identity;
            rigidTransform.update(skeleton.wrist.PoseMatrix);

            WVR_Vector3f_t wrist_pos;
            wrist_pos.v0 = rigidTransform.pos.x;
            wrist_pos.v1 = rigidTransform.pos.y;
            wrist_pos.v2 = rigidTransform.pos.z;

            SetWrist(state, HandJointName.Wrist, rigidTransform);
            SetJoint(state, HandJointName.ThumbMetacarpal, skeleton.thumb.joint1, skeleton.thumb.joint2, rigidTransform.rot);
            SetJoint(state, HandJointName.ThumbProximal, skeleton.thumb.joint2, skeleton.thumb.joint3, rigidTransform.rot);
            SetJoint(state, HandJointName.ThumbDistal, skeleton.thumb.joint3, skeleton.thumb.tip, rigidTransform.rot);
            SetJoint(state, HandJointName.ThumbTip, skeleton.thumb.tip, null, rigidTransform.rot);
            SetJoint(state, HandJointName.IndexProximal, skeleton.index.joint1, skeleton.index.joint2, rigidTransform.rot);
            SetJoint(state, HandJointName.IndexIntermediate, skeleton.index.joint2, skeleton.index.joint3, rigidTransform.rot);
            SetJoint(state, HandJointName.IndexDistal, skeleton.index.joint3, skeleton.index.tip, rigidTransform.rot);
            SetJoint(state, HandJointName.IndexTip, skeleton.index.tip, null, rigidTransform.rot);
            SetJoint(state, HandJointName.MiddleProximal, skeleton.middle.joint1, skeleton.middle.joint2, rigidTransform.rot);
            SetJoint(state, HandJointName.MiddleIntermediate, skeleton.middle.joint2, skeleton.middle.joint3, rigidTransform.rot);
            SetJoint(state, HandJointName.MiddleDistal, skeleton.middle.joint3, skeleton.middle.tip, rigidTransform.rot);
            SetJoint(state, HandJointName.MiddleTip, skeleton.middle.tip, null, rigidTransform.rot);
            SetJoint(state, HandJointName.RingProximal, skeleton.ring.joint1, skeleton.ring.joint2, rigidTransform.rot);
            SetJoint(state, HandJointName.RingIntermediate, skeleton.ring.joint2, skeleton.ring.joint3, rigidTransform.rot);
            SetJoint(state, HandJointName.RingDistal, skeleton.ring.joint3, skeleton.ring.tip, rigidTransform.rot);
            SetJoint(state, HandJointName.RingTip, skeleton.ring.tip, null, rigidTransform.rot);
            SetJoint(state, HandJointName.PinkyProximal, skeleton.pinky.joint1, skeleton.pinky.joint2, rigidTransform.rot);
            SetJoint(state, HandJointName.PinkyIntermediate, skeleton.pinky.joint2, skeleton.pinky.joint3, rigidTransform.rot);
            SetJoint(state, HandJointName.PinkyDistal, skeleton.pinky.joint3, skeleton.pinky.tip, rigidTransform.rot);
            SetJoint(state, HandJointName.PinkyTip, skeleton.pinky.tip, null, rigidTransform.rot);
#endif
        }

#if VIU_WAVEXR_ESSENCE_HAND
        private static void SetWrist(IVRModuleDeviceStateRW state, HandJointName joint, RigidTransform pose)
        {
            var pos = pose.pos;
            var rot = pose.rot * Quaternion.Euler(90, 0, 180);
            state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, pos, rot);
            state.position = pos;
            state.rotation = rot;
        }

        private static void SetJoint(IVRModuleDeviceStateRW state, HandJointName joint, WVR_Vector3f_t currPose, WVR_Vector3f_t? nextPose, Quaternion wrist_rot)
        {
            if (nextPose != null)
            {
                var currPos = new Vector3(currPose.v0, currPose.v1, -currPose.v2);
                var nextPos = new Vector3(nextPose.Value.v0, nextPose.Value.v1, -nextPose.Value.v2);
                var normalized_pos = (nextPos - currPos).normalized * -1;
                var up = Vector3.Cross(normalized_pos, wrist_rot * Quaternion.Euler(90, 0, 180) * Vector3.right);
                if (joint.Equals(HandJointName.ThumbMetacarpal) || joint.Equals(HandJointName.ThumbProximal)
                    || joint.Equals(HandJointName.ThumbDistal))
                {
                    switch (state.deviceModel)
                    {
                        case VRModuleDeviceModel.WaveTrackedHandLeft:
                            {
                                var rot = Quaternion.LookRotation(normalized_pos, up) * Quaternion.Euler(0, 0, 50);
                                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPos, rot);
                            }
                            break;
                        case VRModuleDeviceModel.WaveTrackedHandRight:
                            {
                                var rot = Quaternion.LookRotation(normalized_pos, up) * Quaternion.Euler(0, 0, -50);
                                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPos, rot);
                            }
                            break;
                    }
                }
                else
                {
                    var rot = Quaternion.LookRotation(normalized_pos, up);
                    state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPos, rot);
                }
            }
            else
            {
                var currPos = new Vector3(currPose.v0, currPose.v1, -currPose.v2);
                var rot = Quaternion.identity;
                state.handJoints[HandJointPose.NameToIndex(joint)] = new HandJointPose(joint, currPos, rot);
            }
        }
#endif

//        protected override void OnCustomDeviceDisconnected(uint index)
//        {
//#if VIU_WAVEXR_ESSENCE_HAND
//            if (rightTrackedHandIndex == index)
//            {
//                rightTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
//            }

//            if (leftTrackedHandIndex == index)
//            {
//                leftTrackedHandIndex = VRModule.INVALID_DEVICE_INDEX;
//            }
//#endif
//        }

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

        private void UpdateTrackedHandGestureState(IVRModuleDeviceStateRW state, bool isLeft)
        {
#if VIU_WAVEXR_ESSENCE_HAND
            WVR_HandGestureData_t handGestureData = new WVR_HandGestureData_t();
            if (Interop.WVR_GetHandGestureData(ref handGestureData) == WVR_Result.WVR_Success)
            {
                var gesture = isLeft ? handGestureData.left : handGestureData.right;
                switch (gesture)
                {
                    case WVR_HandGestureType.WVR_HandGestureType_Fist:
                        break;
                    case WVR_HandGestureType.WVR_HandGestureType_Five:
                        break;
                    case WVR_HandGestureType.WVR_HandGestureType_IndexUp:
                        state.SetButtonPress(VRModuleRawButton.Trigger, true);
                        state.SetAxisValue(VRModuleRawAxis.Trigger, 1f);
                        break;
                    case WVR_HandGestureType.WVR_HandGestureType_OK:
                        break;
                    case WVR_HandGestureType.WVR_HandGestureType_ThumbUp:
                        break;
                    case WVR_HandGestureType.WVR_HandGestureType_Invalid:
                    case WVR_HandGestureType.WVR_HandGestureType_Unknown:
                        state.SetButtonPress(VRModuleRawButton.Trigger, false);
                        state.SetAxisValue(VRModuleRawAxis.Trigger, 0f);
                        break;
                }
            }
#endif
        }
#endif
    }
}