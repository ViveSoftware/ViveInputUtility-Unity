//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using System;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public interface ISimulatorVRModule
    {
        event Action onActivated;
        event Action onDeactivated;
        event SimulatorVRModule.UpdateDeviceStateHandler onUpdateDeviceState;
    }

    public class SimulatorVRModule : VRModule.ModuleBase, ISimulatorVRModule
    {
        public delegate void UpdateDeviceStateHandler(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState);

        private const uint RIGHT_INDEX = 1;
        private const uint LEFT_INDEX = 2;
        private static readonly RigidPose m_initHmdPose = new RigidPose(new Vector3(0f, 1.75f, 0f), Quaternion.identity);

        private bool m_prevXREnabled;
        private bool m_resetDevices;

        public event Action onActivated;
        public event Action onDeactivated;
        public event UpdateDeviceStateHandler onUpdateDeviceState;

        public override bool ShouldActiveModule() { return VIUSettings.activateSimulatorModule; }

        public override void OnActivated()
        {
            if (m_prevXREnabled = XRSettings.enabled)
            {
                XRSettings.enabled = false;
            }

            m_resetDevices = true;

            if (onActivated != null)
            {
                onActivated();
            }
        }

        public override void OnDeactivated()
        {
            UpdateMainCamTracking();

            XRSettings.enabled = m_prevXREnabled;

            if (onDeactivated != null)
            {
                onDeactivated();
            }
        }

        public override uint GetRightControllerDeviceIndex() { return RIGHT_INDEX; }

        public override uint GetLeftControllerDeviceIndex() { return LEFT_INDEX; }

        public override void Update()
        {
            if (VIUSettings.enableSimulatorKeyboardMouseControl)
            {
                UpdateAlphaKeyDown();
            }
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            if (VIUSettings.enableSimulatorKeyboardMouseControl)
            {
                // Reset to default state
                if (m_resetDevices)
                {
                    m_resetDevices = false;

                    foreach (var state in currState)
                    {
                        switch (state.deviceIndex)
                        {
                            case VRModule.HMD_DEVICE_INDEX:
                            case RIGHT_INDEX:
                            case LEFT_INDEX:
                                InitializeDevice(currState[VRModule.HMD_DEVICE_INDEX], state);
                                break;
                            default:
                                if (state.isConnected)
                                {
                                    state.Reset();
                                }
                                break;
                        }
                    }

                    SelectDevice(currState[VRModule.HMD_DEVICE_INDEX]);
                }

                // select/deselect device
                var keySelectDevice = default(IVRModuleDeviceStateRW);
                if (GetDeviceByInputDownKeyCode(currState, out keySelectDevice))
                {
                    if (IsShiftKeyPressed())
                    {
                        if (keySelectDevice.isConnected && keySelectDevice.deviceIndex != VRModule.HMD_DEVICE_INDEX)
                        {
                            if (IsSelectedDevice(keySelectDevice))
                            {
                                DeselectDevice();
                            }

                            keySelectDevice.Reset();
                        }
                    }
                    else
                    {
                        if (!IsSelectedDevice(keySelectDevice))
                        {
                            // select
                            if (!keySelectDevice.isConnected)
                            {
                                InitializeDevice(currState[VRModule.HMD_DEVICE_INDEX], keySelectDevice);
                            }

                            SelectDevice(keySelectDevice);
                        }
                        else
                        {
                            // deselect
                            DeselectDevice();
                        }
                    }
                }

                // control selected device
                var selectedDevice = VRModule.IsValidDeviceIndex(m_selectedDeviceIndex) && currState[m_selectedDeviceIndex].isConnected ? currState[m_selectedDeviceIndex] : null;
                if (selectedDevice != null)
                {
                    ControlDevice(selectedDevice);

                    if (selectedDevice.deviceClass != VRModuleDeviceClass.HMD)
                    {
                        HandleDeviceInput(selectedDevice);
                    }
                }

                // control camera
                if (currState[VRModule.HMD_DEVICE_INDEX].isConnected)
                {
                    ControlCamera(currState[VRModule.HMD_DEVICE_INDEX]);
                }
            }
            else if (IsDeviceSelected())
            {
                DeselectDevice();
            }

            if (onUpdateDeviceState != null)
            {
                onUpdateDeviceState(prevState, currState);
            }

            UpdateMainCamTracking();
        }

        private bool m_autoTrackMainCam;
        private RigidPose m_mainCamStartPose;
        private void UpdateMainCamTracking()
        {
            if (VIUSettings.simulatorAutoTrackMainCamera)
            {
                if (!m_autoTrackMainCam)
                {
                    m_autoTrackMainCam = true;
                    m_mainCamStartPose = new RigidPose(Camera.main.transform, true);
                }

                if (Camera.main != null)
                {
                    var hmd = VRModule.GetDeviceState(VRModule.HMD_DEVICE_INDEX);
                    if (hmd.isConnected)
                    {
                        RigidPose.SetPose(Camera.main.transform, hmd.pose);
                    }
                }
            }
            else
            {
                if (m_autoTrackMainCam)
                {
                    m_autoTrackMainCam = false;

                    if (Camera.main != null)
                    {
                        RigidPose.SetPose(Camera.main.transform, m_mainCamStartPose);
                    }
                }
            }
        }

        private uint m_selectedDeviceIndex;
        private bool IsSelectedDevice(IVRModuleDeviceStateRW state)
        {
            return m_selectedDeviceIndex == state.deviceIndex;
        }

        private bool IsDeviceSelected()
        {
            return VRModule.IsValidDeviceIndex(m_selectedDeviceIndex);
        }

        private void SelectDevice(IVRModuleDeviceStateRW state)
        {
            m_selectedDeviceIndex = state.deviceIndex;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void DeselectDevice()
        {
            m_selectedDeviceIndex = VRModule.INVALID_DEVICE_INDEX;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Input.GetKeyDown in UpdateDeviceState is not working
        private bool m_shiftKeyPressed;
        private bool[] m_alphaKeyDownState = new bool[10];
        private void UpdateAlphaKeyDown()
        {
            m_shiftKeyPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            m_alphaKeyDownState[0] = Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0);
            m_alphaKeyDownState[1] = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            m_alphaKeyDownState[2] = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            m_alphaKeyDownState[3] = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            m_alphaKeyDownState[4] = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            m_alphaKeyDownState[5] = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
            m_alphaKeyDownState[6] = Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
            m_alphaKeyDownState[7] = Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7);
            m_alphaKeyDownState[8] = Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8);
            m_alphaKeyDownState[9] = Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);
        }

        private bool IsShiftKeyPressed()
        {
            return m_shiftKeyPressed;
        }

        private bool IsAlphaKeyDown(int num)
        {
            return m_alphaKeyDownState[num];
        }

        private bool GetDeviceByInputDownKeyCode(IVRModuleDeviceStateRW[] deviceStates, out IVRModuleDeviceStateRW deviceState)
        {
            var backQuatePressed = Input.GetKey(KeyCode.BackQuote);
            if (!backQuatePressed && IsAlphaKeyDown(0)) { deviceState = deviceStates[0]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(1)) { deviceState = deviceStates[1]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(2)) { deviceState = deviceStates[2]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(3)) { deviceState = deviceStates[3]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(4)) { deviceState = deviceStates[4]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(5)) { deviceState = deviceStates[5]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(6)) { deviceState = deviceStates[6]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(7)) { deviceState = deviceStates[7]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(8)) { deviceState = deviceStates[8]; return true; }
            if (!backQuatePressed && IsAlphaKeyDown(9)) { deviceState = deviceStates[9]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(0)) { deviceState = deviceStates[10]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(1)) { deviceState = deviceStates[11]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(2)) { deviceState = deviceStates[12]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(3)) { deviceState = deviceStates[13]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(4)) { deviceState = deviceStates[14]; return true; }
            if (backQuatePressed && IsAlphaKeyDown(5)) { deviceState = deviceStates[15]; return true; }

            deviceState = null;
            return false;
        }

        private void InitializeDevice(IVRModuleDeviceStateRW hmdState, IVRModuleDeviceStateRW deviceState)
        {
            switch (deviceState.deviceIndex)
            {
                case VRModule.HMD_DEVICE_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.HMD;
                        deviceState.serialNumber = "VIU Simulator HMD Device";
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveHMD;

                        deviceState.isPoseValid = true;
                        deviceState.pose = m_initHmdPose;

                        break;
                    }
                case RIGHT_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.Controller;
                        deviceState.serialNumber = "VIU Simulator Controller Device " + RIGHT_INDEX;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveController;

                        var pose = new RigidPose(new Vector3(0.3f, -0.7f, 0.4f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : m_initHmdPose) * pose;
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
                case LEFT_INDEX:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.Controller;
                        deviceState.serialNumber = "VIU Simulator Controller Device " + LEFT_INDEX;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveController;

                        var pose = new RigidPose(new Vector3(-0.3f, -0.7f, 0.4f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : m_initHmdPose) * pose;
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
                default:
                    {
                        deviceState.isConnected = true;
                        deviceState.deviceClass = VRModuleDeviceClass.GenericTracker;
                        deviceState.serialNumber = "VIU Simulator Generic Tracker Device " + deviceState.deviceIndex;
                        deviceState.modelNumber = deviceState.serialNumber;
                        deviceState.renderModelName = deviceState.serialNumber;
                        deviceState.deviceModel = VRModuleDeviceModel.ViveTracker;

                        var pose = new RigidPose(new Vector3(0f, -0.7f, 0.4f), Quaternion.identity);
                        deviceState.isPoseValid = true;
                        deviceState.pose = (hmdState.isConnected ? hmdState.pose : m_initHmdPose) * pose;
                        deviceState.buttonPressed = 0ul;
                        deviceState.buttonTouched = 0ul;
                        deviceState.ResetAxisValues();
                        break;
                    }
            }
        }

        private void ControlDevice(IVRModuleDeviceStateRW deviceState)
        {
            var pose = deviceState.pose;
            var poseEuler = pose.rot.eulerAngles;
            var deltaAngle = Time.unscaledDeltaTime * VIUSettings.simulatorMouseRotateSpeed;
            var deltaKeyAngle = Time.unscaledDeltaTime * VIUSettings.simulatorKeyRotateSpeed;

            poseEuler.x = Mathf.Repeat(poseEuler.x + 180f, 360f) - 180f;

            if (!IsShiftKeyPressed())
            {
                var pitchDelta = -Input.GetAxisRaw("Mouse Y") * deltaAngle;
                if (pitchDelta > 0f)
                {
                    if (poseEuler.x < 90f && poseEuler.x > -180f)
                    {
                        poseEuler.x = Mathf.Min(90f, poseEuler.x + pitchDelta);
                    }
                }
                else if (pitchDelta < 0f)
                {
                    if (poseEuler.x < 180f && poseEuler.x > -90f)
                    {
                        poseEuler.x = Mathf.Max(-90f, poseEuler.x + pitchDelta);
                    }
                }

                poseEuler.y += Input.GetAxisRaw("Mouse X") * deltaAngle;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                if (poseEuler.x < 90f && poseEuler.x > -180f)
                {
                    poseEuler.x = Mathf.Min(90f, poseEuler.x + deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                if (poseEuler.x < 180f && poseEuler.x > -90f)
                {
                    poseEuler.x = Mathf.Max(-90f, poseEuler.x - deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.RightArrow)) { poseEuler.y += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.LeftArrow)) { poseEuler.y -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.C)) { poseEuler.z += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.Z)) { poseEuler.z -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.X)) { poseEuler.z = 0f; }

            pose.rot = Quaternion.Euler(poseEuler);

            var deltaMove = Time.unscaledDeltaTime * VIUSettings.simulatorKeyMoveSpeed;
            var moveForward = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.forward;
            var moveRight = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.right;
            if (Input.GetKey(KeyCode.D)) { pose.pos += moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.A)) { pose.pos -= moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.E)) { pose.pos += Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.Q)) { pose.pos -= Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.W)) { pose.pos += moveForward * deltaMove; }
            if (Input.GetKey(KeyCode.S)) { pose.pos -= moveForward * deltaMove; }

            deviceState.pose = pose;
        }

        private void ControlCamera(IVRModuleDeviceStateRW deviceState)
        {
            var pose = deviceState.pose;
            var poseEuler = pose.rot.eulerAngles;
            var deltaKeyAngle = Time.unscaledDeltaTime * VIUSettings.simulatorKeyRotateSpeed;

            poseEuler.x = Mathf.Repeat(poseEuler.x + 180f, 360f) - 180f;

            if (Input.GetKey(KeyCode.K))
            {
                if (poseEuler.x < 90f && poseEuler.x > -180f)
                {
                    poseEuler.x = Mathf.Min(90f, poseEuler.x + deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.I))
            {
                if (poseEuler.x < 180f && poseEuler.x > -90f)
                {
                    poseEuler.x = Mathf.Max(-90f, poseEuler.x - deltaKeyAngle);
                }
            }

            if (Input.GetKey(KeyCode.L)) { poseEuler.y += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.J)) { poseEuler.y -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.N)) { poseEuler.z += deltaKeyAngle; }
            if (Input.GetKey(KeyCode.V)) { poseEuler.z -= deltaKeyAngle; }
            if (Input.GetKey(KeyCode.B)) { poseEuler.z = 0f; }

            pose.rot = Quaternion.Euler(poseEuler);

            var deltaMove = Time.unscaledDeltaTime * VIUSettings.simulatorKeyMoveSpeed;
            var moveForward = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.forward;
            var moveRight = Quaternion.Euler(0f, poseEuler.y, 0f) * Vector3.right;
            if (Input.GetKey(KeyCode.H)) { pose.pos += moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.F)) { pose.pos -= moveRight * deltaMove; }
            if (Input.GetKey(KeyCode.Y)) { pose.pos += Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.R)) { pose.pos -= Vector3.up * deltaMove; }
            if (Input.GetKey(KeyCode.T)) { pose.pos += moveForward * deltaMove; }
            if (Input.GetKey(KeyCode.G)) { pose.pos -= moveForward * deltaMove; }

            deviceState.pose = pose;
        }

        private void HandleDeviceInput(IVRModuleDeviceStateRW deviceState)
        {
            var leftPressed = Input.GetMouseButton(0);
            var midPressed = Input.GetMouseButton(1);
            var rightPressed = Input.GetMouseButton(2);

            deviceState.SetButtonPress(VRModuleRawButton.Trigger, leftPressed);
            deviceState.SetButtonTouch(VRModuleRawButton.Trigger, leftPressed);
            deviceState.SetAxisValue(VRModuleRawAxis.Trigger, leftPressed ? 1f : 0f);

            deviceState.SetButtonPress(VRModuleRawButton.Grip, midPressed);
            deviceState.SetButtonTouch(VRModuleRawButton.Grip, midPressed);
            deviceState.SetAxisValue(VRModuleRawAxis.CapSenseGrip, midPressed ? 1f : 0f);

            deviceState.SetButtonPress(VRModuleRawButton.Touchpad, rightPressed);

            if (VIUSettings.simulateTrackpadTouch && IsShiftKeyPressed())
            {
                deviceState.SetButtonTouch(VRModuleRawButton.Touchpad, true);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadX, deviceState.GetAxisValue(VRModuleRawAxis.TouchpadX) + (Input.GetAxisRaw("Mouse X") * 0.1f));
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadY, deviceState.GetAxisValue(VRModuleRawAxis.TouchpadY) + (Input.GetAxisRaw("Mouse Y") * 0.1f));
            }
            else
            {
                deviceState.SetButtonTouch(VRModuleRawButton.Touchpad, rightPressed);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadX, 0f);
                deviceState.SetAxisValue(VRModuleRawAxis.TouchpadY, 0f);
            }
        }
    }
}