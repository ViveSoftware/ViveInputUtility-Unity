//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

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
        public static bool GetAnyPress<TRole>(TRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAnyPress(usePrevState ? GetState(role).PreviousButtonPressed : GetState(role).CurrentButtonPressed);
        }

        //public static bool GetAnyPressDown<TRole>(TRole role, ControllerButtonMask mask)
        //{
        //    var state = GetState(role);
        //    return !mask.GetAnyPress(state.PreviousButtonPressed) && mask.GetAnyPress(state.CurrentButtonPressed);
        //}

        //public static bool GetAnyPressUp<TRole>(TRole role, ControllerButtonMask mask)
        //{
        //    var state = GetState(role);
        //    return mask.GetAllPress(state.PreviousButtonPressed) && !mask.GetAllPress(state.CurrentButtonPressed);
        //}

        public static bool GetAllPress<TRole>(TRole role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAllPress(usePrevState ? GetState(role).PreviousButtonPressed : GetState(role).CurrentButtonPressed);
        }

        //public static bool GetAllPressDown<TRole>(TRole role, ControllerButtonMask mask)
        //{
        //    var state = GetState(role);
        //    return !mask.GetAllPress(state.PreviousButtonPressed) && mask.GetAllPress(state.CurrentButtonPressed);
        //}

        //public static bool GetAllPressUp<TRole>(TRole role, ControllerButtonMask mask)
        //{
        //    var state = GetState(role);
        //    return mask.GetAnyPress(state.PreviousButtonPressed) && !mask.GetAnyPress(state.CurrentButtonPressed);
        //}

        public static bool GetAnyPress(ViveRoleProperty role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAnyPress(role.roleType, role.roleValue, mask, usePrevState);
        }

        //public static bool GetAnyPressDown(ViveRoleProperty role, ControllerButtonMask mask)
        //{
        //    return GetAnyPressDown(role.roleType, role.roleValue, mask);
        //}

        //public static bool GetAnyPressUp(ViveRoleProperty role, ControllerButtonMask mask)
        //{
        //    return GetAnyPressUp(role.roleType, role.roleValue, mask);
        //}

        public static bool GetAllPress(ViveRoleProperty role, ControllerButtonMask mask, bool usePrevState = false)
        {
            return GetAllPress(role.roleType, role.roleValue, mask, usePrevState);
        }

        //public static bool GetAllPressDown(ViveRoleProperty role, ControllerButtonMask mask)
        //{
        //    return GetAllPressDown(role.roleType, role.roleValue, mask);
        //}

        //public static bool GetAllPressUp(ViveRoleProperty role, ControllerButtonMask mask)
        //{
        //    return GetAllPressUp(role.roleType, role.roleValue, mask);
        //}

        public static bool GetAnyPress(Type roleType, int roleValue, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAnyPress(usePrevState ? GetState(roleType, roleValue).PreviousButtonPressed : GetState(roleType, roleValue).CurrentButtonPressed);
        }

        //public static bool GetAnyPressDown(Type roleType, int roleValue, ControllerButtonMask mask)
        //{
        //    var state = GetState(roleType, roleValue);
        //    return !mask.GetAnyPress(state.PreviousButtonPressed) && mask.GetAnyPress(state.CurrentButtonPressed);
        //}

        //public static bool GetAnyPressUp(Type roleType, int roleValue, ControllerButtonMask mask)
        //{
        //    var state = GetState(roleType, roleValue);
        //    return mask.GetAllPress(state.PreviousButtonPressed) && !mask.GetAllPress(state.CurrentButtonPressed);
        //}

        public static bool GetAllPress(Type roleType, int roleValue, ControllerButtonMask mask, bool usePrevState = false)
        {
            return mask.GetAllPress(usePrevState ? GetState(roleType, roleValue).PreviousButtonPressed : GetState(roleType, roleValue).CurrentButtonPressed);
        }

        //public static bool GetAllPressDown(Type roleType, int roleValue, ControllerButtonMask mask)
        //{
        //    var state = GetState(roleType, roleValue);
        //    return !mask.GetAllPress(state.PreviousButtonPressed) && mask.GetAllPress(state.CurrentButtonPressed);
        //}

        //public static bool GetAllPressUp(Type roleType, int roleValue, ControllerButtonMask mask)
        //{
        //    var state = GetState(roleType, roleValue);
        //    return mask.GetAnyPress(state.PreviousButtonPressed) && !mask.GetAnyPress(state.CurrentButtonPressed);
        //}
    }
}
