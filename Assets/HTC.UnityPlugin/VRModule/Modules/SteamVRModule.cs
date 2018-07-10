//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Text;
using UnityEngine;
using Valve.VR;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#elif UNITY_5_4_OR_NEWER
using XRSettings = UnityEngine.VR.VRSettings;
#endif
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
            return VIUSettings.activateSteamVRModule && XRSettings.enabled && XRSettings.loadedDeviceName == "OpenVR";
#else
            return VIUSettings.activateSteamVRModule && SteamVR.enabled;
#endif
        }

        public override void OnActivated()
        {
            // Make sure SteamVR_Render instance exist. It Polls New Poses Event
            if (SteamVR_Render.instance == null) { }

            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                m_prevTrackingSpace = compositor.GetTrackingSpace();
                UpdateTrackingSpaceType();
            }
#if VIU_STEAMVR_1_2_1_OR_NEWER
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).AddListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_2_0_OR_NEWER
            SteamVR_Events.System("TrackedDeviceRoleChanged").AddListener(OnTrackedDeviceRoleChanged);
#else
            SteamVR_Utils.Event.Listen("TrackedDeviceRoleChanged", OnTrackedDeviceRoleChangedArgs);
#endif
        }

        public override void OnDeactivated()
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                compositor.SetTrackingSpace(m_prevTrackingSpace);
            }
#if VIU_STEAMVR_1_2_1_OR_NEWER
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceRoleChanged).RemoveListener(OnTrackedDeviceRoleChanged);
#elif VIU_STEAMVR_1_2_0_OR_NEWER
            SteamVR_Events.System("TrackedDeviceRoleChanged").RemoveListener(OnTrackedDeviceRoleChanged);
#else
            SteamVR_Utils.Event.Remove("TrackedDeviceRoleChanged", OnTrackedDeviceRoleChangedArgs);
#endif
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
            if (SteamVR.active)
            {
                SteamVR_Render.instance.lockPhysicsUpdateRateToRenderFrequency = VRModule.lockPhysicsUpdateRateToRenderFrequency;
            }
        }

        public override bool HasInputFocus()
        {
            return m_hasInputFocus;
        }
#if VIU_STEAMVR_1_1_1
        private void OnTrackedDeviceRoleChangedArgs(params object[] args) { OnTrackedDeviceRoleChanged((VREvent_t)args[0]); }
#endif
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

#if VIU_STEAMVR_1_2_3_OR_NEWER
            m_hasInputFocus = system == null ? false : system.IsInputAvailable();
#else
            m_hasInputFocus = system == null ? false : !system.IsInputFocusCapturedByAnotherProcess();
#endif

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

            for (uint i = 0; i < MAX_DEVICE_COUNT && i < OpenVR.k_unMaxTrackedDeviceCount; ++i)
            {
                currState[i].isConnected = m_rawPoses[i].bDeviceIsConnected;

                if (currState[i].isConnected)
                {
                    if (!prevState[i].isConnected)
                    {
                        currState[i].deviceClass = (VRModuleDeviceClass)system.GetTrackedDeviceClass(i);
                        currState[i].serialNumber = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_SerialNumber_String);
                        currState[i].modelNumber = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        currState[i].renderModelName = QueryDeviceStringProperty(system, i, ETrackedDeviceProperty.Prop_RenderModelName_String);

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
                        currState[i].pose = RigidPose.identity;
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
#if VIU_STEAMVR_1_2_0_OR_NEWER
                        if (system == null || !system.GetControllerState(i, ref m_ctrlState, s_sizeOfControllerStats))
#else
                        if (system == null || !system.GetControllerState(i, ref m_ctrlState))
#endif
                        {
                            m_ctrlState = default(VRControllerState_t);
                        }

                        // update device input button
                        currState[i].buttonPressed = m_ctrlState.ulButtonPressed;
                        currState[i].buttonTouched = m_ctrlState.ulButtonTouched;

                        // update device input axis
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis0X, m_ctrlState.rAxis0.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis0Y, m_ctrlState.rAxis0.y);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis1X, m_ctrlState.rAxis1.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis1Y, m_ctrlState.rAxis1.y);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis2X, m_ctrlState.rAxis2.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis2Y, m_ctrlState.rAxis2.y);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis3X, m_ctrlState.rAxis3.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis3Y, m_ctrlState.rAxis3.y);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis4X, m_ctrlState.rAxis4.x);
                        currState[i].SetAxisValue(VRModuleRawAxis.Axis4Y, m_ctrlState.rAxis4.y);
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
