//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// To provide static APIs to retrieve controller's button status
    /// </summary>
    [DisallowMultipleComponent]
    public partial class ViveInput : MonoBehaviour
    {
        #region origin
        /// <summary>
        /// Returns true while the button on the controller identified by role is held down
        /// </summary>
        public static bool GetPress(HandRole role, ControllerButton button)
        {
            return GetPressEx(role, button);
        }

        /// <summary>
        /// Returns true during the frame the user pressed down the button on the controller identified by role
        /// </summary>
        public static bool GetPressDown(HandRole role, ControllerButton button)
        {
            return GetPressDownEx(role, button);
        }

        /// <summary>
        /// Returns true during the frame the user releases the button on the controller identified by role
        /// </summary>
        public static bool GetPressUp(HandRole role, ControllerButton button)
        {
            return GetPressUpEx(role, button);
        }

        /// <summary>
        /// Returns time of the last frame that user pressed down the button on the controller identified by role
        /// </summary>
        public static float LastPressDownTime(HandRole role, ControllerButton button)
        {
            return LastPressDownTimeEx(role, button);
        }

        /// <summary>
        /// Return amount of clicks in a row for the button on the controller identified by role
        /// Set ViveInput.clickInterval to configure click interval
        /// </summary>
        public static int ClickCount(HandRole role, ControllerButton button)
        {
            return ClickCountEx(role, button);
        }

        /// <summary>
        /// Returns raw analog value of the trigger button on the controller identified by role
        /// </summary>
        public static float GetTriggerValue(HandRole role, bool usePrevState = false)
        {
            return GetTriggerValueEx(role, usePrevState);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad  on the controller identified by role
        /// </summary>
        public static Vector2 GetPadAxis(HandRole role, bool usePrevState = false)
        {
            return GetPadAxisEx(role, usePrevState);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad on the controller identified by role if pressed,
        /// otherwise, returns Vector2.zero
        /// </summary>
        public static Vector2 GetPadPressAxis(HandRole role)
        {
            return GetPadPressAxisEx(role);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad on the controller identified by role if touched,
        /// otherwise, returns Vector2.zero
        /// </summary>
        public static Vector2 GetPadTouchAxis(HandRole role)
        {
            return GetPadTouchAxisEx(role);
        }

        public static Vector2 GetPadPressVector(HandRole role)
        {
            return GetPadPressVectorEx(role);
        }

        public static Vector2 GetPadTouchVector(HandRole role)
        {
            return GetPadTouchVectorEx(role);
        }

        public static Vector2 GetPadPressDelta(HandRole role)
        {
            return GetPadPressDeltaEx(role);
        }

        public static Vector2 GetPadTouchDelta(HandRole role)
        {
            return GetPadTouchDeltaEx(role);
        }

        /// <summary>
        /// Add press handler for the button on the controller identified by role
        /// </summary>
        public static void AddPress(HandRole role, ControllerButton button, Action callback)
        {
            AddListenerEx(role, button, ButtonEventType.Press, callback);
        }

        /// <summary>
        /// Add press down handler for the button on the controller identified by role
        /// </summary>
        public static void AddPressDown(HandRole role, ControllerButton button, Action callback)
        {
            AddListenerEx(role, button, ButtonEventType.Down, callback);
        }

        /// <summary>
        /// Add press up handler for the button on the controller identified by role
        /// </summary>
        public static void AddPressUp(HandRole role, ControllerButton button, Action callback)
        {
            AddListenerEx(role, button, ButtonEventType.Up, callback);
        }

        /// <summary>
        /// Add click handler for the button on the controller identified by role
        /// Use ViveInput.ClickCount to get click count
        /// </summary>
        public static void AddClick(HandRole role, ControllerButton button, Action callback)
        {
            AddListenerEx(role, button, ButtonEventType.Click, callback);
        }

        /// <summary>
        /// Remove press handler for the button on the controller identified by role
        /// </summary>
        public static void RemovePress(HandRole role, ControllerButton button, Action callback)
        {
            RemoveListenerEx(role, button, ButtonEventType.Press, callback);
        }

        /// <summary>
        /// Remove press down handler for the button on the controller identified by role
        /// </summary>
        public static void RemovePressDown(HandRole role, ControllerButton button, Action callback)
        {
            RemoveListenerEx(role, button, ButtonEventType.Down, callback);
        }

        /// <summary>
        /// Remove press up handler for the button on the controller identified by role
        /// </summary>
        public static void RemovePressUp(HandRole role, ControllerButton button, Action callback)
        {
            RemoveListenerEx(role, button, ButtonEventType.Up, callback);
        }

        /// <summary>
        /// Remove click handler for the button on the controller identified by role
        /// </summary>
        public static void RemoveClick(HandRole role, ControllerButton button, Action callback)
        {
            RemoveListenerEx(role, button, ButtonEventType.Click, callback);
        }

        /// <summary>
        /// Trigger vibration of the controller identified by role
        /// </summary>
        public static void TriggerHapticPulse(HandRole role, ushort durationMicroSec = 500)
        {
            TriggerHapticPulseEx(role, durationMicroSec);
        }

        public static VRControllerState_t GetCurrentRawControllerState(HandRole role)
        {
            return GetCurrentRawControllerStateEx(role);
        }

        public static VRControllerState_t GetPreviousRawControllerState(HandRole role)
        {
            return GetPreviousRawControllerStateEx(role);
        }
        #endregion origin

        #region extend generic
        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressEx<TRole>(TRole role, ControllerButton button)
        {
            return GetState(role).GetPress(button);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressDownEx<TRole>(TRole role, ControllerButton button)
        {
            return GetState(role).GetPressDown(button);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressUpEx<TRole>(TRole role, ControllerButton button)
        {
            return GetState(role).GetPressUp(button);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static float LastPressDownTimeEx<TRole>(TRole role, ControllerButton button)
        {
            return GetState(role).LastPressDownTime(button);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static int ClickCountEx<TRole>(TRole role, ControllerButton button)
        {
            return GetState(role).ClickCount(button);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static float GetTriggerValueEx<TRole>(TRole role, bool usePrevState = false)
        {
            return GetState(role).GetTriggerValue(usePrevState);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadAxisEx<TRole>(TRole role, bool usePrevState = false)
        {
            return GetState(role).GetAxis(usePrevState);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressAxisEx<TRole>(TRole role)
        {
            var handState = GetState(role);
            return handState.GetPress(ControllerButton.Pad) ? handState.GetAxis() : Vector2.zero;
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchAxisEx<TRole>(TRole role)
        {
            var handState = GetState(role);
            return handState.GetPress(ControllerButton.PadTouch) ? handState.GetAxis() : Vector2.zero;
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressVectorEx<TRole>(TRole role)
        {
            return GetState(role).GetPadPressVector();
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchVectorEx<TRole>(TRole role)
        {
            return GetState(role).GetPadTouchVector();
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressDeltaEx<TRole>(TRole role)
        {
            var handState = GetState(role);
            if (handState.GetPress(ControllerButton.Pad) && !handState.GetPressDown(ControllerButton.Pad))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchDeltaEx<TRole>(TRole role)
        {
            var handState = GetState(role);
            if (handState.GetPress(ControllerButton.PadTouch) && !handState.GetPressDown(ControllerButton.PadTouch))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void AddListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            GetState(role).AddListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void RemoveListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            GetState(role).RemoveListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void AddListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            GetState(role).AddListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void RemoveListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            GetState(role).RemoveListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void AddListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, RoleEventListener<TRole> callback)
        {
            GetState(role).AddListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void RemoveListenerEx<TRole>(TRole role, ControllerButton button, ButtonEventType eventType, RoleEventListener<TRole> callback)
        {
            GetState(role).RemoveListener(button, callback, eventType);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void TriggerHapticPulseEx<TRole>(TRole role, ushort durationMicroSec = 500)
        {
            var system = OpenVR.System;
            if (system != null)
            {
                system.TriggerHapticPulse(ViveRole.GetDeviceIndexEx(role), (uint)EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)EVRButtonId.k_EButton_Axis0, (char)durationMicroSec);
            }
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static VRControllerState_t GetCurrentRawControllerStateEx<TRole>(TRole role)
        {
            return GetState(role).GetCurrentRawState();
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static VRControllerState_t GetPreviousRawControllerStateEx<TRole>(TRole role)
        {
            return GetState(role).GetPreviousRawState();
        }
        #endregion extend generic

        #region extend general
        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).GetPress(button);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressDownEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).GetPressDown(button);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressUpEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).GetPressUp(button);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static float LastPressDownTimeEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).LastPressDownTime(button);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static int ClickCountEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).ClickCount(button);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static float GetTriggerValueEx(Type roleType, int roleValue, bool usePrevState = false)
        {
            return GetState(roleType, roleValue).GetTriggerValue(usePrevState);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadAxisEx(Type roleType, int roleValue, bool usePrevState = false)
        {
            return GetState(roleType, roleValue).GetAxis(usePrevState);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressAxisEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            return handState.GetPress(ControllerButton.Pad) ? handState.GetAxis() : Vector2.zero;
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchAxisEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            return handState.GetPress(ControllerButton.PadTouch) ? handState.GetAxis() : Vector2.zero;
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressVectorEx(Type roleType, int roleValue)
        {
            return GetState(roleType, roleValue).GetPadPressVector();
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchVectorEx(Type roleType, int roleValue)
        {
            return GetState(roleType, roleValue).GetPadTouchVector();
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressDeltaEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            if (handState.GetPress(ControllerButton.Pad) && !handState.GetPressDown(ControllerButton.Pad))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchDeltaEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            if (handState.GetPress(ControllerButton.PadTouch) && !handState.GetPressDown(ControllerButton.PadTouch))
            {
                return handState.GetAxis() - handState.GetAxis(true);
            }
            return Vector2.zero;
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void AddListenerEx(Type roleType, int roleValue, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            GetState(roleType, roleValue).AddListener(button, callback, eventType);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void RemoveListenerEx(Type roleType, int roleValue, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            GetState(roleType, roleValue).RemoveListener(button, callback, eventType);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void AddListenerEx(Type roleType, int roleValue, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            GetState(roleType, roleValue).AddListener(button, callback, eventType);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void RemoveListenerEx(Type roleType, int roleValue, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            GetState(roleType, roleValue).RemoveListener(button, callback, eventType);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void TriggerHapticPulseEx(Type roleType, int roleValue, ushort durationMicroSec = 500)
        {
            var system = OpenVR.System;
            if (system != null)
            {
                system.TriggerHapticPulse(ViveRole.GetDeviceIndexEx(roleType, roleValue), (uint)EVRButtonId.k_EButton_SteamVR_Touchpad - (uint)EVRButtonId.k_EButton_Axis0, (char)durationMicroSec);
            }
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static VRControllerState_t GetCurrentRawControllerStateEx(Type roleType, int roleValue)
        {
            return GetState(roleType, roleValue).GetCurrentRawState();
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static VRControllerState_t GetPreviousRawControllerStateEx(Type roleType, int roleValue)
        {
            return GetState(roleType, roleValue).GetPreviousRawState();
        }
        #endregion extend general
    }
}