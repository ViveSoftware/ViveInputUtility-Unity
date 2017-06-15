//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR
using HTC.UnityPlugin.PoseTracker;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed class SteamVRModule : VRModule.ModuleBase
    {
#if VIU_STEAMVR
        private static readonly uint s_sizeOfControllerStats = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));
        private static readonly StringBuilder s_sb = new StringBuilder();

        private ETrackingUniverseOrigin m_prevTrackingSpace;
        private VRControllerState_t m_ctrlState;
        private readonly TrackedDevicePose_t[] m_rawPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private readonly TrackedDevicePose_t[] m_rawGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        private bool m_hasInputFocus = true;

        public override bool ShouldActiveModule()
        {
#if UNITY_5_4_OR_NEWER
            return VRSettings.enabled && VRSettings.loadedDeviceName == "OpenVR";
#else
            return SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            var system = OpenVR.System;
            if (system != null)
            {
                m_hasInputFocus = !system.IsInputFocusCapturedByAnotherProcess();
            }

            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                m_prevTrackingSpace = compositor.GetTrackingSpace();
                UpdateTrackingSpaceType();
            }

            SteamVR_Events.InputFocus.AddListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).AddListener(OnTrackedDeviceRoleChanged);
        }

        public override void OnDeactivated()
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                compositor.SetTrackingSpace(m_prevTrackingSpace);
            }

            SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).RemoveListener(OnTrackedDeviceRoleChanged);
        }

        public override void UpdateTrackingSpaceType()
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                switch (VRModule.trackingSpaceType)
                {
                    case VRModuleTrackingSpaceType.RoomScale:
                        compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseStanding);
                        break;
                    case VRModuleTrackingSpaceType.Stationary:
                        compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseSeated);
                        break;
                }
            }
        }

        public override void Update()
        {
            SteamVR_Render.instance.lockPhysicsUpdateRateToRenderFrequency = VRModule.lockPhysicsUpdateRateToRenderFrequency;
        }

        public override bool HasInputFocus()
        {
            return m_hasInputFocus;
        }

        private void OnInputFocus(bool value)
        {
            m_hasInputFocus = value;
            InvokeInputFocusEvent(value);
        }

        private void OnTrackedDeviceRoleChanged(VREvent_t arg)
        {
            InvokeControllerRoleChangedEvent();
        }

        public override uint GetLeftControllerDeviceIndex()
        {
            var system = OpenVR.System;
            return system == null ? INVALID_DEVICE_INDEX : system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
        }

        public override uint GetRightControllerDeviceIndex()
        {
            var system = OpenVR.System;
            return system == null ? INVALID_DEVICE_INDEX : system.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
        }

        public override void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500)
        {
            var system = OpenVR.System;
            if (system != null)
            {
                system.TriggerHapticPulse(deviceIndex, (uint)EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)EVRButtonId.k_EButton_Axis0, (char)durationMicroSec);
            }
        }

        public override void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
        {
            var system = OpenVR.System;
            var compositor = OpenVR.Compositor;

            if (compositor != null)
            {
                compositor.GetLastPoses(m_rawPoses, m_rawGamePoses);
            }
            else
            {
                for (uint i = 0; i < MAX_DEVICE_COUNT; ++i)
                {
                    if (prevState[i].isConnected) { currState[i].Reset(); }
                }
                return;
            }

            for (uint i = 0; i < MAX_DEVICE_COUNT; ++i)
            {
                currState[i].isConnected = m_rawPoses[i].bDeviceIsConnected;

                if (currState[i].isConnected)
                {
                    if (!prevState[i].isConnected)
                    {
                        currState[i].deviceClass = (VRModuleDeviceClass)system.GetTrackedDeviceClass(currState[i].deviceIndex);
                        currState[i].deviceSerialID = QueryDeviceStringProperty(system, currState[i].deviceIndex, ETrackedDeviceProperty.Prop_SerialNumber_String);
                        currState[i].deviceModelNumber = QueryDeviceStringProperty(system, currState[i].deviceIndex, ETrackedDeviceProperty.Prop_ModelNumber_String);

                        SetupKnownDeviceModel(currState[i]);
                    }

                    // update device status
                    currState[i].isPoseValid = m_rawPoses[i].bPoseIsValid;
                    currState[i].isOutOfRange = m_rawPoses[i].eTrackingResult == ETrackingResult.Running_OutOfRange || m_rawPoses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                    currState[i].isCalibrating = m_rawPoses[i].eTrackingResult == ETrackingResult.Calibrating_InProgress || m_rawPoses[i].eTrackingResult == ETrackingResult.Calibrating_OutOfRange;
                    currState[i].isUninitialized = m_rawPoses[i].eTrackingResult == ETrackingResult.Uninitialized;
                    currState[i].velocity = new Vector3(m_rawPoses[i].vVelocity.v0, m_rawPoses[i].vVelocity.v1, -m_rawPoses[i].vVelocity.v2);
                    currState[i].angularVelocity = new Vector3(-m_rawPoses[i].vAngularVelocity.v0, -m_rawPoses[i].vAngularVelocity.v1, m_rawPoses[i].vAngularVelocity.v2);

                    // update poses
                    if (prevState[i].isPoseValid && !currState[i].isPoseValid)
                    {
                        currState[i].pose = Pose.identity;
                    }
                    else if (currState[i].isPoseValid)
                    {
                        var rigidTransform = new SteamVR_Utils.RigidTransform(m_rawPoses[i].mDeviceToAbsoluteTracking);
                        currState[i].position = rigidTransform.pos;
                        currState[i].rotation = rigidTransform.rot;
                    }

                    if (currState[i].deviceClass == VRModuleDeviceClass.Controller || currState[i].deviceClass == VRModuleDeviceClass.GenericTracker)
                    {
                        // get device state from openvr api
                        if (system == null || !system.GetControllerState(i, ref m_ctrlState, s_sizeOfControllerStats))
                        {
                            m_ctrlState = default(VRControllerState_t);
                        }

                        // update device input button
                        currState[i].SetButtonPress(VRModuleRawButton.PadOrStickPress, (m_ctrlState.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0ul);
                        currState[i].SetButtonPress(VRModuleRawButton.PadOrStickTouch, (m_ctrlState.ulButtonTouched & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0ul);
                        currState[i].SetButtonPress(VRModuleRawButton.FunctionKey, (m_ctrlState.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0ul);

                        // update device input axis
                        currState[i].SetAxisValue(VRModuleRawAxis.PadOrStickX, m_ctrlState.rAxis0.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.PadOrStickY, m_ctrlState.rAxis0.y);
                        currState[i].SetAxisValue(VRModuleRawAxis.Trigger, m_ctrlState.rAxis1.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.GripOrHandTrigger, (m_ctrlState.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0ul ? 1f : 0f);
                    }
                }
                else
                {
                    if (prevState[i].isConnected)
                    {
                        currState[i].Reset();
                    }
                }
            }
        }

        private static string QueryDeviceStringProperty(CVRSystem system, uint deviceIndex, ETrackedDeviceProperty prop)
        {
            var error = default(ETrackedPropertyError);
            var capacity = (int)system.GetStringTrackedDeviceProperty(deviceIndex, prop, null, 0, ref error);
            if (capacity <= 1 || capacity > 128) { return string.Empty; }

            system.GetStringTrackedDeviceProperty(deviceIndex, prop, s_sb, (uint)s_sb.EnsureCapacity(capacity), ref error);
            if (error != ETrackedPropertyError.TrackedProp_Success) { return string.Empty; }

            var result = s_sb.ToString();
            s_sb.Length = 0;

            return result;
        }
#endif
    }
}