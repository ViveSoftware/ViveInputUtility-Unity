//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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

        private const uint HEAD_INDEX = 0u;
        private const uint LEFT_INDEX = 1u;
        private const uint RIGHT_INDEX = 2u;

        private uint m_leftIndex = INVALID_DEVICE_INDEX;
        private uint m_rightIndex = INVALID_DEVICE_INDEX;

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

            EnsureDeviceStateLength(3);
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

        public override void BeforeRenderUpdate()
        {
            var joystickNames = default(string[]);

            FlushDeviceState();

            // head
            IVRModuleDeviceState headPrevState;
            IVRModuleDeviceStateRW headCurrState;
            EnsureValidDeviceState(HEAD_INDEX, out headPrevState, out headCurrState);

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
            IVRModuleDeviceState rightPrevState;
            IVRModuleDeviceStateRW rightCurrState;
            EnsureValidDeviceState(RIGHT_INDEX, out rightPrevState, out rightCurrState);

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
                            m_rightIndex = RIGHT_INDEX;
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
                        m_rightIndex = INVALID_DEVICE_INDEX;
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
                        rightCurrState.input2DType = VRModuleInput2DType.TouchpadOnly;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        rightCurrState.deviceModel = VRModuleDeviceModel.OculusTouchRight;
                        rightCurrState.input2DType = VRModuleInput2DType.JoystickOnly;
                    }
                    else
                    {
                        rightCurrState.deviceModel = VRModuleDeviceModel.Unknown;
                        rightCurrState.input2DType = VRModuleInput2DType.Unknown;
                    }

                    rightCurrState.renderModelName = VRDevice.model + " " + rightCurrState.deviceModel.ToString();
                }

                UpdateRightControllerInput(rightPrevState, rightCurrState);
            }
            else
            {
                if (rightPrevState.isConnected)
                {
                    rightCurrState.Reset();
                }
            }

            // left
            IVRModuleDeviceState leftPrevState;
            IVRModuleDeviceStateRW leftCurrState;
            EnsureValidDeviceState(LEFT_INDEX, out leftPrevState, out leftCurrState);

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
                            m_leftIndex = LEFT_INDEX;
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
                        m_leftIndex = INVALID_DEVICE_INDEX;
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
                        leftCurrState.input2DType = VRModuleInput2DType.TouchpadOnly;
                    }
                    else if (m_oculusRgx.IsMatch(VRDevice.model))
                    {
                        leftCurrState.deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                        leftCurrState.input2DType = VRModuleInput2DType.JoystickOnly;
                    }
                    else
                    {
                        leftCurrState.deviceModel = VRModuleDeviceModel.Unknown;
                        leftCurrState.input2DType = VRModuleInput2DType.Unknown;
                    }

                    leftCurrState.renderModelName = VRDevice.model + " " + leftCurrState.deviceModel.ToString();
                }

                UpdateLeftControllerInput(leftPrevState, leftCurrState);
            }
            else
            {
                if (leftPrevState.isConnected)
                {
                    leftCurrState.Reset();
                }
            }

            ProcessConnectedDeviceChanged();
            ProcessDevicePoseChanged();
            ProcessDeviceInputChanged();
        }
#endif
    }
}