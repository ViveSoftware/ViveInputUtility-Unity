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
                }
            }

            if (GvrControllerInput.State == GvrConnectionState.Error)
            {
                Debug.LogError(GvrControllerInput.ErrorDetails);
                return;
            }

            if (m_gvrArmModelInstance == null)
            {
                m_gvrArmModelInstance = VRModule.Instance.GetComponent<GvrArmModel>();

                if (m_gvrArmModelInstance == null)
                {
                    m_gvrArmModelInstance = VRModule.Instance.gameObject.AddComponent<GvrArmModel>();
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

            ctrlCurrState.isConnected = GvrControllerInput.State == GvrConnectionState.Connected;

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
                ctrlCurrState.isPoseValid = GvrControllerInput.Orientation != Quaternion.identity;
                ctrlCurrState.velocity = GvrControllerInput.Accel;
                ctrlCurrState.angularVelocity = GvrControllerInput.Gyro;

                ctrlCurrState.SetButtonPress(VRModuleRawButton.Touchpad, GvrControllerInput.ClickButton);
                ctrlCurrState.SetButtonPress(VRModuleRawButton.ApplicationMenu, GvrControllerInput.AppButton);
                ctrlCurrState.SetButtonPress(VRModuleRawButton.System, GvrControllerInput.HomeButtonState);

                ctrlCurrState.SetButtonTouch(VRModuleRawButton.Touchpad, GvrControllerInput.IsTouching);

                if (GvrControllerInput.IsTouching)
                {
                    var touchPadPosCentered = GvrControllerInput.TouchPosCentered;
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
                    ctrlCurrState.SetButtonPress(VRModuleRawButton.Trigger, GvrControllerInput.ClickButton);
                    ctrlCurrState.SetButtonTouch(VRModuleRawButton.Trigger, GvrControllerInput.IsTouching);
                    ctrlCurrState.SetAxisValue(VRModuleRawAxis.Trigger, GvrControllerInput.ClickButton ? 1f : 0f);
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