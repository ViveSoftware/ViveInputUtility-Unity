﻿//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        public abstract class ModuleBase
        {
            protected enum DefaultModuleOrder
            {
                Simulator = 1,
                UnityNativeVR,
                UnityXR,
                SteamVR,
                OculusVR,
                DayDream,
                WaveVR,
            }

            [Obsolete("Module should set their own MAX_DEVICE_COUNT, use EnsureDeviceStateLength to set, VRModule.GetDeviceStateCount() to get")]
            protected const uint MAX_DEVICE_COUNT = VRModule.MAX_DEVICE_COUNT;
            protected const uint INVALID_DEVICE_INDEX = VRModule.INVALID_DEVICE_INDEX;

            private static readonly Regex s_viveRgx = new Regex("^.*(vive|htc).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_viveCosmosRgx = new Regex("^.*(cosmos).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_oculusRgx = new Regex("^.*(oculus).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_indexRgx = new Regex("^.*(index|knuckles).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_knucklesRgx = new Regex("^.*(knu_ev1).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_daydreamRgx = new Regex("^.*(daydream).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_wmrRgx = new Regex("(^.*(asus|acer|dell|lenovo|hp|samsung|windowsmr).*(mr|$))|spatial", RegexOptions.IgnoreCase);
            private static readonly Regex s_magicLeapRgx = new Regex("^.*(magicleap).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_viveFocusRgx = new Regex("^.*(wvr).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_leftRgx = new Regex("^.*(left|_l).*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_rightRgx = new Regex("^.*(right|_r).*$", RegexOptions.IgnoreCase);

            public bool isActivated { get; private set; }

            public virtual int moduleOrder { get { return moduleIndex; } }

            public abstract int moduleIndex { get; }

            public virtual bool ShouldActiveModule() { return false; }

            public void Activated()
            {
                isActivated = true;
                OnActivated();
            }

            public void Deactivated()
            {
                isActivated = false;
                OnDeactivated();
            }

            public virtual void OnActivated() { }

            public virtual void OnDeactivated() { }

            public virtual bool HasInputFocus() { return true; }
            public virtual uint GetLeftControllerDeviceIndex() { return INVALID_DEVICE_INDEX; }
            public virtual uint GetRightControllerDeviceIndex() { return INVALID_DEVICE_INDEX; }
            public virtual void UpdateTrackingSpaceType() { }
            public virtual void Update() { }
            public virtual void FixedUpdate() { }
            public virtual void LateUpdate() { }
            public virtual void BeforeRenderUpdate() { }

            [Obsolete]
            public virtual void UpdateDeviceState(IVRModuleDeviceState[] prevState, IVRModuleDeviceStateRW[] currState) { }

            public virtual void TriggerViveControllerHaptic(uint deviceIndex, ushort durationMicroSec = 500) { }

            public virtual void TriggerHapticVibration(uint deviceIndex, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f) { }

            protected void InvokeInputFocusEvent(bool value)
            {
                VRModule.InvokeInputFocusEvent(value);
            }

            protected void InvokeControllerRoleChangedEvent()
            {
                VRModule.InvokeControllerRoleChangedEvent();
            }

            protected uint GetDeviceStateLength()
            {
                return Instance.GetDeviceStateLength();
            }

            protected void EnsureDeviceStateLength(uint capacity)
            {
                Instance.EnsureDeviceStateLength(capacity);
            }

            protected bool TryGetValidDeviceState(uint index, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState)
            {
                return Instance.TryGetValidDeviceState(index, out prevState, out currState);
            }

            protected void EnsureValidDeviceState(uint index, out IVRModuleDeviceState prevState, out IVRModuleDeviceStateRW currState)
            {
                Instance.EnsureValidDeviceState(index, out prevState, out currState);
            }

            protected void FlushDeviceState()
            {
                Instance.ModuleFlushDeviceState();
            }

            protected void ProcessConnectedDeviceChanged()
            {
                Instance.ModuleConnectedDeviceChanged();
            }

            protected void ProcessDevicePoseChanged()
            {
                InvokeNewPosesEvent();
            }

            protected void ProcessDeviceInputChanged()
            {
                InvokeNewInputEvent();
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
                            if (s_viveCosmosRgx.IsMatch(deviceState.modelNumber))
                            {
                                if (s_leftRgx.IsMatch(deviceState.renderModelName))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.ViveCosmosControllerLeft;
                                }
                                else if (s_rightRgx.IsMatch(deviceState.renderModelName))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.ViveCosmosControllerRight;
                                }
                                deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                            }
                            else
                            {
                                deviceState.deviceModel = VRModuleDeviceModel.ViveController;
                                deviceState.input2DType = VRModuleInput2DType.TouchpadOnly;
                            }
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
                            if (Application.platform == RuntimePlatform.Android)
                            {
                                if (deviceState.modelNumber.Contains("Go"))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.OculusGoController;
                                    deviceState.input2DType = VRModuleInput2DType.TouchpadOnly;
                                    return;
                                }
                                else if (s_leftRgx.IsMatch(deviceState.modelNumber))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.OculusQuestControllerLeft;
                                    deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    return;
                                }
                                else if (s_rightRgx.IsMatch(deviceState.modelNumber))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.OculusQuestControllerRight;
                                    deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                    return;
                                }
                            }
                            else
                            {
                                if (deviceState.modelNumber.Contains("Rift S"))
                                {
                                    if (s_leftRgx.IsMatch(deviceState.modelNumber))
                                    {
                                        deviceState.deviceModel = VRModuleDeviceModel.OculusQuestControllerLeft;
                                        deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                        return;
                                    }
                                    else if (s_rightRgx.IsMatch(deviceState.modelNumber))
                                    {
                                        deviceState.deviceModel = VRModuleDeviceModel.OculusQuestControllerRight;
                                        deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                        return;
                                    }
                                }
                                else
                                {
                                    if (s_leftRgx.IsMatch(deviceState.modelNumber))
                                    {
                                        deviceState.deviceModel = VRModuleDeviceModel.OculusTouchLeft;
                                        deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                        return;
                                    }
                                    else if (s_rightRgx.IsMatch(deviceState.modelNumber))
                                    {
                                        deviceState.deviceModel = VRModuleDeviceModel.OculusTouchRight;
                                        deviceState.input2DType = VRModuleInput2DType.JoystickOnly;
                                        return;
                                    }
                                }
                            }
                            break;
                        case VRModuleDeviceClass.TrackingReference:
                            deviceState.deviceModel = VRModuleDeviceModel.OculusSensor;
                            return;
                    }
                }
                else if (s_wmrRgx.IsMatch(deviceState.modelNumber) || s_wmrRgx.IsMatch(deviceState.renderModelName))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.WMRHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            if (s_leftRgx.IsMatch(deviceState.modelNumber))
                            {
                                deviceState.deviceModel = VRModuleDeviceModel.WMRControllerLeft;
                                deviceState.input2DType = VRModuleInput2DType.Both;
                                return;
                            }
                            else if (s_rightRgx.IsMatch(deviceState.modelNumber))
                            {
                                deviceState.deviceModel = VRModuleDeviceModel.WMRControllerRight;
                                deviceState.input2DType = VRModuleInput2DType.Both;
                                return;
                            }
                            break;
                    }
                }
                else if (s_indexRgx.IsMatch(deviceState.modelNumber) || s_indexRgx.IsMatch(deviceState.renderModelName))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.IndexHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            deviceState.input2DType = VRModuleInput2DType.TouchpadOnly;
                            if (s_leftRgx.IsMatch(deviceState.renderModelName))
                            {
                                if (s_knucklesRgx.IsMatch(deviceState.renderModelName))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.KnucklesLeft;
                                }
                                else
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.IndexControllerLeft;
#if VIU_STEAMVR_2_0_0_OR_NEWER || (UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS)
                                    deviceState.input2DType = VRModuleInput2DType.Both;
#endif
                                }
                            }
                            else if (s_rightRgx.IsMatch(deviceState.renderModelName))
                            {
                                if (s_knucklesRgx.IsMatch(deviceState.renderModelName))
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.KnucklesRight;
                                }
                                else
                                {
                                    deviceState.deviceModel = VRModuleDeviceModel.IndexControllerRight;
#if VIU_STEAMVR_2_0_0_OR_NEWER || (UNITY_2019_3_OR_NEWER && VIU_XR_GENERAL_SETTINGS)
                                    deviceState.input2DType = VRModuleInput2DType.Both;
#endif
                                }
                            }
                            return;
                        case VRModuleDeviceClass.TrackingReference:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveBaseStation;
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
                            deviceState.input2DType = VRModuleInput2DType.TrackpadOnly;
                            return;
                    }
                }
                else if (s_magicLeapRgx.IsMatch(deviceState.modelNumber))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.MagicLeapHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            deviceState.deviceModel = VRModuleDeviceModel.MagicLeapController;
                            deviceState.input2DType = VRModuleInput2DType.TouchpadOnly;
                            return;
                    }
                }

                else if (s_viveFocusRgx.IsMatch(deviceState.modelNumber))
                {
                    switch (deviceState.deviceClass)
                    {
                        case VRModuleDeviceClass.HMD:
                            deviceState.deviceModel = VRModuleDeviceModel.ViveFocusHMD;
                            return;
                        case VRModuleDeviceClass.Controller:
                            deviceState.input2DType = VRModuleInput2DType.TouchpadOnly;
                            deviceState.deviceModel = VRModuleDeviceModel.ViveFocusChirp;
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

        private sealed class DefaultModule : ModuleBase
        {
            public override int moduleIndex { get { return (int)VRModuleActiveEnum.None; } }
        }
    }
}