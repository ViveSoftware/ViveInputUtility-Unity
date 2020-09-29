﻿//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using System.Collections.Generic;
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
        TrackedHand,
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
        ViveFocusChirp,
        OculusGoController,
        OculusGearVrController,
        WMRHMD,
        WMRControllerLeft,
        WMRControllerRight,
        ViveCosmosControllerLeft,
        ViveCosmosControllerRight,
        OculusQuestControllerLeft,
        OculusQuestControllerRight,
        OculusQuestOrRiftSControllerLeft = OculusQuestControllerLeft,
        OculusQuestOrRiftSControllerRight = OculusQuestControllerRight,
        IndexHMD,
        IndexControllerLeft,
        IndexControllerRight,
        MagicLeapHMD,
        MagicLeapController,
        WaveTrackedHandLeft,
        WaveTrackedHandRight,
        OculusTrackedHandLeft,
        OculusTrackedHandRight,
        WMRTrackedHandLeft,
        WMRTrackedHandRight,
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
        DashboardBack = 2, // Grip
        Touchpad = 32, // Axis0
        Trigger = 33, // Axis1
        CapSenseGrip = 34, // Axis2
        Bumper = 35, // Axis3

        // alias
        Axis0 = 32,
        Axis1 = 33,
        Axis2 = 34,
        Axis3 = 35,
        Axis4 = 36,

        // Gestures
        GestureIndexPinch,
        GestureMiddlePinch,
        GestureRingPinch,
        GesturePinkyPinch,
        GestureFist,
        GestureFive,
        GestureOk,
        GestureThumbUp,
        GestureIndexUp,
    }

    public enum VRModuleRawAxis
    {
        TouchpadX = Axis0X,
        TouchpadY = Axis0Y,
        Trigger = Axis1X,
        CapSenseGrip = Axis2X,
        IndexCurl = Axis3X,
        MiddleCurl = Axis3Y,
        RingCurl = Axis4X,
        PinkyCurl = Axis4Y,

        JoystickX = Axis2X,
        JoystickY = Axis2Y,

        // alias
        Axis0X = 0,
        Axis0Y,
        Axis1X,
        Axis1Y,
        Axis2X,
        Axis2Y,
        Axis3X,
        Axis3Y,
        Axis4X,
        Axis4Y,

        // Gestures
        IndexPinch,
        MiddlePinch,
        RingPinch,
        PinkyPinch,
    }

    public enum VRModuleInput2DType
    {
        None,
        Unknown,
        TouchpadOnly,
        ThumbstickOnly,
        Both,

        TrackpadOnly = TouchpadOnly,
        JoystickOnly = ThumbstickOnly,
    }

    public interface IVRModuleDeviceStateRW
    {
        uint deviceIndex { get; }
        string serialNumber { get; set; }
        string modelNumber { get; set; }
        string renderModelName { get; set; }
        VRModuleDeviceClass deviceClass { get; set; }
        VRModuleDeviceModel deviceModel { get; set; }
        VRModuleInput2DType input2DType { get; set; }

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
        HandJointPose[] handJoints { get; set; }

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
        VRModuleInput2DType input2DType { get; }

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
        bool TryGetHandJointPose(HandJointName jointName, out RigidPose pose);
        void GetAllHandJoints(IList<HandJointPose> outHandJoints, bool trimInvalidJoint = true);
        void GetFingerJoints(FingerName fingerName, IList<HandJointPose> outHandJoints, bool trimInvalidJoint = true);
        int GetHandJointCount();
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
            private VRModuleInput2DType m_input2DType;

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
            [SerializeField]
            private HandJointPose[] m_handJoints = new HandJointPose[HandJointPose.GetMaxCount()];

            // device property, changed only when connected or disconnected
            public uint deviceIndex { get; private set; }
            public string serialNumber { get { return m_serialNumber; } set { m_serialNumber = value; } }
            public string modelNumber { get { return m_modelNumber; } set { m_modelNumber = value; } }
            public string renderModelName { get { return m_renderModelName; } set { m_renderModelName = value; } }
            public VRModuleDeviceClass deviceClass { get { return m_deviceClass; } set { m_deviceClass = value; } }
            public VRModuleDeviceModel deviceModel { get { return m_deviceModel; } set { m_deviceModel = value; } }
            public VRModuleInput2DType input2DType { get { return m_input2DType; } set { m_input2DType = value; } }
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
            public HandJointPose[] handJoints { get { return m_handJoints; } set { m_handJoints = value; } } 

            // device input state
            [SerializeField]
            private ulong m_buttonPressed;
            [SerializeField]
            private ulong m_buttonTouched;
            [SerializeField]
            private float[] m_axisValue;

            public ulong buttonPressed { get { return m_buttonPressed; } set { m_buttonPressed = value; } }
            public ulong buttonTouched { get { return m_buttonTouched; } set { m_buttonTouched = value; } }
            public float[] axisValue { get { return m_axisValue; } }

            public bool GetButtonPress(VRModuleRawButton button) { return EnumUtils.GetFlag(m_buttonPressed, (int)button); }
            public bool GetButtonTouch(VRModuleRawButton button) { return EnumUtils.GetFlag(m_buttonTouched, (int)button); }
            public float GetAxisValue(VRModuleRawAxis axis) { return m_axisValue[(int)axis]; }

            public bool TryGetHandJointPose(HandJointName jointName, out RigidPose pose)
            {
                HandJointPose joint = m_handJoints[HandJointPose.NameToIndex(jointName)];
                pose = joint.pose;

                return joint.IsValid();
            }

            public void GetAllHandJoints(IList<HandJointPose> outHandJoints, bool trimInvalidJoint = true)
            {
                if (outHandJoints == null)
                {
                    return;
                }

                foreach (HandJointPose joint in m_handJoints)
                {
                    if (trimInvalidJoint && !joint.IsValid())
                    {
                        continue;
                    }

                    outHandJoints.Add(joint);
                }
            }

            public void GetFingerJoints(FingerName fingerName, IList<HandJointPose> outHandJoints, bool trimInvalidJoint = true)
            {
                if (outHandJoints == null)
                {
                    return;
                }

                switch (fingerName)
                {
                    case FingerName.Thumb:
                        GetRangeHandJoints(HandJointName.ThumbMetacarpal, HandJointName.ThumbTip, outHandJoints, trimInvalidJoint);
                        break;
                    case FingerName.Index:
                        GetRangeHandJoints(HandJointName.IndexMetacarpal, HandJointName.IndexTip, outHandJoints, trimInvalidJoint);
                        break;
                    case FingerName.Middle:
                        GetRangeHandJoints(HandJointName.MiddleMetacarpal, HandJointName.MiddleTip, outHandJoints, trimInvalidJoint);
                        break;
                    case FingerName.Ring:
                        GetRangeHandJoints(HandJointName.RingMetacarpal, HandJointName.RingTip, outHandJoints, trimInvalidJoint);
                        break;
                    case FingerName.Pinky:
                        GetRangeHandJoints(HandJointName.PinkyMetacarpal, HandJointName.PinkyTip, outHandJoints, trimInvalidJoint);
                        break;
                }
            }

            public int GetHandJointCount()
            {
                int count = 0;
                foreach (HandJointPose joint in m_handJoints)
                {
                    if (joint.IsValid())
                    {
                        count++;
                    }
                }

                return count;
            }

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
                m_input2DType = state.m_input2DType;

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

                Array.Copy(state.m_handJoints, m_handJoints, state.m_handJoints.Length);
            }

            public void Reset()
            {
                deviceClass = VRModuleDeviceClass.Invalid;
                input2DType = VRModuleInput2DType.None;
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

            private void GetRangeHandJoints(HandJointName start, HandJointName end, IList<HandJointPose> outHandJoints, bool trimInvalidJoint)
            {
                if (outHandJoints == null)
                {
                    return;
                }

                for (int i = (int) start; i <= (int) end; i++)
                {
                    HandJointPose joint = m_handJoints[i];
                    if (trimInvalidJoint && !joint.IsValid())
                    {
                        continue;
                    }

                    outHandJoints.Add(joint);
                }
            }
        }
    }

    public static class VRModuleDeviceModelExtension
    {
        public static bool IsRight(this VRModuleDeviceModel deviceModel)
        {
            switch(deviceModel)
            {
                case VRModuleDeviceModel.OculusTouchRight:
                case VRModuleDeviceModel.KnucklesRight:
                case VRModuleDeviceModel.WMRControllerRight:
                case VRModuleDeviceModel.ViveCosmosControllerRight:
                case VRModuleDeviceModel.OculusQuestControllerRight:
                case VRModuleDeviceModel.IndexControllerRight:
                case VRModuleDeviceModel.WaveTrackedHandRight:
                case VRModuleDeviceModel.OculusTrackedHandRight:
                case VRModuleDeviceModel.WMRTrackedHandRight:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsLeft(this VRModuleDeviceModel deviceModel)
        {
            switch (deviceModel)
            {
                case VRModuleDeviceModel.OculusTouchLeft:
                case VRModuleDeviceModel.KnucklesLeft:
                case VRModuleDeviceModel.WMRControllerLeft:
                case VRModuleDeviceModel.ViveCosmosControllerLeft:
                case VRModuleDeviceModel.OculusQuestControllerLeft:
                case VRModuleDeviceModel.IndexControllerLeft:
                case VRModuleDeviceModel.WaveTrackedHandLeft:
                case VRModuleDeviceModel.OculusTrackedHandLeft:
                case VRModuleDeviceModel.WMRTrackedHandLeft:
                    return true;
                default:
                    return false;
            }
        }
    }
}