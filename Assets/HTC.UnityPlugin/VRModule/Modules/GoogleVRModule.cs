//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
#if VIU_GOOGLEVR && UNITY_5_6_OR_NEWER
using UnityEngine;
using HTC.UnityPlugin.Vive;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
using XRNode = UnityEngine.VR.VRNode;
using InputTracking = UnityEngine.VR.InputTracking;
#endif

#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public static readonly bool isGoogleVRPluginDetected =
#if VIU_GOOGLEVR
            true;
#else
            false;
#endif
        public static readonly bool isGoogleVRSupported =
#if VIU_GOOGLEVR_SUPPORT
            true;
#else
            false;
#endif
    }

    public sealed class GoogleVRModule : VRModule.ModuleBase
    {
        public override int moduleOrder { get { return (int)DefaultModuleOrder.DayDream; } }

        public override int moduleIndex { get { return (int)VRModuleSelectEnum.DayDream; } }

#if VIU_GOOGLEVR && UNITY_5_6_OR_NEWER
        private const uint HEAD_INDEX = 0u;

        private uint m_rightIndex = INVALID_DEVICE_INDEX;
        private uint m_leftIndex = INVALID_DEVICE_INDEX;

        public override uint GetRightControllerDeviceIndex() { return m_rightIndex; }

        public override uint GetLeftControllerDeviceIndex() { return m_leftIndex; }

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateGoogleVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "daydream";
        }

        public override void Update()
        {
            UpdateDeviceInput();
            ProcessDeviceInputChanged();
        }

        public override void BeforeRenderUpdate()
        {
            FlushDeviceState();
            UpdateConnectedDevices();
            ProcessConnectedDeviceChanged();
            UpdateDevicePose();
            ProcessDevicePoseChanged();
        }

#if VIU_GOOGLEVR_1_150_0_NEWER
        private const uint RIGHT_HAND_INDEX = 1u;
        private const uint LEFT_HAND_INDEX = 2u;

        private GvrControllerInputDevice m_rightDevice;
        private GvrControllerInputDevice m_leftDevice;
        private GvrArmModel m_rightArm;
        private GvrArmModel m_leftArm;

        public override void OnActivated()
        {
            EnsureDeviceStateLength(3);

            if (Object.FindObjectOfType<GvrHeadset>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<GvrHeadset>();
            }

            if (Object.FindObjectOfType<GvrControllerInput>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<GvrControllerInput>();
            }

            m_rightDevice = GvrControllerInput.GetDevice(GvrControllerHand.Dominant);
            m_leftDevice = GvrControllerInput.GetDevice(GvrControllerHand.Dominant);

            var armModels = VRModule.Instance.GetComponents<GvrArmModel>();

            if (armModels != null && armModels.Length >= 1)
            {
                m_rightArm = armModels[0];
            }
            else
            {
                m_rightArm = VRModule.Instance.GetComponent<GvrArmModel>();

                if (m_rightArm == null)
                {
                    m_rightArm = VRModule.Instance.gameObject.AddComponent<GvrArmModel>();
                }
            }
            m_rightArm.ControllerInputDevice = m_rightDevice;

            if (armModels != null && armModels.Length >= 2)
            {
                m_leftArm = armModels[1];
            }
            else
            {
                m_leftArm = VRModule.Instance.GetComponent<GvrArmModel>();

                if (m_leftArm == null)
                {
                    m_leftArm = VRModule.Instance.gameObject.AddComponent<GvrArmModel>();
                }
            }
            m_leftArm.ControllerInputDevice = m_leftDevice;
        }

        // update connected devices
        private void UpdateConnectedDevices()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(HEAD_INDEX, out prevState, out currState);
            if (!XRDevice.isPresent)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();
                }
            }
            else
            {
                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = VRModuleDeviceClass.HMD;
                    currState.serialNumber = XRDevice.model + " HMD";
                    currState.modelNumber = XRDevice.model + " HMD";
                    currState.deviceModel = VRModuleDeviceModel.DaydreamHMD;
                    currState.renderModelName = string.Empty;
                }
            }

            EnsureValidDeviceState(RIGHT_HAND_INDEX, out prevState, out currState);
            if (m_rightDevice.State != GvrConnectionState.Connected)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();
                    m_rightIndex = INVALID_DEVICE_INDEX;
                }
            }
            else
            {
                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = VRModuleDeviceClass.Controller;
                    currState.serialNumber = XRDevice.model + " Controller Right";
                    currState.modelNumber = XRDevice.model + " Controller Right";
                    currState.deviceModel = VRModuleDeviceModel.DaydreamController;
                    currState.renderModelName = string.Empty;
                    currState.input2DType = VRModuleInput2DType.TouchpadOnly;
                    m_rightIndex = RIGHT_HAND_INDEX;
                }
            }

            EnsureValidDeviceState(LEFT_HAND_INDEX, out prevState, out currState);
            if (m_leftDevice.State != GvrConnectionState.Connected)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();
                    m_leftIndex = INVALID_DEVICE_INDEX;
                }
            }
            else
            {
                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = VRModuleDeviceClass.Controller;
                    currState.serialNumber = XRDevice.model + " Controller Left";
                    currState.modelNumber = XRDevice.model + " Controller Left";
                    currState.deviceModel = VRModuleDeviceModel.DaydreamController;
                    currState.renderModelName = string.Empty;
                    currState.input2DType = VRModuleInput2DType.TouchpadOnly;
                    m_leftIndex = RIGHT_HAND_INDEX;
                }
            }
        }

        private void UpdateDevicePose()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(HEAD_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                currState.position = InputTracking.GetLocalPosition(XRNode.Head);
                currState.rotation = InputTracking.GetLocalRotation(XRNode.Head);
                currState.isPoseValid = currState.pose != RigidPose.identity;
            }

            EnsureValidDeviceState(RIGHT_HAND_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                currState.position = m_rightArm.ControllerPositionFromHead;
                currState.rotation = m_rightArm.ControllerRotationFromHead;
                currState.isPoseValid = m_rightDevice.Orientation != Quaternion.identity;
            }

            EnsureValidDeviceState(LEFT_HAND_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                currState.position = m_leftArm.ControllerPositionFromHead;
                currState.rotation = m_leftArm.ControllerRotationFromHead;
                currState.isPoseValid = m_leftDevice.Orientation != Quaternion.identity;
            }
        }

        private void UpdateDeviceInput()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(RIGHT_HAND_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                var appPressed = m_rightDevice.GetButton(GvrControllerButton.App);
                var systemPressed = m_rightDevice.GetButton(GvrControllerButton.System);
                var padPressed = m_rightDevice.GetButton(GvrControllerButton.TouchPadButton);
                var padTouched = m_rightDevice.GetButton(GvrControllerButton.TouchPadTouch);
                var padAxis = m_rightDevice.TouchPos;

                currState.SetButtonPress(VRModuleRawButton.Touchpad, padPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, appPressed);
                currState.SetButtonPress(VRModuleRawButton.System, systemPressed);

                currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouched);

                currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.TouchpadY, padAxis.y);

                if (VIUSettings.daydreamSyncPadPressToTrigger)
                {
                    currState.SetButtonPress(VRModuleRawButton.Trigger, padPressed);
                    currState.SetButtonTouch(VRModuleRawButton.Trigger, padTouched);
                    currState.SetAxisValue(VRModuleRawAxis.Trigger, padPressed ? 1f : 0f);
                }
            }

            EnsureValidDeviceState(LEFT_HAND_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                var appPressed = m_leftDevice.GetButton(GvrControllerButton.App);
                var systemPressed = m_leftDevice.GetButton(GvrControllerButton.System);
                var padPressed = m_leftDevice.GetButton(GvrControllerButton.TouchPadButton);
                var padTouched = m_leftDevice.GetButton(GvrControllerButton.TouchPadTouch);
                var padAxis = m_leftDevice.TouchPos;

                currState.SetButtonPress(VRModuleRawButton.Touchpad, padPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, appPressed);
                currState.SetButtonPress(VRModuleRawButton.System, systemPressed);

                currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouched);

                currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.TouchpadY, padAxis.y);

                if (VIUSettings.daydreamSyncPadPressToTrigger)
                {
                    currState.SetButtonPress(VRModuleRawButton.Trigger, padPressed);
                    currState.SetButtonTouch(VRModuleRawButton.Trigger, padTouched);
                    currState.SetAxisValue(VRModuleRawAxis.Trigger, padPressed ? 1f : 0f);
                }
            }
        }
#else
        public const uint CONTROLLER_INDEX = 1u;

        private GvrArmModel m_gvrArmModel;

        public override void OnActivated()
        {
            EnsureDeviceStateLength(2);

            if (Object.FindObjectOfType<GvrHeadset>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<GvrHeadset>();
            }

            if (Object.FindObjectOfType<GvrControllerInput>() == null)
            {
                VRModule.Instance.gameObject.AddComponent<GvrControllerInput>();
            }

            m_gvrArmModel = VRModule.Instance.GetComponent<GvrArmModel>();
            if (m_gvrArmModel == null)
            {
                m_gvrArmModel = VRModule.Instance.gameObject.AddComponent<GvrArmModel>();
            }
        }

        // update connected devices
        private void UpdateConnectedDevices()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(HEAD_INDEX, out prevState, out currState);
            if (!XRDevice.isPresent)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();
                }
            }
            else
            {
                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = VRModuleDeviceClass.HMD;
                    currState.serialNumber = XRDevice.model + " HMD";
                    currState.modelNumber = XRDevice.model + " HMD";
                    currState.deviceModel = VRModuleDeviceModel.DaydreamHMD;
                    currState.renderModelName = string.Empty;
                }
            }

            var controllerRoleChanged = false;
            EnsureValidDeviceState(CONTROLLER_INDEX, out prevState, out currState);
            if (GvrControllerInput.State != GvrConnectionState.Connected)
            {
                if (prevState.isConnected)
                {
                    currState.Reset();
                }
            }
            else
            {
                if (!prevState.isConnected)
                {
                    currState.isConnected = true;
                    currState.deviceClass = VRModuleDeviceClass.Controller;
                    currState.serialNumber = XRDevice.model + " Controller";
                    currState.modelNumber = XRDevice.model + " Controller";
                    currState.deviceModel = VRModuleDeviceModel.DaydreamController;
                    currState.renderModelName = string.Empty;
                }

                switch (GvrSettings.Handedness)
                {
                    case GvrSettings.UserPrefsHandedness.Right:
                        controllerRoleChanged = !VRModule.IsValidDeviceIndex(m_rightIndex) && m_leftIndex == CONTROLLER_INDEX;
                        m_rightIndex = CONTROLLER_INDEX;
                        m_leftIndex = INVALID_DEVICE_INDEX;
                        break;
                    case GvrSettings.UserPrefsHandedness.Left:
                        controllerRoleChanged = m_rightIndex == CONTROLLER_INDEX && !VRModule.IsValidDeviceIndex(m_leftIndex);
                        m_rightIndex = INVALID_DEVICE_INDEX;
                        m_leftIndex = CONTROLLER_INDEX;
                        break;
                    case GvrSettings.UserPrefsHandedness.Error:
                    default:
                        Debug.LogError("GvrSettings.Handedness error");
                        break;
                }
            }

            if (controllerRoleChanged)
            {
                InvokeControllerRoleChangedEvent();
            }
        }

        private void UpdateDevicePose()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(HEAD_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                currState.position = InputTracking.GetLocalPosition(XRNode.Head);
                currState.rotation = InputTracking.GetLocalRotation(XRNode.Head);
                currState.isPoseValid = currState.pose != RigidPose.identity;
            }

            EnsureValidDeviceState(CONTROLLER_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                currState.position = m_gvrArmModel.ControllerPositionFromHead;
                currState.rotation = m_gvrArmModel.ControllerRotationFromHead;
                currState.isPoseValid = GvrControllerInput.Orientation != Quaternion.identity;
            }
        }

        private void UpdateDeviceInput()
        {
            IVRModuleDeviceState prevState;
            IVRModuleDeviceStateRW currState;

            EnsureValidDeviceState(CONTROLLER_INDEX, out prevState, out currState);
            if (currState.isConnected)
            {
                var appPressed = GvrControllerInput.AppButton;
                var homePressed = GvrControllerInput.HomeButtonState;
                var padPressed = GvrControllerInput.ClickButton;
                var padTouched = GvrControllerInput.IsTouching;
                var padAxis = GvrControllerInput.TouchPosCentered;

                currState.SetButtonPress(VRModuleRawButton.Touchpad, padPressed);
                currState.SetButtonPress(VRModuleRawButton.ApplicationMenu, appPressed);
                currState.SetButtonPress(VRModuleRawButton.System, homePressed);

                currState.SetButtonTouch(VRModuleRawButton.Touchpad, padTouched);

                currState.SetAxisValue(VRModuleRawAxis.TouchpadX, padAxis.x);
                currState.SetAxisValue(VRModuleRawAxis.TouchpadY, padAxis.y);

                if (VIUSettings.daydreamSyncPadPressToTrigger)
                {
                    currState.SetButtonPress(VRModuleRawButton.Trigger, padPressed);
                    currState.SetButtonTouch(VRModuleRawButton.Trigger, padTouched);
                    currState.SetAxisValue(VRModuleRawAxis.Trigger, padPressed ? 1f : 0f);
                }
            }
        }
#endif

#endif
    }
}