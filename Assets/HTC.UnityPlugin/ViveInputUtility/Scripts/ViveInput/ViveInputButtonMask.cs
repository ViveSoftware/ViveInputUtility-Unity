//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;

namespace HTC.UnityPlugin.Vive
{
    [Serializable]
    public struct ControllerButtonMask
    {
        public ulong raw;

        public ControllerButtonMask(ControllerButton button, params ControllerButton[] buttons)
        {
            raw = GetRawMask(button, buttons);
        }

        public bool IsSet(ControllerButton button) { return button >= 0 ? (raw & (1ul << (int)button)) > 0ul : false; }

        public bool IsAnySet(ControllerButton button, params ControllerButton[] buttons) { var m = GetRawMask(button, buttons); return (raw & m) > 0ul; }

        public bool IsAllSet(ControllerButton button, params ControllerButton[] buttons) { var m = GetRawMask(button, buttons); return (raw & m) == m; }

        public void Set(ControllerButton button, params ControllerButton[] buttons) { raw |= GetRawMask(button, buttons); }

        public void Unset(ControllerButton button, params ControllerButton[] buttons) { raw &= ~GetRawMask(button, buttons); }

        public bool GetAnyPress(ulong pressed) { return (pressed & raw) > 0ul; }

        public bool GetAllPress(ulong pressed) { return (pressed & raw) == raw; }

        public static ulong GetRawMask(ControllerButton button, params ControllerButton[] buttons)
        {
            var value = button >= 0 ? 1ul << (int)button : 0ul;
            if (buttons != null && buttons.Length > 0) { foreach (var b in buttons) { if (b >= 0) { value |= 1ul << (int)b; } } }
            return value;
        }

        public static ControllerButtonMask All { get { return new ControllerButtonMask() { raw = ~0ul }; } }
    }

    public partial class ViveInput : SingletonBehaviour<ViveInput>
    {
        public static bool GetAnyPress(HandRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAnyPressEx(role, mask, usePrevState);
        }

        public static bool GetAllPress(HandRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAllPressEx(role, mask, usePrevState);
        }

        public static bool GetAnyPressEx<TRole>(TRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAnyPress(GetPressEx(role, usePrevState));
        }

        public static bool GetAllPressEx<TRole>(TRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAllPress(GetPressEx(role, usePrevState));
        }

        public static bool GetAnyPress(ViveRoleProperty role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAnyPress(role.roleType, role.roleValue, mask, usePrevState);
        }

        public static bool GetAllPress(ViveRoleProperty role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAllPress(role.roleType, role.roleValue, mask, usePrevState);
        }

        public static bool GetAnyPress(Type roleType, int roleValue, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAnyPress(GetPressEx(roleType, roleValue, usePrevState));
        }

        public static bool GetAllPress(Type roleType, int roleValue, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAllPress(GetPressEx(roleType, roleValue, usePrevState));
        }
    }
}
