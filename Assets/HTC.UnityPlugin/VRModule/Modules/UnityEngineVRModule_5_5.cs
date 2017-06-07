//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.VR;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_5_5_OR_NEWER// && !UNITY_2017_OR_NEWER
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

        public override bool ShouldActiveModule() { return VRSettings.enabled; }

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

#if UNITY_5_6_OR_NEWER
        private TrackingSpaceType prevTrackingSpace;

        public override void OnActivated()
        {
            prevTrackingSpace = VRDevice.GetTrackingSpaceType();
            UpdateTrackingSpaceType();
        }

        public override void OnDeactivated()
        {
            VRDevice.SetTrackingSpaceType(prevTrackingSpace);
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

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            var joystickNames = default(string[]);

            // head
            currState[m_headIndex].isConnected = VRDevice.isPresent;

            if (currState[m_headIndex].isConnected)
            {
                if (!prevState[m_headIndex].isConnected)
                {
                    currState[m_headIndex].deviceClass = VRModuleDeviceClass.HMD;
                    currState[m_headIndex].deviceSerialID = VRDevice.model + " HMD";
                    currState[m_headIndex].deviceModelNumber = VRDevice.model + " HMD";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_headIndex].deviceModel = VRModuleDeviceModel.ViveHMD;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_headIndex].deviceModel = VRModuleDeviceModel.OculusHMD;
                    }
                    else
                    {
                        currState[m_headIndex].deviceModel = VRModuleDeviceModel.Unknown;
                    }
                }

                currState[m_headIndex].position = InputTracking.GetLocalPosition(VRNode.Head);
                currState[m_headIndex].rotation = InputTracking.GetLocalRotation(VRNode.Head);
                currState[m_headIndex].isPoseValid = currState[m_headIndex].pose != Pose.identity && currState[m_headIndex].pose != prevState[m_headIndex].pose;
            }
            else
            {
                if (prevState[m_headIndex].isConnected)
                {
                    currState[m_headIndex].Reset();
                }
            }

            // right
            currState[m_rightIndex].position = InputTracking.GetLocalPosition(VRNode.RightHand);
            currState[m_rightIndex].rotation = InputTracking.GetLocalRotation(VRNode.RightHand);
            currState[m_rightIndex].isPoseValid = currState[m_rightIndex].pose != Pose.identity && currState[m_rightIndex].pose != prevState[m_rightIndex].pose;

            // right connected state
            if (currState[m_rightIndex].isPoseValid)
            {
                if (!prevState[m_rightIndex].isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    for (int i = joystickNames.Length - 1; i >= 0; --i)
                    {
                        if (!string.IsNullOrEmpty(joystickNames[i]) && m_rightRgx.IsMatch(joystickNames[i]))
                        {
                            currState[m_rightIndex].isConnected = true;
                            m_rightJoystickName = joystickNames[i];
                            m_rightJoystickNameIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (prevState[m_rightIndex].isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    if (string.IsNullOrEmpty(joystickNames[m_rightJoystickNameIndex]))
                    {
                        currState[m_rightIndex].isConnected = false;
                        m_rightJoystickName = string.Empty;
                        m_rightJoystickNameIndex = -1;
                    }
                }
            }
            // right input state
            if (currState[m_rightIndex].isConnected)
            {
                if (!prevState[m_rightIndex].isConnected)
                {
                    currState[m_rightIndex].deviceClass = VRModuleDeviceClass.Controller;
                    currState[m_rightIndex].deviceSerialID = m_rightJoystickName;
                    currState[m_rightIndex].deviceModelNumber = VRDevice.model + " Controller";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_rightIndex].deviceModel = VRModuleDeviceModel.ViveController;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_rightIndex].deviceModel = VRModuleDeviceModel.OculusTouchRight;
                    }
                    else
                    {
                        currState[m_rightIndex].deviceModel = VRModuleDeviceModel.Unknown;
                    }
                }

                currState[m_rightIndex].SetButtonPress(VRModuleRawButton.PadOrStickPress, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightTrackpadPress]));
                currState[m_rightIndex].SetButtonPress(VRModuleRawButton.PadOrStickTouch, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightTrackpadTouch]));
                currState[m_rightIndex].SetButtonPress(VRModuleRawButton.FunctionKey, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.RightMenuButtonPress]));

                currState[m_rightIndex].SetAxisValue(VRModuleRawAxis.PadOrStickX, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTrackpadHorizontal]));
                currState[m_rightIndex].SetAxisValue(VRModuleRawAxis.PadOrStickY, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTrackpadVertical]));
                currState[m_rightIndex].SetAxisValue(VRModuleRawAxis.Trigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightTriggerSqueeze]));
                currState[m_rightIndex].SetAxisValue(VRModuleRawAxis.GripOrHandTrigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.RightGripSqueeze]));
            }
            else
            {
                if (prevState[m_rightIndex].isConnected)
                {
                    currState[m_rightIndex].Reset();
                }
            }

            // left
            currState[m_leftIndex].position = InputTracking.GetLocalPosition(VRNode.LeftHand);
            currState[m_leftIndex].rotation = InputTracking.GetLocalRotation(VRNode.LeftHand);
            currState[m_leftIndex].isPoseValid = currState[m_leftIndex].pose != Pose.identity && currState[m_leftIndex].pose != prevState[m_leftIndex].pose;
            // left connected state
            if (currState[m_leftIndex].isPoseValid)
            {
                if (!prevState[m_leftIndex].isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    for (int i = joystickNames.Length - 1; i >= 0; --i)
                    {
                        if (!string.IsNullOrEmpty(joystickNames[i]) && m_leftRgx.IsMatch(joystickNames[i]))
                        {
                            currState[m_leftIndex].isConnected = true;
                            m_leftJoystickName = joystickNames[i];
                            m_leftJoystickNameIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (prevState[m_leftIndex].isConnected)
                {
                    if (joystickNames == null) { joystickNames = Input.GetJoystickNames(); }
                    if (string.IsNullOrEmpty(joystickNames[m_leftJoystickNameIndex]))
                    {
                        currState[m_leftIndex].isConnected = false;
                        m_leftJoystickName = string.Empty;
                        m_leftJoystickNameIndex = -1;
                    }
                }
            }
            // left input state
            if (currState[m_leftIndex].isConnected)
            {
                if (!prevState[m_leftIndex].isConnected)
                {
                    currState[m_leftIndex].deviceClass = VRModuleDeviceClass.Controller;
                    currState[m_leftIndex].deviceSerialID = m_leftJoystickName;
                    currState[m_leftIndex].deviceModelNumber = VRDevice.model + " Controller";

                    if (m_viveRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_leftIndex].deviceModel = VRModuleDeviceModel.ViveController;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        currState[m_leftIndex].deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                    }
                    else
                    {
                        currState[m_leftIndex].deviceModel = VRModuleDeviceModel.Unknown;
                    }
                }

                currState[m_leftIndex].SetButtonPress(VRModuleRawButton.PadOrStickPress, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftTrackpadPress]));
                currState[m_leftIndex].SetButtonPress(VRModuleRawButton.PadOrStickTouch, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftTrackpadTouch]));
                currState[m_leftIndex].SetButtonPress(VRModuleRawButton.FunctionKey, Input.GetKey(vrControllerButtonKeyCodes[(int)UnityVRControllerButton.LeftMenuButtonPress]));

                currState[m_leftIndex].SetAxisValue(VRModuleRawAxis.PadOrStickX, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTrackpadHorizontal]));
                currState[m_leftIndex].SetAxisValue(VRModuleRawAxis.PadOrStickY, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTrackpadVertical]));
                currState[m_leftIndex].SetAxisValue(VRModuleRawAxis.Trigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftTriggerSqueeze]));
                currState[m_leftIndex].SetAxisValue(VRModuleRawAxis.GripOrHandTrigger, Input.GetAxis(vrControllerAxisVirtualButtonNames[(int)UnityVRControllerAxis.LeftGripSqueeze]));
            }
            else
            {
                if (prevState[m_leftIndex].isConnected)
                {
                    currState[m_leftIndex].Reset();
                }
            }
        }
#endif
    }
}