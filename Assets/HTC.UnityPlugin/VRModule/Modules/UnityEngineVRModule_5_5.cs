//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if UNITY_5_5_OR_NEWER && !UNITY_2017_1_OR_NEWER
using HTC.UnityPlugin.Utility;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.VR;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_5_5_OR_NEWER && !UNITY_2017_1_OR_NEWER
        private static readonly Regex m_viveRgx = new Regex("^.*(htc|vive|openvr).*$", RegexOptions.IgnoreCase);
        private static readonly Regex m_oculusRgx = new Regex("^.*(oculus).*$", RegexOptions.IgnoreCase);
        private static readonly Regex m_leftRgx = new Regex("^.*left.*$", RegexOptions.IgnoreCase);
        private static readonly Regex m_rightRgx = new Regex("^.*right.*$", RegexOptions.IgnoreCase);

        private readonly uint m_headIndex = 0u;
        private readonly uint m_leftIndex = 1u;
        private readonly uint m_rightIndex = 2u;

        private string m_leftJoystickName = string.Empty;
        private string m_rightJoystickName = string.Empty;
        private int m_leftJoystickNameIndex = -1;
        private int m_rightJoystickNameIndex = -1;

#if UNITY_5_6_OR_NEWER
        private TrackingSpaceType m_prevTrackingSpace;

        public override void OnActivated()
        {
            m_prevTrackingSpace = VRDevice.GetTrackingSpaceType();
            UpdateTrackingSpaceType();
        }

        public override void OnDeactivated()
        {
            VRDevice.SetTrackingSpaceType(m_prevTrackingSpace);
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary:
                    VRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);
                    break;
                case VRModuleTrackingSpaceType.RoomScale:
                    VRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
                    break;
            }
        }
#endif

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            var joystickNames = default(string[]);

            // head
            var headCurrState = currState[m_headIndex];
            var headPrevState = prevState[m_headIndex];

            headCurrState.isConnected = VRDevice.isPresent;

            if (headCurrState.isConnected)
            {
                if (!headPrevState.isConnected)
                {
                    headCurrState.deviceClass = VRModuleDeviceClass.HMD;
                    headCurrState.serialNumber = VRDevice.model + " HMD";
                    headCurrState.modelNumber = VRDevice.model + " HMD";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        headCurrState.deviceModel = VRModuleDeviceModel.ViveHMD;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        headCurrState.deviceModel = VRModuleDeviceModel.OculusHMD;
                    }
                    else
                    {
                        headCurrState.deviceModel = VRModuleDeviceModel.Unknown;
                    }

                    headCurrState.renderModelName = VRDevice.model + " " + headCurrState.deviceModel.ToString();
                }

                headCurrState.position = InputTracking.GetLocalPosition(VRNode.Head);
                headCurrState.rotation = InputTracking.GetLocalRotation(VRNode.Head);
                headCurrState.isPoseValid = headCurrState.pose != RigidPose.identity && headCurrState.pose != headPrevState.pose;
            }
            else
            {
                if (headPrevState.isConnected)
                {
                    headCurrState.Reset();
                }
            }

            // right
            var rightCurrState = currState[m_rightIndex];
            var rightPrevState = prevState[m_rightIndex];

            rightCurrState.position = InputTracking.GetLocalPosition(VRNode.RightHand);
            rightCurrState.rotation = InputTracking.GetLocalRotation(VRNode.RightHand);
            rightCurrState.isPoseValid = rightCurrState.pose != RigidPose.identity && rightCurrState.pose != rightPrevState.pose;

            // right connected state
            if (rightCurrState.isPoseValid)
            {
                if (!rightPrevState.isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    for (int i = joystickNames.Length - 1; i >= 0; --i)
                    {
                        if (!string.IsNullOrEmpty(joystickNames[i]) && m_rightRgx.IsMatch(joystickNames[i]))
                        {
                            rightCurrState.isConnected = true;
                            m_rightJoystickName = joystickNames[i];
                            m_rightJoystickNameIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (rightPrevState.isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    if (string.IsNullOrEmpty(joystickNames[m_rightJoystickNameIndex]))
                    {
                        rightCurrState.isConnected = false;
                        m_rightJoystickName = string.Empty;
                        m_rightJoystickNameIndex = -1;
                    }
                }
            }
            // right input state
            if (rightCurrState.isConnected)
            {
                if (!rightPrevState.isConnected)
                {
                    rightCurrState.deviceClass = VRModuleDeviceClass.Controller;
                    rightCurrState.serialNumber = m_rightJoystickName;
                    rightCurrState.modelNumber = VRDevice.model + " Controller";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        rightCurrState.deviceModel = VRModuleDeviceModel.ViveController;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        rightCurrState.deviceModel = VRModuleDeviceModel.OculusTouchRight;
                    }
                    else
                    {
                        rightCurrState.deviceModel = VRModuleDeviceModel.Unknown;
                    }

                    rightCurrState.renderModelName = VRDevice.model + " " + rightCurrState.deviceModel.ToString();
                }

                var rightMenuPress = Input.GetKey(ButtonKeyCode.RMenuPress);
                var rightAButtonPress = Input.GetKey(ButtonKeyCode.RAKeyPress);
                var rightPadPress = Input.GetKey(ButtonKeyCode.RPadPress);

                var rightMenuTouch = Input.GetKey(ButtonKeyCode.RMenuTouch);
                var rightAButtonTouch = Input.GetKey(ButtonKeyCode.RAKeyTouch);
                var rightPadTouch = Input.GetKey(ButtonKeyCode.RPadTouch);
                var rightTriggerTouch = Input.GetKey(ButtonKeyCode.RTriggerTouch);

                var rightTrackpadX = Input.GetAxisRaw(ButtonAxisName.RPadX);
                var rightTrackpadY = Input.GetAxisRaw(ButtonAxisName.RPadY);
                var rightTrigger = Input.GetAxisRaw(ButtonAxisName.RTrigger);
                var rightGrip = Input.GetAxisRaw(ButtonAxisName.RGrip);

                rightCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, rightMenuPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.A, rightAButtonPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.Touchpad, rightPadPress);
                rightCurrState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.Trigger), rightTrigger, 0.55f, 0.45f));
                rightCurrState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.Grip), rightGrip, 0.55f, 0.45f));
                rightCurrState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(rightPrevState.GetButtonPress(VRModuleRawButton.CapSenseGrip), rightGrip, 0.55f, 0.45f));

                rightCurrState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, rightMenuTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.A, rightAButtonTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, rightPadTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.Trigger, rightTriggerTouch);
                rightCurrState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(rightPrevState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), rightGrip, 0.25f, 0.20f));

                rightCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, rightTrackpadX);
                rightCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, rightTrackpadY);
                rightCurrState.SetAxisValue(VRModuleRawAxis.Trigger, rightTrigger);
                rightCurrState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, rightGrip);
            }
            else
            {
                if (rightPrevState.isConnected)
                {
                    rightCurrState.Reset();
                }
            }

            // left
            var leftCurrState = currState[m_leftIndex];
            var leftPrevState = prevState[m_leftIndex];

            leftCurrState.position = InputTracking.GetLocalPosition(VRNode.LeftHand);
            leftCurrState.rotation = InputTracking.GetLocalRotation(VRNode.LeftHand);
            leftCurrState.isPoseValid = leftCurrState.pose != RigidPose.identity && leftCurrState.pose != leftPrevState.pose;
            // left connected state
            if (leftCurrState.isPoseValid)
            {
                if (!leftPrevState.isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    for (int i = joystickNames.Length - 1; i >= 0; --i)
                    {
                        if (!string.IsNullOrEmpty(joystickNames[i]) && m_leftRgx.IsMatch(joystickNames[i]))
                        {
                            leftCurrState.isConnected = true;
                            m_leftJoystickName = joystickNames[i];
                            m_leftJoystickNameIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (leftPrevState.isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    if (string.IsNullOrEmpty(joystickNames[m_leftJoystickNameIndex]))
                    {
                        leftCurrState.isConnected = false;
                        m_leftJoystickName = string.Empty;
                        m_leftJoystickNameIndex = -1;
                    }
                }
            }
            // left input state
            if (leftCurrState.isConnected)
            {
                if (!leftPrevState.isConnected)
                {
                    leftCurrState.deviceClass = VRModuleDeviceClass.Controller;
                    leftCurrState.serialNumber = m_leftJoystickName;
                    leftCurrState.modelNumber = VRDevice.model + " Controller";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        leftCurrState.deviceModel = VRModuleDeviceModel.ViveController;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        leftCurrState.deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                    }
                    else
                    {
                        leftCurrState.deviceModel = VRModuleDeviceModel.Unknown;
                    }

                    leftCurrState.renderModelName = VRDevice.model + " " + leftCurrState.deviceModel.ToString();
                }

                var leftMenuPress = Input.GetKey(ButtonKeyCode.LMenuPress);
                var leftAButtonPress = Input.GetKey(ButtonKeyCode.LAKeyPress);
                var leftPadPress = Input.GetKey(ButtonKeyCode.LPadPress);

                var leftMenuTouch = Input.GetKey(ButtonKeyCode.LMenuTouch);
                var leftAButtonTouch = Input.GetKey(ButtonKeyCode.LAKeyTouch);
                var leftPadTouch = Input.GetKey(ButtonKeyCode.LPadTouch);
                var leftTriggerTouch = Input.GetKey(ButtonKeyCode.LTriggerTouch);

                var leftTrackpadX = Input.GetAxisRaw(ButtonAxisName.LPadX);
                var leftTrackpadY = Input.GetAxisRaw(ButtonAxisName.LPadY);
                var leftTrigger = Input.GetAxisRaw(ButtonAxisName.LTrigger);
                var leftGrip = Input.GetAxisRaw(ButtonAxisName.LGrip);

                leftCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, leftMenuPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.A, leftAButtonPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.Touchpad, leftPadPress);
                leftCurrState.SetButtonPress(VRModuleRawButton.Trigger, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.Trigger), leftTrigger, 0.55f, 0.45f));
                leftCurrState.SetButtonPress(VRModuleRawButton.Grip, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.Grip), leftGrip, 0.55f, 0.45f));
                leftCurrState.SetButtonPress(VRModuleRawButton.CapSenseGrip, AxisToPress(leftPrevState.GetButtonPress(VRModuleRawButton.CapSenseGrip), leftGrip, 0.55f, 0.45f));

                leftCurrState.SetButtonTouch(VRModuleRawButton.ApplicationMenu, leftMenuTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.A, leftAButtonTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, leftPadTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.Trigger, leftTriggerTouch);
                leftCurrState.SetButtonTouch(VRModuleRawButton.CapSenseGrip, AxisToPress(leftPrevState.GetButtonTouch(VRModuleRawButton.CapSenseGrip), leftGrip, 0.25f, 0.20f));

                leftCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, leftTrackpadX);
                leftCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, leftTrackpadY);
                leftCurrState.SetAxisValue(VRModuleRawAxis.Trigger, leftTrigger);
                leftCurrState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, leftGrip);
            }
            else
            {
                if (leftPrevState.isConnected)
                {
                    leftCurrState.Reset();
                }
            }
        }
#endif
    }
}