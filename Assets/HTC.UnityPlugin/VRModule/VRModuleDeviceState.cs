//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
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
    }

    public enum VRModuleRawButton
    {
        PadOrStickPress,
        PadOrStickTouch,
        FunctionKey,
    }

    public enum VRModuleRawAxis
    {
        PadOrStickX,
        PadOrStickY,
        Trigger,
        GripOrHandTrigger,
    }

    public interface IVRModuleDeviceStateRW
    {
        uint deviceIndex { get; }
        string deviceSerialID { get; set; }
        string deviceModelNumber { get; set; }
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
        Pose pose { get; set; }

        bool GetButtonPress(VRModuleRawButton button);
        float GetAxisValue(VRModuleRawAxis axis);
        void SetButtonPress(VRModuleRawButton button, bool value);
        void SetAxisValue(VRModuleRawAxis axis, float value);
        void Reset();
    }

    public interface IVRModuleDeviceState
    {
        uint deviceIndex { get; }
        string deviceSerialID { get; }
        string deviceModelNumber { get; }
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
        Pose pose { get; }

        bool GetButtonPress(VRModuleRawButton button);
        float GetAxisValue(VRModuleRawAxis axis);
    }

    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        [Serializable]
        private class DeviceState : IVRModuleDeviceState, IVRModuleDeviceStateRW
        {
            [SerializeField]
            private string m_deviceSerialID;
            [SerializeField]
            private string m_deviceModelNumber;
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
            public string deviceSerialID { get { return m_deviceSerialID; } set { m_deviceSerialID = value; } }
            public string deviceModelNumber { get { return m_deviceModelNumber; } set { m_deviceModelNumber = value; } }
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
            public Pose pose { get { return new Pose(m_position, m_rotation); } set { m_position = value.pos; m_rotation = value.rot; } }
            // device input state
            [SerializeField]
            public bool[] m_buttonPress;
            [SerializeField]
            public float[] m_axisValue;

            public bool GetButtonPress(VRModuleRawButton button) { return m_buttonPress[(int)button]; }
            public float GetAxisValue(VRModuleRawAxis axis) { return m_axisValue[(int)axis]; }

            public void SetButtonPress(VRModuleRawButton button, bool value) { m_buttonPress[(int)button] = value; }
            public void SetAxisValue(VRModuleRawAxis axis, float value) { m_axisValue[(int)axis] = value; }

            public DeviceState(uint deviceIndex)
            {
                this.deviceIndex = deviceIndex;
                this.m_buttonPress = new bool[EnumUtils.GetMaxValue(typeof(VRModuleRawButton)) + 1];
                this.m_axisValue = new float[EnumUtils.GetMaxValue(typeof(VRModuleRawAxis)) + 1];
                Reset();
            }

            public void CopyFrom(DeviceState state)
            {
                deviceClass = state.deviceClass;
                deviceSerialID = state.deviceSerialID;
                deviceModelNumber = state.deviceModelNumber;
                isConnected = state.isConnected;
                isPoseValid = state.isPoseValid;
                isOutOfRange = state.isOutOfRange;
                isCalibrating = state.isCalibrating;
                isUninitialized = state.isUninitialized;
                velocity = state.velocity;
                angularVelocity = state.angularVelocity;
                pose = state.pose;

                for (int i = m_buttonPress.Length - 1; i >= 0; --i) { m_buttonPress[i] = state.m_buttonPress[i]; }
                for (int i = m_axisValue.Length - 1; i >= 0; --i) { m_axisValue[i] = state.m_axisValue[i]; }
            }

            public void Reset()
            {
                deviceClass = VRModuleDeviceClass.Invalid;
                deviceSerialID = string.Empty;
                deviceModelNumber = string.Empty;
                isConnected = false;
                isPoseValid = false;
                isOutOfRange = false;
                isCalibrating = false;
                isUninitialized = false;
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                pose = Pose.identity;

                for (int i = m_buttonPress.Length - 1; i >= 0; --i) { m_buttonPress[i] = false; }
                for (int i = m_axisValue.Length - 1; i >= 0; --i) { m_axisValue[i] = 0f; }
            }
        }
    }
}