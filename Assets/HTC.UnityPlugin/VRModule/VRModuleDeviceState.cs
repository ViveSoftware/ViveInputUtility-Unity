//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public enum VRModuleDeviceClass
    {
        Invalid,
        HMD,
        Controller,
        GenericTracker,
        TrackingReference,
    }

    public enum VRModuleDeviceModel
    {
        Unknown,
        ViveHMD,
        ViveController,
        ViveTracker,
        ViveBaseStation,
        OculusHMD,
        OculusTouchLeft,
        OculusTouchRight,
        OculusSensor,
        KnucklesLeft,
        KnucklesRight,
        DaydreamHMD,
        DaydreamController,
        ViveFocusHMD,
        ViveFocusFinch,
        OculusGoController,
        OculusGearVrController,
    }

    public enum VRModuleRawButton
    {
        System = 0,
        ApplicationMenu = 1,
        Grip = 2,
        DPadLeft = 3,
        DPadUp = 4,
        DPadRight = 5,
        DPadDown = 6,
        A = 7,
        ProximitySensor = 31,
        Axis0 = 32,
        Axis1 = 33,
        Axis2 = 34,
        Axis3 = 35,
        Axis4 = 36,

        // alias
        DashboardBack = 2, // Grip
        Touchpad = 32, // Axis0
        Trigger = 33, // Axis1
        CapSenseGrip = 34, // Axis2
    }

    public enum VRModuleRawAxis
    {
        Axis0X,
        Axis0Y,
        Axis1X,
        Axis1Y,
        Axis2X,
        Axis2Y,
        Axis3X,
        Axis3Y,
        Axis4X,
        Axis4Y,

        // alias
        TouchpadX = Axis0X,
        TouchpadY = Axis0Y,
        Trigger = Axis1X,
        CapSenseGrip = Axis2X,
        IndexCurl = Axis3X,
        MiddleCurl = Axis3Y,
        RingCurl = Axis4X,
        PinkyCurl = Axis4Y,
    }

    public interface IVRModuleDeviceStateRW
    {
        uint deviceIndex { get; }
        string serialNumber { get; set; }
        string modelNumber { get; set; }
        string renderModelName { get; set; }
        VRModuleDeviceClass deviceClass { get; set; }
        VRModuleDeviceModel deviceModel { get; set; }

        bool isConnected { get; set; }
        bool isPoseValid { get; set; }
        bool isOutOfRange { get; set; }
        bool isCalibrating { get; set; }
        bool isUninitialized { get; set; }
        Vector3 velocity { get; set; }
        Vector3 angularVelocity { get; set; }
        Vector3 position { get; set; }
        Quaternion rotation { get; set; }
        RigidPose pose { get; set; }

        ulong buttonPressed { get; set; }
        ulong buttonTouched { get; set; }
        float[] axisValue { get; }

        bool GetButtonPress(VRModuleRawButton button);
        bool GetButtonTouch(VRModuleRawButton button);
        float GetAxisValue(VRModuleRawAxis axis);

        void SetButtonPress(VRModuleRawButton button, bool value);
        void SetButtonTouch(VRModuleRawButton button, bool value);
        void SetAxisValue(VRModuleRawAxis axis, float value);
        void ResetAxisValues();
        void Reset();
    }

    public interface IVRModuleDeviceState
    {
        uint deviceIndex { get; }
        string serialNumber { get; }
        string modelNumber { get; }
        string renderModelName { get; }
        VRModuleDeviceClass deviceClass { get; }
        VRModuleDeviceModel deviceModel { get; }

        bool isConnected { get; }
        bool isPoseValid { get; }
        bool isOutOfRange { get; }
        bool isCalibrating { get; }
        bool isUninitialized { get; }
        Vector3 velocity { get; }
        Vector3 angularVelocity { get; }
        Vector3 position { get; }
        Quaternion rotation { get; }
        RigidPose pose { get; }

        ulong buttonPressed { get; }
        ulong buttonTouched { get; }

        bool GetButtonPress(VRModuleRawButton button);
        bool GetButtonTouch(VRModuleRawButton button);
        float GetAxisValue(VRModuleRawAxis axis);
    }

    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        [Serializable]
        private class DeviceState : IVRModuleDeviceState, IVRModuleDeviceStateRW
        {
            [SerializeField]
            private string m_serialNumber;
            [SerializeField]
            private string m_modelNumber;
            [SerializeField]
            private string m_renderModelName;
            [SerializeField]
            private VRModuleDeviceClass m_deviceClass;
            [SerializeField]
            private VRModuleDeviceModel m_deviceModel;

            [SerializeField]
            private bool m_isPoseValid;
            [SerializeField]
            private bool m_isConnected;
            [SerializeField]
            private bool m_isOutOfRange;
            [SerializeField]
            private bool m_isCalibrating;
            [SerializeField]
            private bool m_isUninitialized;
            [SerializeField]
            private Vector3 m_velocity;
            [SerializeField]
            private Vector3 m_angularVelocity;
            [SerializeField]
            private Vector3 m_position;
            [SerializeField]
            private Quaternion m_rotation;

            // device property, changed only when connected or disconnected
            public uint deviceIndex { get; private set; }
            public string serialNumber { get { return m_serialNumber; } set { m_serialNumber = value; } }
            public string modelNumber { get { return m_modelNumber; } set { m_modelNumber = value; } }
            public string renderModelName { get { return m_renderModelName; } set { m_renderModelName = value; } }
            public VRModuleDeviceClass deviceClass { get { return m_deviceClass; } set { m_deviceClass = value; } }
            public VRModuleDeviceModel deviceModel { get { return m_deviceModel; } set { m_deviceModel = value; } }
            // device pose state
            public bool isPoseValid { get { return m_isPoseValid; } set { m_isPoseValid = value; } }
            public bool isConnected { get { return m_isConnected; } set { m_isConnected = value; } }
            public bool isOutOfRange { get { return m_isOutOfRange; } set { m_isOutOfRange = value; } }
            public bool isCalibrating { get { return m_isCalibrating; } set { m_isCalibrating = value; } }
            public bool isUninitialized { get { return m_isUninitialized; } set { m_isUninitialized = value; } }
            public Vector3 velocity { get { return m_velocity; } set { m_velocity = value; } }
            public Vector3 angularVelocity { get { return m_angularVelocity; } set { m_angularVelocity = value; } }
            public Vector3 position { get { return m_position; } set { m_position = value; } }
            public Quaternion rotation { get { return m_rotation; } set { m_rotation = value; } }
            public RigidPose pose { get { return new RigidPose(m_position, m_rotation); } set { m_position = value.pos; m_rotation = value.rot; } }

            // device input state
            [SerializeField]
            public ulong m_buttonPressed;
            [SerializeField]
            public ulong m_buttonTouched;
            [SerializeField]
            public float[] m_axisValue;

            public ulong buttonPressed { get { return m_buttonPressed; } set { m_buttonPressed = value; } }
            public ulong buttonTouched { get { return m_buttonTouched; } set { m_buttonTouched = value; } }
            public float[] axisValue { get { return m_axisValue; } }

            public bool GetButtonPress(VRModuleRawButton button) { return EnumUtils.GetFlag(m_buttonPressed, (int)button); }
            public bool GetButtonTouch(VRModuleRawButton button) { return EnumUtils.GetFlag(m_buttonTouched, (int)button); }
            public float GetAxisValue(VRModuleRawAxis axis) { return m_axisValue[(int)axis]; }

            public void SetButtonPress(VRModuleRawButton button, bool value) { m_buttonPressed = value ? EnumUtils.SetFlag(m_buttonPressed, (int)button) : EnumUtils.UnsetFlag(m_buttonPressed, (int)button); }
            public void SetButtonTouch(VRModuleRawButton button, bool value) { m_buttonTouched = value ? EnumUtils.SetFlag(m_buttonTouched, (int)button) : EnumUtils.UnsetFlag(m_buttonTouched, (int)button); }
            public void SetAxisValue(VRModuleRawAxis axis, float value) { m_axisValue[(int)axis] = value; }
            public void ResetAxisValues() { Array.Clear(m_axisValue, 0, m_axisValue.Length); }

            public DeviceState(uint deviceIndex)
            {
                this.deviceIndex = deviceIndex;
                this.m_axisValue = new float[EnumUtils.GetMaxValue(typeof(VRModuleRawAxis)) + 1];
                Reset();
            }

            public void CopyFrom(DeviceState state)
            {
                m_serialNumber = state.m_serialNumber;
                m_modelNumber = state.m_modelNumber;
                m_renderModelName = state.m_renderModelName;
                m_deviceClass = state.m_deviceClass;
                m_deviceModel = state.m_deviceModel;

                m_isPoseValid = state.m_isPoseValid;
                m_isConnected = state.m_isConnected;
                m_isOutOfRange = state.m_isOutOfRange;
                m_isCalibrating = state.m_isCalibrating;
                m_isUninitialized = state.m_isUninitialized;
                m_velocity = state.m_velocity;
                m_angularVelocity = state.m_angularVelocity;
                m_position = state.m_position;
                m_rotation = state.m_rotation;

                m_buttonPressed = state.m_buttonPressed;
                m_buttonTouched = state.m_buttonTouched;
                Array.Copy(state.m_axisValue, m_axisValue, m_axisValue.Length);
            }

            public void Reset()
            {
                deviceClass = VRModuleDeviceClass.Invalid;
                serialNumber = string.Empty;
                modelNumber = string.Empty;
                renderModelName = string.Empty;
                isConnected = false;
                isPoseValid = false;
                isOutOfRange = false;
                isCalibrating = false;
                isUninitialized = false;
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                m_position = Vector3.zero;
                m_rotation = Quaternion.identity;

                m_buttonPressed = 0ul;
                m_buttonTouched = 0ul;
                ResetAxisValues();
            }
        }
    }
}