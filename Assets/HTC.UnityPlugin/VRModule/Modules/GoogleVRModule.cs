//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if VIU_GOOGLEVR && UNITY_5_6_OR_NEWER

using UnityEngine;
using HTC.UnityPlugin.Utility;
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
    public sealed class GoogleVRModule : VRModule.ModuleBase
    {
#if VIU_GOOGLEVR && UNITY_5_6_OR_NEWER
        public const uint CONTROLLER_DEVICE_INDEX = 1u;

        private GvrHeadset m_gvrHeadSetInstance;
        private GvrControllerInput m_gvrCtrlInputInstance;
        private GvrArmModel m_gvrArmModelInstance;

#if VIU_GOOGLEVR_1_150_0_NEWER
        private GvrControllerInputDevice m_gvrCtrlInputDevice;
#endif

        public override uint GetRightControllerDeviceIndex() { return CONTROLLER_DEVICE_INDEX; }

        public override bool ShouldActiveModule()
        {
            return VIUSettings.activateGoogleVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "daydream";
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (m_gvrCtrlInputInstance == null)
            {
                m_gvrCtrlInputInstance = Object.FindObjectOfType<GvrControllerInput>();

                if (m_gvrCtrlInputInstance == null)
                {
                    m_gvrCtrlInputInstance = VRModule.Instance.gameObject.AddComponent<GvrControllerInput>();
#if VIU_GOOGLEVR_1_150_0_NEWER
                    m_gvrCtrlInputDevice = GvrControllerInput.GetDevice(GvrControllerHand.Dominant);
#endif
                }
            }
#if VIU_GOOGLEVR_1_150_0_NEWER
            if (m_gvrCtrlInputDevice.State == GvrConnectionState.Error)
            {
                Debug.LogError(m_gvrCtrlInputDevice.ErrorDetails);
                return;
            }
#else
            if (GvrControllerInput.State == GvrConnectionState.Error)
            {
                Debug.LogError(GvrControllerInput.ErrorDetails);
                return;
            }
#endif

            if (m_gvrArmModelInstance == null)
            {
                m_gvrArmModelInstance = VRModule.Instance.GetComponent<GvrArmModel>();

                if (m_gvrArmModelInstance == null)
                {
                    m_gvrArmModelInstance = VRModule.Instance.gameObject.AddComponent<GvrArmModel>();
#if VIU_GOOGLEVR_1_150_0_NEWER
                    m_gvrArmModelInstance.ControllerInputDevice = m_gvrCtrlInputDevice;
#endif
                }
            }

            if (m_gvrHeadSetInstance == null)
            {
                m_gvrHeadSetInstance = Object.FindObjectOfType<GvrHeadset>();

                if (m_gvrHeadSetInstance == null)
                {
                    m_gvrHeadSetInstance = VRModule.Instance.gameObject.AddComponent<GvrHeadset>();
                }
            }

            var headPrevState = prevState[VRModule.HMD_DEVICE_INDEX];
            var headCurrState = currState[VRModule.HMD_DEVICE_INDEX];

            headCurrState.isConnected = XRDevice.isPresent;

            if (headCurrState.isConnected)
            {
                if (!headPrevState.isConnected)
                {
                    headCurrState.deviceClass = VRModuleDeviceClass.HMD;
                    headCurrState.serialNumber = XRDevice.model + " HMD";
                    headCurrState.modelNumber = XRDevice.model + " HMD";

                    headCurrState.deviceModel = VRModuleDeviceModel.DaydreamHMD;
                    headCurrState.renderModelName = string.Empty;
                }

                headCurrState.position = InputTracking.GetLocalPosition(XRNode.Head);
                headCurrState.rotation = InputTracking.GetLocalRotation(XRNode.Head);
                headCurrState.isPoseValid = headCurrState.pose != RigidPose.identity;

                headCurrState.pose = headCurrState.pose;
            }
            else
            {
                if (headPrevState.isConnected)
                {
                    headCurrState.Reset();
                }
            }

            var ctrlPrevState = prevState[CONTROLLER_DEVICE_INDEX];
            var ctrlCurrState = currState[CONTROLLER_DEVICE_INDEX];

#if VIU_GOOGLEVR_1_150_0_NEWER
            ctrlCurrState.isConnected = m_gvrCtrlInputDevice.State == GvrConnectionState.Connected;
#else
            ctrlCurrState.isConnected = GvrControllerInput.State == GvrConnectionState.Connected;
#endif

            if (ctrlCurrState.isConnected)
            {
                if (!ctrlPrevState.isConnected)
                {
                    ctrlCurrState.deviceClass = VRModuleDeviceClass.Controller;
                    ctrlCurrState.serialNumber = XRDevice.model + " Controller";
                    ctrlCurrState.modelNumber = XRDevice.model + " Controller";

                    ctrlCurrState.deviceModel = VRModuleDeviceModel.DaydreamController;
                    ctrlCurrState.renderModelName = string.Empty;
                }

                ctrlCurrState.pose = new RigidPose(m_gvrArmModelInstance.ControllerPositionFromHead, m_gvrArmModelInstance.ControllerRotationFromHead);

#if VIU_GOOGLEVR_1_150_0_NEWER
                ctrlCurrState.isPoseValid = m_gvrCtrlInputDevice.Orientation != Quaternion.identity;
                ctrlCurrState.velocity = m_gvrCtrlInputDevice.Accel;
                ctrlCurrState.angularVelocity = m_gvrCtrlInputDevice.Gyro;

                ctrlCurrState.SetButtonPress(VRModuleRawButton.Touchpad, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadButton));
                ctrlCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.App));
                ctrlCurrState.SetButtonPress(VRModuleRawButton.System, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.System));

                ctrlCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadTouch));
#else
                ctrlCurrState.isPoseValid = GvrControllerInput.Orientation != Quaternion.identity;
                ctrlCurrState.velocity = GvrControllerInput.Accel;
                ctrlCurrState.angularVelocity = GvrControllerInput.Gyro;

                ctrlCurrState.SetButtonPress(VRModuleRawButton.Touchpad, GvrControllerInput.ClickButton);
                ctrlCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, GvrControllerInput.AppButton);
                ctrlCurrState.SetButtonPress(VRModuleRawButton.System, GvrControllerInput.HomeButtonState);

                ctrlCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, GvrControllerInput.IsTouching);
#endif

#if VIU_GOOGLEVR_1_150_0_NEWER
                if (m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadTouch))
#else
                if (GvrControllerInput.IsTouching)
#endif
                {
#if VIU_GOOGLEVR_1_150_0_NEWER
                    var touchPadPosCentered = m_gvrCtrlInputDevice.TouchPos;
#else
                    var touchPadPosCentered = GvrControllerInput.TouchPosCentered;
#endif
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, touchPadPosCentered.x);
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, touchPadPosCentered.y);
                }
                else
                {
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
                }

                if (VIUSettings.daydreamSyncPadPressToTrigger)
                {
#if VIU_GOOGLEVR_1_150_0_NEWER
                    ctrlCurrState.SetButtonPress(VRModuleRawButton.Trigger, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadButton));
                    ctrlCurrState.SetButtonTouch(VRModuleRawButton.Trigger, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadTouch));
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.Trigger, m_gvrCtrlInputDevice.GetButton(GvrControllerButton.TouchPadButton) ? 1f : 0f);
#else
                    ctrlCurrState.SetButtonPress(VRModuleRawButton.Trigger, GvrControllerInput.ClickButton);
                    ctrlCurrState.SetButtonTouch(VRModuleRawButton.Trigger, GvrControllerInput.IsTouching);
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.Trigger, GvrControllerInput.ClickButton ? 1f : 0f);
#endif
                }
            }
            else
            {
                if (ctrlPrevState.isConnected)
                {
                    ctrlCurrState.Reset();
                }
            }
        }
#endif
                }
}