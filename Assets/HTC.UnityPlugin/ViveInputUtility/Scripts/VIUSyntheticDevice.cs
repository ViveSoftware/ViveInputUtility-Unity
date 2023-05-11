//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#if ENABLE_INPUT_SYSTEM
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using TrackingState = UnityEngine.XR.InputTrackingState;
#if VIU_UIS_POSE_CONTROL
using UnityPoseControl = UnityEngine.InputSystem.XR.PoseControl;
#endif
#if VIU_OPENXR_PLUGIN_POSE_CONTROL
using OpenXRPoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif

namespace HTC.UnityPlugin.Vive
{
    public struct VIUSyntheticDeviceState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('V', 'I', 'U', 'D');

        [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
        public struct PoseState : IInputStateTypeInfo
        {
            internal const int kSizeInBytes = 60;
            public FourCC format => new FourCC('P', 'o', 's', 'e');
            [FieldOffset(0), InputControl(displayName = "Is Tracked", layout = "Button")]
            public bool isTracked;
            [FieldOffset(4), InputControl(displayName = "Tracking State", layout = "Integer")]
            public TrackingState trackingState;
            [FieldOffset(8), InputControl(displayName = "Position", noisy = true)]
            public Vector3 position;
            [FieldOffset(20), InputControl(displayName = "Rotation", noisy = true)]
            public Quaternion rotation;
            [FieldOffset(36), InputControl(displayName = "Velocity", noisy = true)]
            public Vector3 velocity;
            [FieldOffset(48), InputControl(displayName = "Angular Velocity", noisy = true)]
            public Vector3 angularVelocity;
        }

        [InputControl(name = "pose", layout = "Pose")]
        public PoseState pose;

        /// align <see cref="ControllerButton"/>
        [InputControl(name = "System", layout = "Button", bit = (uint)ControllerButton.System)]
        [InputControl(name = "Menu", layout = "Button", bit = (uint)ControllerButton.Menu, aliases = new[] { "BKey", "OuterFaceButton" }, usage = "Menu")]
        [InputControl(name = "MenuTouch", layout = "Button", bit = (uint)ControllerButton.MenuTouch, aliases = new[] { "BkeyTouch", "OuterFaceButtonTouch" })]
        [InputControl(name = "Trigger", layout = "Button", bit = (uint)ControllerButton.Trigger, aliases = new[] { "Axis1" }, usages = new[] { "PrimaryTrigger", "PrimaryAction", "Submit" })]
        [InputControl(name = "TriggerTouch", layout = "Button", bit = (uint)ControllerButton.TriggerTouch, aliases = new[] { "Axis1Touch" })]
        [InputControl(name = "Pad", layout = "Button", bit = (uint)ControllerButton.Pad, aliases = new[] { "Axis0" }, usage = "SecondaryAction")]
        [InputControl(name = "PadTouch", layout = "Button", bit = (uint)ControllerButton.PadTouch, aliases = new[] { "Axis0Touch" })]
        [InputControl(name = "Joystick", layout = "Button", bit = (uint)ControllerButton.Joystick)]
        [InputControl(name = "JoystickTouch", layout = "Button", bit = (uint)ControllerButton.JoystickTouch)]
        [InputControl(name = "Grip", layout = "Button", bit = (uint)ControllerButton.Grip, usage = "Cancel")]
        [InputControl(name = "GripTouch", layout = "Button", bit = (uint)ControllerButton.GripTouch)]
        [InputControl(name = "CapSenseGrip", layout = "Button", bit = (uint)ControllerButton.CapSenseGrip, aliases = new[] { "Axis2" })]
        [InputControl(name = "CapSenseGripTouch", layout = "Button", bit = (uint)ControllerButton.CapSenseGripTouch, aliases = new[] { "Axis2Touch" })]
        [InputControl(name = "ProximitySensor", layout = "Button", bit = (uint)ControllerButton.ProximitySensor)]
        [InputControl(name = "Bumper", layout = "Button", bit = (uint)ControllerButton.Bumper, aliases = new[] { "Axis3" })]
        [InputControl(name = "BumperTouch", layout = "Button", bit = (uint)ControllerButton.BumperTouch, aliases = new[] { "Axis3Touch" })]
        [InputControl(name = "AKey", layout = "Button", bit = (uint)ControllerButton.AKey, aliases = new[] { "InnerFaceButton" })]
        [InputControl(name = "AKeyTouch", layout = "Button", bit = (uint)ControllerButton.AKeyTouch, aliases = new[] { "InnerFaceButtonTouch" })]
        [InputControl(name = "Axis4", layout = "Button", bit = (uint)ControllerButton.Axis4)]
        [InputControl(name = "Axis4Touch", layout = "Button", bit = (uint)ControllerButton.Axis4Touch)]
        [InputControl(name = "HairTrigger", layout = "Button", bit = (uint)ControllerButton.HairTrigger)]
        [InputControl(name = "FullTrigger", layout = "Button", bit = (uint)ControllerButton.FullTrigger)]
        [InputControl(name = "DPadLeft", layout = "Button", bit = (uint)ControllerButton.DPadLeft, usage = "Back")]
        [InputControl(name = "DPadUp", layout = "Button", bit = (uint)ControllerButton.DPadUp)]
        [InputControl(name = "DPadRight", layout = "Button", bit = (uint)ControllerButton.DPadRight, usage = "Forward")]
        [InputControl(name = "DPadDown", layout = "Button", bit = (uint)ControllerButton.DPadDown)]
        [InputControl(name = "DPadLeftTouch", layout = "Button", bit = (uint)ControllerButton.DPadLeftTouch)]
        [InputControl(name = "DPadUpTouch", layout = "Button", bit = (uint)ControllerButton.DPadUpTouch)]
        [InputControl(name = "DPadRightTouch", layout = "Button", bit = (uint)ControllerButton.DPadRightTouch)]
        [InputControl(name = "DPadDownTouch", layout = "Button", bit = (uint)ControllerButton.DPadDownTouch)]
        [InputControl(name = "DPadUpperLeft", layout = "Button", bit = (uint)ControllerButton.DPadUpperLeft)]
        [InputControl(name = "DPadUpperRight", layout = "Button", bit = (uint)ControllerButton.DPadUpperRight)]
        [InputControl(name = "DPadLowerRight", layout = "Button", bit = (uint)ControllerButton.DPadLowerRight)]
        [InputControl(name = "DPadLowerLeft", layout = "Button", bit = (uint)ControllerButton.DPadLowerLeft)]
        [InputControl(name = "DPadUpperLeftTouch", layout = "Button", bit = (uint)ControllerButton.DPadUpperLeftTouch)]
        [InputControl(name = "DPadUpperRightTouch", layout = "Button", bit = (uint)ControllerButton.DPadUpperRightTouch)]
        [InputControl(name = "DPadLowerRightTouch", layout = "Button", bit = (uint)ControllerButton.DPadLowerRightTouch)]
        [InputControl(name = "DPadLowerLeftTouch", layout = "Button", bit = (uint)ControllerButton.DPadLowerLeftTouch)]
        [InputControl(name = "DPadCenter", layout = "Button", bit = (uint)ControllerButton.DPadCenter)]
        [InputControl(name = "DPadCenterTouch", layout = "Button", bit = (uint)ControllerButton.DPadCenterTouch)]
        [InputControl(name = "IndexPinch", layout = "Button", bit = (uint)ControllerButton.IndexPinch)]
        [InputControl(name = "MiddlePinch", layout = "Button", bit = (uint)ControllerButton.MiddlePinch)]
        [InputControl(name = "RingPinch", layout = "Button", bit = (uint)ControllerButton.RingPinch)]
        [InputControl(name = "PinkyPinch", layout = "Button", bit = (uint)ControllerButton.PinkyPinch)]
        [InputControl(name = "Fist", layout = "Button", bit = (uint)ControllerButton.Fist)]
        [InputControl(name = "Five", layout = "Button", bit = (uint)ControllerButton.Five)]
        [InputControl(name = "Ok", layout = "Button", bit = (uint)ControllerButton.Ok)]
        [InputControl(name = "ThumbUp", layout = "Button", bit = (uint)ControllerButton.ThumbUp)]
        [InputControl(name = "IndexUp", layout = "Button", bit = (uint)ControllerButton.IndexUp)]
        public ulong buttons;

        /// align <see cref="ControllerAxis"/>
        [InputControl(name = "PadAxis", layout = "Stick", usage = "Primary2DMotion")]
        [InputControl(name = "PadAxis/x", layout = "Axis", usages = new[] { "Horizontal", "ScrollHorizontal" })]
        [InputControl(name = "PadAxis/y", layout = "Axis", usages = new[] { "Vertical", "ScrollVertical" })]
        public Vector2 touchpad;
        [InputControl(name = "TriggerAxis", layout = "Axis")]
        public float trigger;
        [InputControl(name = "CapSenseGripAxis", layout = "Axis")]
        public float capSenseGrip;
        [InputControl(name = "IndexCurlAxis", layout = "Axis")]
        public float indexCurl;
        [InputControl(name = "MiddleCurlAxis", layout = "Axis")]
        public float middleCurl;
        [InputControl(name = "RingCurlAxis", layout = "Axis")]
        public float ringCurl;
        [InputControl(name = "PinkyCurlAxis", layout = "Axis")]
        public float pinkyCurl;
        [InputControl(name = "JoystickAxis", layout = "Stick", usage = "Secondary2DMotion")]
        [InputControl(name = "JoystickAxis/x", layout = "Axis", usages = new[] { "Horizontal", "ScrollHorizontal" })]
        [InputControl(name = "JoystickAxis/y", layout = "Axis", usages = new[] { "Vertical", "ScrollVertical" })]
        public Vector2 joystick;
        [InputControl(name = "IndexPinchAxis", layout = "Axis")]
        public float indexPinch;
        [InputControl(name = "MiddlePinchAxis", layout = "Axis")]
        public float middlePinch;
        [InputControl(name = "RingPinchAxis", layout = "Axis")]
        public float ringPinch;
        [InputControl(name = "PinkyPinchAxis", layout = "Axis")]
        public float pinkyPinch;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [InputControlLayout(displayName = "VIU Synthetic Device", stateType = typeof(VIUSyntheticDeviceState), isGenericTypeOfDevice = true)]
    public class VIUSyntheticDevice : InputDevice, IInputUpdateCallbackReceiver
    {
        [Serializable]
        private struct LayoutStruct
        {
            public string name;
            public string displayName;
            public string extend;
            public string[] extendMultiple;
            public string[] commonUsages;
        }

        private class RoleDeviceCreator : IDisposable
        {
            private readonly string layoutName;
            private readonly int minRoleValue;
            private readonly ViveRole.IMap map;
            private VIUSyntheticDevice[] devices;

            private const RegexOptions REGEX_OPTIONS = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;
            private static readonly Regex leftRgx = new Regex("^.*(left).*$", REGEX_OPTIONS);
            private static readonly Regex rightRgx = new Regex("^.*(right).*$", REGEX_OPTIONS);

            public RoleDeviceCreator(ViveRole.IMap map, string layoutName)
            {
                var roleInfo = map.RoleValueInfo;
                devices = new VIUSyntheticDevice[roleInfo.ValidRoleLength];
                this.layoutName = layoutName;
                this.minRoleValue = roleInfo.MinValidRoleValue;
                this.map = map;

                map.onRoleValueMappingChanged += OnRoleValueMappingChanged;

                for (int i = 0, imax = roleInfo.ValidRoleLength; i < imax; ++i)
                {
                    var roleValue = i + minRoleValue;
                    var deviceIndex = map.GetMappedDeviceByRoleValue(roleValue);
                    if (VRModule.IsValidDeviceIndex(deviceIndex))
                    {
                        devices[i] = AddDevice(map, roleValue);
                    }
                }
            }

            public void Dispose()
            {
                RemoveAllDevices();
                devices = null;
                map.onRoleValueMappingChanged -= OnRoleValueMappingChanged;
            }

            public void RemoveAllDevices()
            {
                for (int i = 0, imax = devices.Length; i < imax; ++i)
                {
                    if (devices[i] != null)
                    {
                        InputSystem.RemoveDevice(devices[i]);
                    }
                    devices[i] = null;
                }
            }

            public VIUSyntheticDevice GetDevice(int roleValue)
            {
                var roleIndex = roleValue - minRoleValue;
                return roleIndex >= 0 && roleIndex < devices.Length ? devices[roleIndex] : null;
            }

            private VIUSyntheticDevice AddDevice(ViveRole.IMap map, int roleValue)
            {
                VIUSyntheticDevice device;
                try
                {
                    device = InputSystem.AddDevice(new InputDeviceDescription()
                    {
                        interfaceName = layoutName,
                        manufacturer = "HTC ViveSoftware",
                    }) as VIUSyntheticDevice;

                    InputSystem.AddDeviceUsage(device, map.RoleValueInfo.GetNameByRoleValue(roleValue));
                    if (leftRgx.IsMatch(layoutName))
                    {
                        InputSystem.AddDeviceUsage(device, CommonUsages.LeftHand);
                    }
                    else if (rightRgx.IsMatch(layoutName))
                    {
                        InputSystem.AddDeviceUsage(device, CommonUsages.RightHand);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }
                device.ctrlState = ViveInput.GetState(map.RoleValueInfo.RoleEnumType, roleValue);
                return device;
            }

            private static void ResumeDevice(InputDevice device)
            {
                InputSystem.AddDevice(device);
            }

            private static void RemoveDevice(InputDevice device)
            {
                InputSystem.RemoveDevice(device);
            }

            private void OnRoleValueMappingChanged(ViveRole.IMap map, ViveRole.MappingChangedEventArg arg)
            {
                if (!VRModule.IsValidDeviceIndex(arg.previousDeviceIndex))
                {
                    if (VRModule.IsValidDeviceIndex(arg.currentDeviceIndex))
                    {
                        // try create or add into InputSystem
                        var roleIndex = arg.roleValue - minRoleValue;
                        var device = devices[roleIndex];
                        if (device == null)
                        {
                            devices[roleIndex] = AddDevice(map, arg.roleValue);
                        }
                        else
                        {
                            ResumeDevice(device);
                        }
                    }
                }
                else
                {
                    if (!VRModule.IsValidDeviceIndex(arg.currentDeviceIndex))
                    {
                        // try remove device from InputSystem
                        var roleIndex = arg.roleValue - minRoleValue;
                        var device = devices[roleIndex];
                        if (device != null)
                        {
                            RemoveDevice(device);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, RoleDeviceCreator> roleDeviceCreators = new Dictionary<string, RoleDeviceCreator>();
        private ViveInput.ICtrlState ctrlState;
        private EnumArray<ControllerButton, ButtonControl> _buttons;
        private EnumArray<ControllerAxis, AxisControl> _axises;

        public EnumArray<ControllerButton, ButtonControl>.IReadOnly buttons { get { return _buttons.ReadOnly; } }
        public EnumArray<ControllerAxis, AxisControl>.IReadOnly axises { get { return _axises.ReadOnly; } }
        public StickControl pad { get; private set; }
        public StickControl joystick { get; private set; }

#if VIU_UIS_POSE_CONTROL
        public UnityPoseControl pose { get; private set; }
#endif
#if VIU_OPENXR_PLUGIN_POSE_CONTROL
        public OpenXRPoseControl openxr_pose { get; private set; }
#endif
        public ButtonControl isTracked { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public Vector3Control position { get; private set; }
        public QuaternionControl rotation { get; private set; }
        public Vector3Control velocity { get; private set; }
        public Vector3Control angularVelocity { get; private set; }

        static VIUSyntheticDevice()
        {
            // RegisterLayout() adds a "Control layout" to the system.
            // These can be layouts for individual Controls (like sticks)
            // or layouts for entire Devices (which are themselves
            // Controls) like in our case.
            InputSystem.RegisterLayout<VIUSyntheticDevice>();

            // Duplicate layouts for each Vive role enum
            try
            {
                var currentAsm = typeof(ViveRole).Assembly;
                var currentAsmName = currentAsm.GetName().Name;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var referencingCurrentAsm = false;

                    if (asm == currentAsm)
                    {
                        referencingCurrentAsm = true;
                    }
                    else
                    {
                        foreach (var asmref in asm.GetReferencedAssemblies())
                        {
                            if (asmref.Name == currentAsmName)
                            {
                                referencingCurrentAsm = true;
                                break;
                            }
                        }
                    }

                    if (referencingCurrentAsm)
                    {
                        foreach (var type in asm.GetTypes().Where(t => ViveRoleEnum.ValidateViveRoleEnum(t) == ViveRoleEnumValidateResult.Valid))
                        {
                            TryRegisterLayoutForViveRoleEnum(type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                InputSystem.onLayoutChange += OnLayoutChange;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeInPlayer() { }

        /// <summary>
        /// Registry a new VIUSyntheticDevice Layout for a role so it can be shown in binding UI
        /// </summary>
        /// <param name="viveRoleType">A enum type defined with <see cref="ViveRoleEnumAttribute"/></param>
        /// <returns></returns>
        public static bool TryRegisterLayoutForViveRoleEnum(Type viveRoleType)
        {
            var map = ViveRole.GetMap(viveRoleType);
            var info = map == null ? null : map.RoleValueInfo;
            if (info == null || info.RoleValueNames.Length == 0) { return false; }

            var layoutName = InternalViveRoleLayoutName(viveRoleType);
            var validNames = ListPool<string>.Get();
            try
            {
                for (int i = 0, imax = info.RoleValueNames.Length; i < imax; ++i)
                {
                    if (info.IsValidRoleValue(info.RoleValues[i]))
                    {
                        validNames.Add(info.RoleValueNames[i]);
                    }
                }

                InputSystem.RegisterLayout(JsonUtility.ToJson(new LayoutStruct()
                {
                    name = layoutName,
                    displayName = viveRoleType.Name,
                    extend = typeof(VIUSyntheticDevice).Name,
                    commonUsages = validNames.ToArray(),
                }),
                layoutName,
                new InputDeviceMatcher()
                    .WithInterface(layoutName)
                    .WithManufacturer("HTC ViveSoftware"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                ListPool<string>.Release(validNames);
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                RoleDeviceCreator creator;
                if (!roleDeviceCreators.TryGetValue(layoutName, out creator))
                {
                    creator = new RoleDeviceCreator(map, layoutName);
                    roleDeviceCreators.Add(layoutName, creator);
                }
                else
                {
                    Debug.LogWarning("[VIUSyntheticDevice] TryRegisterLayoutForViveRoleEnum device creator for " + layoutName + " already exist.");
                }
            }

            return true;
        }

        private static void OnLayoutChange(string name, InputControlLayoutChange change)
        {
            if (change == InputControlLayoutChange.Removed)
            {
                RoleDeviceCreator creator;
                if (roleDeviceCreators.TryGetValue(name, out creator))
                {
                    creator.Dispose();
                    roleDeviceCreators.Remove(name);
                }
            }
        }

        private static string InternalViveRoleLayoutName(Type viveRoleType)
        {
            return "VIUSyntheticDeviceLayout" + viveRoleType.Name;
        }

        void IInputUpdateCallbackReceiver.OnUpdate()
        {
            if (ctrlState == null) { return; }
            var deviceIndex = ctrlState.RoleMap.GetMappedDeviceByRoleValue(ctrlState.RoleValue);
            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);

            ctrlState.Update();
            InputSystem.QueueStateEvent(this, new VIUSyntheticDeviceState()
            {
                buttons = ctrlState.CurrentButtonPressed,
                touchpad = new Vector2(ctrlState.GetAxis(ControllerAxis.PadX), ctrlState.GetAxis(ControllerAxis.PadY)),
                trigger = ctrlState.GetAxis(ControllerAxis.Trigger),
                capSenseGrip = ctrlState.GetAxis(ControllerAxis.CapSenseGrip),
                indexCurl = ctrlState.GetAxis(ControllerAxis.IndexCurl),
                middleCurl = ctrlState.GetAxis(ControllerAxis.MiddleCurl),
                ringCurl = ctrlState.GetAxis(ControllerAxis.RingCurl),
                pinkyCurl = ctrlState.GetAxis(ControllerAxis.PinkyCurl),
                joystick = new Vector2(ctrlState.GetAxis(ControllerAxis.JoystickX), ctrlState.GetAxis(ControllerAxis.JoystickY)),
                indexPinch = ctrlState.GetAxis(ControllerAxis.IndexPinch),
                middlePinch = ctrlState.GetAxis(ControllerAxis.MiddlePinch),
                ringPinch = ctrlState.GetAxis(ControllerAxis.RingPinch),
                pinkyPinch = ctrlState.GetAxis(ControllerAxis.PinkyPinch),
                pose = new VIUSyntheticDeviceState.PoseState()
                {
                    isTracked = deviceState.isPoseValid,
                    trackingState = TrackingState.Position | TrackingState.Rotation | TrackingState.Velocity | TrackingState.AngularVelocity,
                    position = deviceState.position,
                    rotation = deviceState.rotation,
                    velocity = deviceState.velocity,
                    angularVelocity = deviceState.angularVelocity,
                },
            });
        }

        // Query device by path
        public static VIUSyntheticDevice FindDeviceByRole<TRole>(TRole role)
#if CSHARP_7_OR_LATER
            where TRole : Enum
#endif
        {
            return InputSystem.FindControl("<" + InternalViveRoleLayoutName(typeof(TRole)) + ">{" + role.ToString() + "}") as VIUSyntheticDevice;
        }

        // Query device by path
        public static VIUSyntheticDevice FindDeviceByRole(Type roleEnumType, int roleValue)
        {
            var info = ViveRoleEnum.GetInfo(roleEnumType);
            if (info == null) { return null; }
            return InputSystem.FindControl("<" + InternalViveRoleLayoutName(roleEnumType) + ">{" + info.GetNameByRoleValue(roleValue) + "}") as VIUSyntheticDevice;
        }


        public static VIUSyntheticDevice GetCreatedDeviceByRole<TRole>(TRole role)
#if CSHARP_7_OR_LATER
            where TRole : Enum
#endif
        {
            var roleEnumType = typeof(TRole);
            if (ViveRoleEnum.ValidateViveRoleEnum(roleEnumType) == ViveRoleEnumValidateResult.Valid)
            {
                return InternalGetCreatedDeviceByRole(roleEnumType, EnumArrayBase<TRole>.E2I(role));
            }

            return null;
        }

        public static VIUSyntheticDevice GetCreatedDeviceByRole(Type roleEnumType, int roleValue)
        {
            if (ViveRoleEnum.ValidateViveRoleEnum(roleEnumType) == ViveRoleEnumValidateResult.Valid)
            {
                return InternalGetCreatedDeviceByRole(roleEnumType, roleValue);
            }

            return null;
        }

        public static VIUSyntheticDevice InternalGetCreatedDeviceByRole(Type roleEnumType, int roleValue)
        {
            var name = InternalViveRoleLayoutName(roleEnumType);

            RoleDeviceCreator creator;
            return roleDeviceCreators.TryGetValue(name, out creator) ? creator.GetDevice(roleValue) : null;
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();

#if VIU_UIS_POSE_CONTROL
            pose = GetChildControl("pose") as UnityPoseControl;
#endif
#if VIU_OPENXR_PLUGIN_POSE_CONTROL
            openxr_pose = GetChildControl("pose") as OpenXRPoseControl;
#endif
            isTracked = GetChildControl<ButtonControl>("pose/isTracked");
            trackingState = GetChildControl<IntegerControl>("pose/trackingState");
            position = GetChildControl<Vector3Control>("pose/position");
            rotation = GetChildControl<QuaternionControl>("pose/rotation");
            velocity = GetChildControl<Vector3Control>("pose/velocity");
            angularVelocity = GetChildControl<Vector3Control>("pose/angularVelocity");

            pad = GetChildControl<StickControl>("PadAxis");
            joystick = GetChildControl<StickControl>("JoystickAxis");

            _buttons = new EnumArray<ControllerButton, ButtonControl>();
            foreach (var btn in EnumArrayBase<ControllerButton>.StaticEnums)
            {
                _buttons[(int)btn] = TryGetChildControl<ButtonControl>(EnumArrayBase<ControllerButton>.StaticEnumName(btn));
            }

            _axises = new EnumArray<ControllerAxis, AxisControl>();
            foreach (var axis in EnumArrayBase<ControllerAxis>.StaticEnums)
            {
                _axises[(int)axis] = TryGetChildControl<AxisControl>(EnumArrayBase<ControllerAxis>.StaticEnumName(axis) + "Axis");
            }

            _axises[(int)ControllerAxis.PadX] = GetChildControl<AxisControl>("PadAxis/x");
            _axises[(int)ControllerAxis.PadY] = GetChildControl<AxisControl>("PadAxis/y");
            _axises[(int)ControllerAxis.JoystickX] = GetChildControl<AxisControl>("JoystickAxis/x");
            _axises[(int)ControllerAxis.JoystickY] = GetChildControl<AxisControl>("JoystickAxis/y");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct VIUSyntheticXRHMDState : IInputStateTypeInfo
    {
        public const int kSizeInBytes = 120;
        public FourCC format => new FourCC('V', 'I', 'U', 'H');
        [FieldOffset(0), InputControl(layout = "Button")]
        public bool isTracked;
        [FieldOffset(4), InputControl(layout = "Integer")]
        public TrackingState trackingState;
        [FieldOffset(8), InputControl(noisy = true)]
        public Vector3 devicePosition;
        [FieldOffset(20), InputControl(noisy = true)]
        public Quaternion deviceRotation;

        [FieldOffset(36), InputControl(noisy = true)]
        public Vector3 leftEyePosition;
        [FieldOffset(48), InputControl(noisy = true)]
        public Quaternion leftEyeRotation;
        [FieldOffset(64), InputControl(noisy = true)]
        public Vector3 rightEyePosition;
        [FieldOffset(76), InputControl(noisy = true)]
        public Quaternion rightEyeRotation;
        [FieldOffset(92), InputControl(noisy = true)]
        public Vector3 centerEyePosition;
        [FieldOffset(104), InputControl(noisy = true)]
        public Quaternion centerEyeRotation;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [InputControlLayout(displayName = "VIU Synthetic XR HMD", stateType = typeof(VIUSyntheticXRHMDState), hideInUI = true)]
    public class VIUSyntheticXRHMD : XRHMD, IInputUpdateCallbackReceiver
    {
        private ViveInput.ICtrlState<DeviceRole> ctrlState;
        private static VIUSyntheticXRHMD hmdDevice;
        private static readonly RigidPose leftEyeLocalPose = new RigidPose(new Vector3(-0.033f, 0f, 0f), Quaternion.identity);
        private static readonly RigidPose rightEyeLocalPose = new RigidPose(new Vector3(0.033f, 0f, 0f), Quaternion.identity);

        static VIUSyntheticXRHMD()
        {
            InputSystem.RegisterLayout<VIUSyntheticXRHMD>();

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                var roleMap = ViveRole.GetMap<DeviceRole>();

                HandleDevice(ref hmdDevice, VRModule.IsValidDeviceIndex(roleMap.GetMappedDeviceByRole(DeviceRole.Hmd)));

                roleMap.onRoleMappingChanged += (map, arg) =>
                {
                    if (arg.role == DeviceRole.Hmd)
                    {
                        HandleDevice(ref hmdDevice, VRModule.IsValidDeviceIndex(arg.currentDeviceIndex));
                    }
                };
            }
        }

        private static void HandleDevice(ref VIUSyntheticXRHMD device, bool connected)
        {
            try
            {
                if (connected)
                {
                    if (device == null)
                    {
                        device = InputSystem.AddDevice<VIUSyntheticXRHMD>("VIUSyntheticXRHMD");
                        device.ctrlState = ViveInput.GetState(DeviceRole.Hmd);
                    }
                    else
                    {
                        InputSystem.AddDevice(device);
                    }
                }
                else
                {
                    if (device != null)
                    {
                        InputSystem.RemoveDevice(device);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void IInputUpdateCallbackReceiver.OnUpdate()
        {
            if (ctrlState == null) { return; }

            var deviceIndex = ctrlState.RoleMap.GetMappedDeviceByRoleValue(ctrlState.RoleValue);
            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            var leftEyePose = deviceState.pose * leftEyeLocalPose;
            var rightEyePose = deviceState.pose * rightEyeLocalPose;
            InputSystem.QueueStateEvent(this, new VIUSyntheticXRHMDState()
            {
                isTracked = deviceState.isPoseValid,
                trackingState = TrackingState.Position | TrackingState.Rotation,
                devicePosition = deviceState.position,
                deviceRotation = deviceState.rotation,
                // TODO: should get eye pose from IPD property or eye tracking sdk
                leftEyePosition = leftEyePose.pos,
                leftEyeRotation = leftEyePose.rot,
                rightEyePosition = rightEyePose.pos,
                rightEyeRotation = rightEyePose.rot,
                centerEyePosition = deviceState.position,
                centerEyeRotation = deviceState.rotation,
            });
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSizeInBytes)]
    public struct VIUSyntheticXRControllerState : IInputStateTypeInfo
    {
        public const int kSizeInBytes = 36;
        public FourCC format => new FourCC('V', 'I', 'U', 'C');
        [FieldOffset(0), InputControl(layout = "Button")]
        public bool isTracked;
        [FieldOffset(4), InputControl(layout = "Integer")]
        public TrackingState trackingState;
        [FieldOffset(8), InputControl(noisy = true)]
        public Vector3 devicePosition;
        [FieldOffset(20), InputControl(noisy = true)]
        public Quaternion deviceRotation;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [InputControlLayout(displayName = "VIU Synthetic XR Controller", stateType = typeof(VIUSyntheticXRControllerState), hideInUI = true)]
    public class VIUSyntheticXRController : XRController, IInputUpdateCallbackReceiver
    {
        private ViveInput.ICtrlState<HandRole> ctrlState;

        private static VIUSyntheticXRController rightDevice;
        private static VIUSyntheticXRController leftDevice;

        static VIUSyntheticXRController()
        {
            InputSystem.RegisterLayout<VIUSyntheticXRController>();

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                var roleMap = ViveRole.GetMap<HandRole>();

                HandleDevice(ref rightDevice, HandRole.RightHand, CommonUsages.RightHand, VRModule.IsValidDeviceIndex(roleMap.GetMappedDeviceByRole(HandRole.RightHand)));
                HandleDevice(ref leftDevice, HandRole.LeftHand, CommonUsages.LeftHand, VRModule.IsValidDeviceIndex(roleMap.GetMappedDeviceByRole(HandRole.LeftHand)));

                roleMap.onRoleMappingChanged += (map, arg) =>
                {
                    if (arg.role == HandRole.RightHand)
                    {
                        HandleDevice(ref rightDevice, HandRole.RightHand, CommonUsages.RightHand, VRModule.IsValidDeviceIndex(arg.currentDeviceIndex));
                    }
                    else if (arg.role == HandRole.LeftHand)
                    {
                        HandleDevice(ref leftDevice, HandRole.LeftHand, CommonUsages.LeftHand, VRModule.IsValidDeviceIndex(arg.currentDeviceIndex));
                    }
                };
            }
        }

        private static void HandleDevice(ref VIUSyntheticXRController device, HandRole hand, InternedString handStr, bool connected)
        {
            try
            {
                if (connected)
                {
                    if (device == null)
                    {
                        device = InputSystem.AddDevice<VIUSyntheticXRController>("VIUSyntheticXRController" + hand.ToString());
                        InputSystem.AddDeviceUsage(device, handStr);
                        device.ctrlState = ViveInput.GetState(hand);
                    }
                    else
                    {
                        InputSystem.AddDevice(device);
                    }
                }
                else
                {
                    if (device != null)
                    {
                        InputSystem.RemoveDevice(device);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void IInputUpdateCallbackReceiver.OnUpdate()
        {
            if (ctrlState == null) { return; }

            var deviceIndex = ctrlState.RoleMap.GetMappedDeviceByRoleValue(ctrlState.RoleValue);
            var deviceState = VRModule.GetCurrentDeviceState(deviceIndex);
            InputSystem.QueueStateEvent(this, new VIUSyntheticXRControllerState()
            {
                isTracked = deviceState.isPoseValid,
                trackingState = TrackingState.Position | TrackingState.Rotation,
                devicePosition = deviceState.position,
                deviceRotation = deviceState.rotation,
            });
        }
    }
}
#endif