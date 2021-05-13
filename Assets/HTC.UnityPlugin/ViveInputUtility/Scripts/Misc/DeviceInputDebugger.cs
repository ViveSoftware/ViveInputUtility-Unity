using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class DeviceInputDebugger : MonoBehaviour
    {
        private class VRModuleRawAxisReslver : EnumToIntResolver<VRModuleRawAxis> { public override int Resolve(VRModuleRawAxis e) { return (int)e; } }
        private class ControllerAxisReslver : EnumToIntResolver<ControllerAxis> { public override int Resolve(ControllerAxis e) { return (int)e; } }

        [Serializable]
        public class RawAxisValueArray : EnumArray<VRModuleRawAxis, float> { }
        [Serializable]
        public class AxisValueArray : EnumArray<ControllerAxis, float> { }

        [Serializable]
        public class DeviceStateDisplay
        {
            public uint deviceIndex;
            public string serialNumber;
            public string modelNumber;
            public string renderModelName;
            public VRModuleDeviceClass deviceClass;
            public VRModuleDeviceModel deviceModel;
            public VRModuleInput2DType input2DType;

            public bool isConnected;
            public bool isPoseValid;
            public bool isOutOfRange;
            public bool isCalibrating;
            public bool isUninitialized;
            public Vector3 velocity;
            public Vector3 angularVelocity;
            public RigidPose pose;

            private ulong pressedFields;
            private ulong touchedFields;
            public List<string> pressed = new List<string>();
            public List<string> touched = new List<string>();

            public RawAxisValueArray axisValue = new RawAxisValueArray();

            public int jointCount;
            public JointEnumArray handJoints = new JointEnumArray();

            public void Update(uint index)
            {
                var deviceState = VRModule.GetDeviceState(index);

                deviceIndex = index;
                serialNumber = deviceState.serialNumber;
                modelNumber = deviceState.modelNumber;
                renderModelName = deviceState.renderModelName;
                deviceClass = deviceState.deviceClass;
                deviceModel = deviceState.deviceModel;
                input2DType = deviceState.input2DType;

                isConnected = deviceState.isConnected;
                isPoseValid = deviceState.isPoseValid;
                isOutOfRange = deviceState.isOutOfRange;
                isCalibrating = deviceState.isCalibrating;
                isUninitialized = deviceState.isUninitialized;

                velocity = deviceState.velocity;
                angularVelocity = deviceState.angularVelocity;
                pose = deviceState.pose;

                UpdateFieldString<VRModuleRawButton>(ref pressedFields, deviceState.buttonPressed, pressed);
                UpdateFieldString<VRModuleRawButton>(ref touchedFields, deviceState.buttonTouched, touched);

                foreach (VRModuleRawAxis i in axisValue.Enums)
                {
                    axisValue[i] = deviceState.GetAxisValue(i);
                }

                jointCount = 0;
                if (deviceState.readOnlyHandJoints != null)
                {
                    foreach (var p in deviceState.readOnlyHandJoints.EnumValues)
                    {
                        handJoints[p.Key] = p.Value;
                        if (p.Value.isValid) { jointCount++; }
                    }
                }
            }
        }

        [Serializable]
        public class ControllerStateDisplay
        {
            private ulong buttonPressedFields;
            public List<string> buttonPressed;

            public AxisValueArray axisValue = new AxisValueArray();

            public void Update(ViveRoleProperty role)
            {
                UpdateFieldString<ControllerButton>(ref buttonPressedFields, ViveInput.GetPress(role), buttonPressed);

                foreach (ControllerAxis i in axisValue.Enums)
                {
                    axisValue[i] = ViveInput.GetAxis(role, i);
                }
            }
        }

        [Serializable]
        public class HapticParameter
        {
            public float durationSeconds = 0.01f;
            public float frequency = 85f;
            public float amplitude = 0.125f;
            public float startSecondsFromNow = 0f;
        }

        [Serializable]
        public class LegacyHapticParameter
        {
            public ushort durationMicroSec = 500;
        }

        public ViveRoleProperty viveRole = ViveRoleProperty.New(HandRole.RightHand);

        [Space]
        public DeviceStateDisplay deviceState = new DeviceStateDisplay();

        [Space]
        public ControllerStateDisplay controllerState = new ControllerStateDisplay();

        [Space]
        public bool sendHaptics;
        public HapticParameter hapticParam = new HapticParameter();

        [Space]
        public bool sendLegacyHaptics;
        public LegacyHapticParameter legacyHapticParam = new LegacyHapticParameter();

        public void Update()
        {
            deviceState.Update(viveRole.GetDeviceIndex());
            controllerState.Update(viveRole);

            if (sendHaptics)
            {
                sendHaptics = false;
                ViveInput.TriggerHapticVibration(viveRole, hapticParam.durationSeconds, hapticParam.frequency, hapticParam.amplitude, hapticParam.startSecondsFromNow);
            }

            if (sendLegacyHaptics)
            {
                sendLegacyHaptics = false;
                ViveInput.TriggerHapticPulse(viveRole, legacyHapticParam.durationMicroSec);
            }
        }

        private static void UpdateFieldString<TEnum>(ref ulong prev, ulong curr, List<string> setNames)
#if CSHARP_7_OR_LATER
            where TEnum : Enum
#endif
        {
            if (curr != prev)
            {
                var enumInfo = EnumUtils.GetDisplayInfo(typeof(TEnum));
                for (int i = EnumArrayBase<TEnum>.StaticMinInt, imax = EnumArrayBase<TEnum>.StaticMaxInt; i <= imax; ++i)
                {
                    if (EnumArrayBase<TEnum>.StaticIsValidIndex(i))
                    {
                        var mask = 1ul << i;
                        var wasSet = (prev & mask) > 0ul;
                        var nowSet = (curr & mask) > 0ul;
                        if (!wasSet)
                        {
                            if (nowSet)
                            {
                                setNames.Add(GetDisplayName<TEnum>(enumInfo, i));
                            }
                        }
                        else
                        {
                            if (!nowSet)
                            {
                                setNames.Remove(GetDisplayName<TEnum>(enumInfo, i));
                            }
                        }
                    }
                }

                prev = curr;
            }
        }

        private static string GetDisplayName<TEnum>(EnumUtils.EnumDisplayInfo info, int i)
#if CSHARP_7_OR_LATER
            where TEnum : Enum
#endif
        {
            int displayIndex;
            if (info.value2displayedIndex.TryGetValue(i, out displayIndex))
            {
                return info.displayedNames[displayIndex];
            }
            else
            {
                return EnumArrayBase<TEnum>.I2E(i).ToString();
            }
        }
    }
}