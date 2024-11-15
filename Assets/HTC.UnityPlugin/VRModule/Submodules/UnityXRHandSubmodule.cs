using HTC.UnityPlugin.Utility;

using UnityEngine;

#if VIU_UNITY_XR_HAND
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class UnityXRHandSubmodule : VRModule.SubmoduleBase
    {
#if VIU_UNITY_XR_HAND
        private enum Handness
        {
            Left,
            Right,
        }

        private XRHandSubsystem handSys;
        public override bool ShouldActiveModule()
        {
            if (handSys == null)
            {
                handSys = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            }

            if (handSys == null)
            {
                return false;
            }

            return handSys.running;
        }

        protected override void OnActivated()
        {
            Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] OnActivated");
#if ENABLE_INPUT_SYSTEM
            leftActions.Enable();
            rightActions.Enable();
#endif
        }

        protected override void OnDeactivated()
        {
            Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] OnDeactivated");
#if ENABLE_INPUT_SYSTEM
            leftActions.Disable();
            rightActions.Disable();
#endif
            handSys = null;
        }

        public override uint GetLeftHandedIndex() { return leftIndex; }
        public override uint GetRightHandedIndex() { return rightIndex; }

        private uint leftIndex = VRModule.INVALID_DEVICE_INDEX;
        private uint rightIndex = VRModule.INVALID_DEVICE_INDEX;
        protected override void OnUpdateDeviceConnectionAndPoses()
        {
            //Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] OnUpdateDeviceConnectionAndPoses enter");

            handSys.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
            UpdateHandConnectionAndPoses(ref leftIndex, handSys.leftHand);
            UpdateHandConnectionAndPoses(ref rightIndex, handSys.rightHand);
        }

        private void UpdateHandConnectionAndPoses(ref uint deviceIndex, XRHand hand)
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            if (!hand.isTracked)
            {
                if (deviceIndex != VRModule.INVALID_DEVICE_INDEX)
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                    currState.Reset();
                    deviceIndex = VRModule.INVALID_DEVICE_INDEX;
                    Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] " + hand.ToString() + " trackeing lost: [" + deviceIndex + "]");
                }
                // else
                // {
                //     Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] UpdateHandConnectionAndPoses " + hand.handedness.ToString() + " not tracking");
                // }
            }
            else
            {
                if (deviceIndex == VRModule.INVALID_DEVICE_INDEX)
                {
                    deviceIndex = FindAndEnsureUnusedNotHMDDeviceState(out prevState, out currState);

                    var idStr = handSys.subsystemDescriptor.id + " " + hand.ToString();
                    currState.serialNumber = idStr;
                    currState.modelNumber = idStr;
                    currState.renderModelName = idStr;

                    currState.deviceClass = VRModuleDeviceClass.TrackedHand;
                    currState.deviceModel = hand.handedness == Handedness.Left ? VRModuleDeviceModel.UnityXRHandLeft : VRModuleDeviceModel.UnityXRHandRight;
                    currState.input2DType = VRModuleInput2DType.None;

                    Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] " + hand.ToString() + " trackeing recived: [" + deviceIndex + "]");
                }
                else
                {
                    EnsureValidDeviceState(deviceIndex, out prevState, out currState);
                }

                currState.isConnected = true;
                currState.isPoseValid = true;
                currState.position = hand.rootPose.position;
                currState.rotation = hand.rootPose.rotation;

                var joints = currState.handJoints;
                UpdateJoint(joints, HandJointName.ThumbTrapezium, hand.GetJoint(XRHandJointID.Palm));
                UpdateJoint(joints, HandJointName.ThumbMetacarpal, hand.GetJoint(XRHandJointID.ThumbMetacarpal));
                UpdateJoint(joints, HandJointName.ThumbProximal, hand.GetJoint(XRHandJointID.ThumbProximal));
                UpdateJoint(joints, HandJointName.ThumbDistal, hand.GetJoint(XRHandJointID.ThumbDistal));
                UpdateJoint(joints, HandJointName.ThumbTip, hand.GetJoint(XRHandJointID.ThumbTip));
                UpdateJoint(joints, HandJointName.IndexMetacarpal, hand.GetJoint(XRHandJointID.IndexMetacarpal));
                UpdateJoint(joints, HandJointName.IndexProximal, hand.GetJoint(XRHandJointID.IndexProximal));
                UpdateJoint(joints, HandJointName.IndexIntermediate, hand.GetJoint(XRHandJointID.IndexIntermediate));
                UpdateJoint(joints, HandJointName.IndexDistal, hand.GetJoint(XRHandJointID.IndexDistal));
                UpdateJoint(joints, HandJointName.IndexTip, hand.GetJoint(XRHandJointID.IndexTip));
                UpdateJoint(joints, HandJointName.MiddleMetacarpal, hand.GetJoint(XRHandJointID.MiddleMetacarpal));
                UpdateJoint(joints, HandJointName.MiddleProximal, hand.GetJoint(XRHandJointID.MiddleProximal));
                UpdateJoint(joints, HandJointName.MiddleIntermediate, hand.GetJoint(XRHandJointID.MiddleIntermediate));
                UpdateJoint(joints, HandJointName.MiddleDistal, hand.GetJoint(XRHandJointID.MiddleDistal));
                UpdateJoint(joints, HandJointName.MiddleTip, hand.GetJoint(XRHandJointID.MiddleTip));
                UpdateJoint(joints, HandJointName.RingMetacarpal, hand.GetJoint(XRHandJointID.RingMetacarpal));
                UpdateJoint(joints, HandJointName.RingProximal, hand.GetJoint(XRHandJointID.RingProximal));
                UpdateJoint(joints, HandJointName.RingIntermediate, hand.GetJoint(XRHandJointID.RingIntermediate));
                UpdateJoint(joints, HandJointName.RingDistal, hand.GetJoint(XRHandJointID.RingDistal));
                UpdateJoint(joints, HandJointName.RingTip, hand.GetJoint(XRHandJointID.RingTip));
                UpdateJoint(joints, HandJointName.PinkyMetacarpal, hand.GetJoint(XRHandJointID.LittleMetacarpal));
                UpdateJoint(joints, HandJointName.PinkyProximal, hand.GetJoint(XRHandJointID.LittleProximal));
                UpdateJoint(joints, HandJointName.PinkyIntermediate, hand.GetJoint(XRHandJointID.LittleIntermediate));
                UpdateJoint(joints, HandJointName.PinkyDistal, hand.GetJoint(XRHandJointID.LittleDistal));
                UpdateJoint(joints, HandJointName.PinkyTip, hand.GetJoint(XRHandJointID.LittleTip));
                UpdateJoint(joints, HandJointName.Wrist, hand.GetJoint(XRHandJointID.Wrist));
                UpdateJoint(joints, HandJointName.Palm, hand.GetJoint(XRHandJointID.Palm));
            }
        }

        private static void UpdateJoint(JointEnumArray dstJoints, HandJointName dstJointID, XRHandJoint srcJoint)
        {
            if (!srcJoint.TryGetPose(out var pose)) { return; }
            dstJoints[dstJointID] = new JointPose(pose.position, pose.rotation);
        }
#if ENABLE_INPUT_SYSTEM
        private struct HandActions
        {
            public InputAction triggerStrength;
            public InputAction gripStrength;
            public InputAction pinchStrengthIndex;
            public InputAction pinchStrengthMiddle;
            public InputAction pinchStrengthRing;
            public InputAction pinchStrengthLittle;
            public InputAction indexPressed;
            public InputAction middlePressed;
            public InputAction ringPressed;
            public InputAction littlePressed;
            public static HandActions Create(Handedness handedness)
            {
                var handednessTerm = handedness == Handedness.Left ? "{leftHand}" : "{rightHand}";
                return new HandActions()
                {
                    triggerStrength = new InputAction(name: "triggerStrength", binding: "<ViveHandInteraction>" + handednessTerm + "/selectValue"),
                    gripStrength = new InputAction(name: "gripStrength", binding: "<ViveHandInteraction>" + handednessTerm + "/gripValue"),
                    pinchStrengthIndex = new InputAction(name: "pinchStrengthIndex", binding: "<MetaAimHand>" + handednessTerm + "/pinchStrengthIndex"),
                    pinchStrengthMiddle = new InputAction(name: "pinchStrengthMiddle", binding: "<MetaAimHand>" + handednessTerm + "/pinchStrengthMiddle"),
                    pinchStrengthRing = new InputAction(name: "pinchStrengthRing", binding: "<MetaAimHand>" + handednessTerm + "/pinchStrengthRing"),
                    pinchStrengthLittle = new InputAction(name: "pinchStrengthLittle", binding: "<MetaAimHand>" + handednessTerm + "/pinchStrengthLittle"),
                    indexPressed = new InputAction(name: "indexPressed", binding: "<MetaAimHand>" + handednessTerm + "/indexPressed"),
                    middlePressed = new InputAction(name: "middlePressed", binding: "<MetaAimHand>" + handednessTerm + "/middlePressed"),
                    ringPressed = new InputAction(name: "ringPressed", binding: "<MetaAimHand>" + handednessTerm + "/ringPressed"),
                    littlePressed = new InputAction(name: "littlePressed", binding: "<MetaAimHand>" + handednessTerm + "/littlePressed"),
                };
            }
            public void Enable()
            {
                triggerStrength.Enable();
                gripStrength.Enable();
                pinchStrengthIndex.Enable();
                pinchStrengthMiddle.Enable();
                pinchStrengthRing.Enable();
                pinchStrengthLittle.Enable();
                indexPressed.Enable();
                middlePressed.Enable();
                ringPressed.Enable();
                littlePressed.Enable();
            }
            public void Disable()
            {
                triggerStrength.Disable();
                gripStrength.Disable();
                pinchStrengthIndex.Disable();
                pinchStrengthMiddle.Disable();
                pinchStrengthRing.Disable();
                pinchStrengthLittle.Disable();
                indexPressed.Disable();
                middlePressed.Disable();
                ringPressed.Disable();
                littlePressed.Disable();
            }
        }

        private HandActions leftActions = HandActions.Create(Handedness.Left);
        private HandActions rightActions = HandActions.Create(Handedness.Right);
        protected override void OnUpdateDeviceInput()
        {
            //Debug.Log("[UnityXRHandSubmodule][" + (Time.frameCount & 0xFFFF).ToString("X4") + "] OnUpdateDeviceInput enter");
            IVRModuleDeviceStateRW deviceState;
            if (TryGetValidDeviceState(leftIndex, out _, out deviceState))
            {
                UpdateInput(deviceState, leftActions);
            }

            if (TryGetValidDeviceState(rightIndex, out _, out deviceState))
            {
                UpdateInput(deviceState, rightActions);
            }
        }

        private void UpdateInput(IVRModuleDeviceStateRW deviceState, HandActions actions)
        {
            var indexCurl = 0f;
            if (actions.pinchStrengthIndex.activeControl != null) { indexCurl = actions.pinchStrengthIndex.ReadValue<float>(); }
            else if (actions.triggerStrength.activeControl != null) { indexCurl = actions.triggerStrength.ReadValue<float>(); }
            else { indexCurl = GetFingerCurl(deviceState, HandJointName.IndexTip); }

            var indexPressed = false;
            var indexTouch = false;
            if (actions.indexPressed.activeControl != null) { indexPressed = indexTouch = actions.indexPressed.ReadValue<bool>(); }
            else
            {
                indexPressed = VRModule.ModuleBase.AxisToPress(deviceState.GetButtonPress(VRModuleRawButton.Trigger), indexCurl, 0.55f, 0.45f);
                indexTouch = VRModule.ModuleBase.AxisToPress(deviceState.GetButtonTouch(VRModuleRawButton.Trigger), indexCurl, 0.25f, 0.20f);
            }

            var middleCurl = 0f;
            if (actions.pinchStrengthMiddle.activeControl != null) { middleCurl = actions.pinchStrengthMiddle.ReadValue<float>(); }
            else { middleCurl = GetFingerCurl(deviceState, HandJointName.MiddleTip); }

            var ringCurl = 0f;
            if (actions.pinchStrengthRing.activeControl != null) { ringCurl = actions.pinchStrengthRing.ReadValue<float>(); }
            else { ringCurl = GetFingerCurl(deviceState, HandJointName.RingTip); }

            var pinckyCurl = 0f;
            if (actions.pinchStrengthLittle.activeControl != null) { pinckyCurl = actions.pinchStrengthLittle.ReadValue<float>(); }
            else { pinckyCurl = GetFingerCurl(deviceState, HandJointName.PinkyTip); }

            var gripValue = 0f;
            if (actions.gripStrength.activeControl != null) { gripValue = actions.gripStrength.ReadValue<float>(); }
            else { gripValue = (indexCurl + middleCurl + ringCurl + pinckyCurl) * 0.25f; }

            var gripPressed = VRModule.ModuleBase.AxisToPress(deviceState.GetButtonPress(VRModuleRawButton.Grip), gripValue, 0.55f, 0.45f);
            var gripTouch = VRModule.ModuleBase.AxisToPress(deviceState.GetButtonTouch(VRModuleRawButton.Grip), gripValue, 0.25f, 0.20f);

            deviceState.SetAxisValue(VRModuleRawAxis.Trigger, indexCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.IndexCurl, indexCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.IndexPinch, indexCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.MiddleCurl, middleCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.MiddlePinch, middleCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.RingCurl, middleCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.RingPinch, middleCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.PinkyCurl, middleCurl);
            deviceState.SetAxisValue(VRModuleRawAxis.PinkyPinch, middleCurl);
            deviceState.SetButtonPress(VRModuleRawButton.Trigger, indexPressed);
            deviceState.SetButtonPress(VRModuleRawButton.Grip, gripPressed);
            deviceState.SetButtonPress(VRModuleRawButton.CapSenseGrip, gripPressed);
            deviceState.SetButtonTouch(VRModuleRawButton.Trigger, indexTouch);
            deviceState.SetButtonTouch(VRModuleRawButton.Grip, gripTouch);
            deviceState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, gripTouch);
        }

        private float GetFingerCurl(IVRModuleDeviceStateRW state, HandJointName finger)
        {
            var palmDir = state.pose.forward;
            var fingerDir = state.handJoints[finger].pose.forward;
            var angle = Vector3.SignedAngle(palmDir, fingerDir, state.pose.right);
            if (angle < -90f) { angle += 360f; }
            return Mathf.InverseLerp(0f, 180f, angle);
        }
#endif
#endif
    }
}