//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Text.RegularExpressions;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public abstract class ModuleBase
        {
            protected const uint MAX_DEVICE_COUNT = VRModule.MAX_DEVICE_COUNT;
            protected const uint INVALID_DEVICE_INDEX = VRModule.INVALID_DEVICE_INDEX;

            private static readonly Regex s_viveRgx = new Regex("^.*(vive|htc).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_oculusRgx = new Regex("^.*(oculus).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_knucklesRgx = new Regex("^.*(knuckles).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_daydreamRgx = new Regex("^.*(daydream).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_leftRgx = new Regex("^.*left.*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_rightRgx = new Regex("^.*right.*$", RegexOptions.IgnoreCase);

            public virtual bool ShouldActiveModule() { return false; }

            public virtual void OnActivated() { }

            public virtual void OnDeactivated() { }

            public virtual bool HasInputFocus() { return true; }
            public virtual uint GetLeftControllerDeviceIndex() { return INVALID_DEVICE_INDEX; }
            public virtual uint GetRightControllerDeviceIndex() { return INVALID_DEVICE_INDEX; }
            public virtual void UpdateTrackingSpaceType() { }
            public virtual void Update() { }

            public virtual void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState)
            {
                for (uint i = 0; i < MAX_DEVICE_COUNT; ++i)
                {
                    if (prevState[i].isConnected) { currState[i].Reset(); }
                }
            }

            public virtual void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500) { }

            protected void InvokeInputFocusEvent(bool value)
            {
                VRModule.InvokeInputFocusEvent(value);
            }

            protected void InvokeControllerRoleChangedEvent()
            {
                VRModule.InvokeControllerRoleChangedEvent();
            }

            protected static void SetupKnownDeviceModel(IVRModuleDeviceStateRW deviceState)
            {
                if (s_viveRgx.IsMatch(deviceState.modelNumber) || s_viveRgx.IsMatch(deviceState.renderModelName))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveController;
                            return;
                        case VRModuleDeviceClass.GenericTracker:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveTracker;
                            return;
                        case VRModuleDeviceClass.TrackingReference:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveBaseStation;
                            return;
                    }
                }
                else if (s_oculusRgx.IsMatch(deviceState.modelNumber))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.OculusHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            if (s_leftRgx.IsMatch(deviceState.modelNumber))
                            {
                                deviceState.deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                                return;
                            }
                            else if (s_rightRgx.IsMatch(deviceState.modelNumber))
                            {
                                deviceState.deviceModel = VRModuleDeviceModel.OculusTouchRight;
                                return;
                            }
                            break;
                        case VRModuleDeviceClass.TrackingReference:
                            deviceState.deviceModel = VRModuleDeviceModel.OculusSensor;
                            return;
                    }
                }
                else if (deviceState.deviceClass == VRModuleDeviceClass.Controller && s_knucklesRgx.IsMatch(deviceState.modelNumber))
                {
                    if (s_leftRgx.IsMatch(deviceState.renderModelName))
                    {
                        deviceState.deviceModel = VRModuleDeviceModel.KnucklesLeft;
                        return;
                    }
                    else if (s_rightRgx.IsMatch(deviceState.renderModelName))
                    {
                        deviceState.deviceModel = VRModuleDeviceModel.KnucklesRight;
                        return;
                    }
                }
                else if (s_daydreamRgx.IsMatch(deviceState.modelNumber))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.DaydreamHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            deviceState.deviceModel = VRModuleDeviceModel.DaydreamController;
                            return;
                    }
                }


                deviceState.deviceModel = VRModuleDeviceModel.Unknown;
            }

            public static bool AxisToPress(bool previousPressedState, float currentAxisValue, float setThreshold, float unsetThreshold)
            {
                return previousPressedState ? currentAxisValue > unsetThreshold : currentAxisValue >= setThreshold;
            }
        }

        private sealed class DefaultModule : ModuleBase { }
    }
}