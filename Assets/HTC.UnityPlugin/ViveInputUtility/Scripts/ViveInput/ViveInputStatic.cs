//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// To provide static APIs to retrieve controller's button status
    /// </summary>
    [DisallowMultipleComponent]
    public partial class ViveInput : SingletonBehaviour<ViveInput>
    {
        #region origin
        /// <summary>
        /// Returns true while the button on the controller identified by role is held down
        /// </summary>
        public static bool GetPress(HandRole role, ControllerButton button)
        {
            return GetPressEx(role, button);
        }

        public static ulong GetPress(HandRole role, bool usePrevState = false)
        {
            return usePrevState ? GetState(role).PreviousButtonPressed : GetState(role).CurrentButtonPressed;
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

        public static float GetAxis(HandRole role, ControllerAxis axis, bool usePrevState = false)
        {
            return GetAxisEx(role, axis, usePrevState);
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

        public static Vector2 GetScrollDelta(HandRole role, ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY)
        {
            return GetScrollDeltaEx(role, scrollType, scale, xAxis, yAxis);
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

        /// <summary>
        /// Trigger vibration of the controller identified by role
        /// </summary>
        public static void TriggerHapticVibration(HandRole role, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            TriggerHapticVibrationEx(role, durationSeconds, frequency, amplitude, startSecondsFromNow);
        }
        #endregion origin

        #region general role property
        /// <summary>
        /// Returns true while the button on the controller identified by role is held down
        /// </summary>
        public static bool GetPress(ViveRoleProperty role, ControllerButton button)
        {
            return GetPressEx(role.roleType, role.roleValue, button);
        }

        public static ulong GetPress(ViveRoleProperty role, bool usePrevState = false)
        {
            return usePrevState ? GetState(role.roleType, role.roleValue).PreviousButtonPressed : GetState(role.roleType, role.roleValue).CurrentButtonPressed;
        }

        /// <summary>
        /// Returns true during the frame the user pressed down the button on the controller identified by role
        /// </summary>
        public static bool GetPressDown(ViveRoleProperty role, ControllerButton button)
        {
            return GetPressDownEx(role.roleType, role.roleValue, button);
        }

        /// <summary>
        /// Returns true during the frame the user releases the button on the controller identified by role
        /// </summary>
        public static bool GetPressUp(ViveRoleProperty role, ControllerButton button)
        {
            return GetPressUpEx(role.roleType, role.roleValue, button);
        }

        /// <summary>
        /// Returns time of the last frame that user pressed down the button on the controller identified by role
        /// </summary>
        public static float LastPressDownTime(ViveRoleProperty role, ControllerButton button)
        {
            return LastPressDownTimeEx(role.roleType, role.roleValue, button);
        }

        /// <summary>
        /// Return amount of clicks in a row for the button on the controller identified by role
        /// Set ViveInput.clickInterval to configure click interval
        /// </summary>
        public static int ClickCount(ViveRoleProperty role, ControllerButton button)
        {
            return ClickCountEx(role.roleType, role.roleValue, button);
        }

        public static float GetAxis(ViveRoleProperty role, ControllerAxis axis, bool usePrevState = false)
        {
            return GetAxisEx(role.roleType, role.roleValue, axis, usePrevState);
        }

        /// <summary>
        /// Returns raw analog value of the trigger button on the controller identified by role
        /// </summary>
        public static float GetTriggerValue(ViveRoleProperty role, bool usePrevState = false)
        {
            return GetTriggerValueEx(role.roleType, role.roleValue, usePrevState);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad  on the controller identified by role
        /// </summary>
        public static Vector2 GetPadAxis(ViveRoleProperty role, bool usePrevState = false)
        {
            return GetPadAxisEx(role.roleType, role.roleValue, usePrevState);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad on the controller identified by role if pressed,
        /// otherwise, returns Vector2.zero
        /// </summary>
        public static Vector2 GetPadPressAxis(ViveRoleProperty role)
        {
            return GetPadPressAxisEx(role.roleType, role.roleValue);
        }

        /// <summary>
        /// Returns raw analog value of the touch pad on the controller identified by role if touched,
        /// otherwise, returns Vector2.zero
        /// </summary>
        public static Vector2 GetPadTouchAxis(ViveRoleProperty role)
        {
            return GetPadTouchAxisEx(role.roleType, role.roleValue);
        }

        public static Vector2 GetPadPressVector(ViveRoleProperty role)
        {
            return GetPadPressVectorEx(role.roleType, role.roleValue);
        }

        public static Vector2 GetPadTouchVector(ViveRoleProperty role)
        {
            return GetPadTouchVectorEx(role.roleType, role.roleValue);
        }

        public static Vector2 GetPadPressDelta(ViveRoleProperty role)
        {
            return GetPadPressDeltaEx(role.roleType, role.roleValue);
        }

        public static Vector2 GetPadTouchDelta(ViveRoleProperty role)
        {
            return GetPadTouchDeltaEx(role.roleType, role.roleValue);
        }

        public static Vector2 GetScrollDelta(ViveRoleProperty role, ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY)
        {
            return GetScrollDeltaEx(role.roleType, role.roleValue, scrollType, scale, xAxis, yAxis);
        }

        public static void AddListener(ViveRoleProperty role, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            AddListenerEx(role.roleType, role.roleValue, button, eventType, callback);
        }

        public static void RemoveListener(ViveRoleProperty role, ControllerButton button, ButtonEventType eventType, Action callback)
        {
            RemoveListenerEx(role.roleType, role.roleValue, button, eventType, callback);
        }

        public static void AddListener(ViveRoleProperty role, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            AddListenerEx(role.roleType, role.roleValue, button, eventType, callback);
        }

        public static void RemoveListener(ViveRoleProperty role, ControllerButton button, ButtonEventType eventType, RoleValueEventListener callback)
        {
            RemoveListenerEx(role.roleType, role.roleValue, button, eventType, callback);
        }

        /// <summary>
        /// Trigger vibration of the controller identified by role
        /// </summary>
        public static void TriggerHapticPulse(ViveRoleProperty role, ushort durationMicroSec = 500)
        {
            TriggerHapticPulseEx(role.roleType, role.roleValue, durationMicroSec);
        }

        /// <summary>
        /// Trigger vibration of the controller identified by role
        /// </summary>
        public static void TriggerHapticVibration(ViveRoleProperty role, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            TriggerHapticVibrationEx(role.roleType, role.roleValue, durationSeconds, frequency, amplitude, startSecondsFromNow);
        }
        #endregion

        #region extend generic role
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

        public static ulong GetPressEx<TRole>(TRole role, bool usePrevState = false)
        {
            return usePrevState ? GetState(role).PreviousButtonPressed : GetState(role).CurrentButtonPressed;
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

        public static float GetAxisEx<TRole>(TRole role, ControllerAxis axis, bool usePrevState = false)
        {
            return GetState(role).GetAxis(axis, usePrevState);
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
            return GetState(role).GetAxis(ControllerAxis.Trigger, usePrevState);
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
            return GetState(role).GetPadAxis(usePrevState);
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
            return handState.GetPress(ControllerButton.Pad) ? handState.GetPadAxis() : Vector2.zero;
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
            return handState.GetPress(ControllerButton.PadTouch) ? handState.GetPadAxis() : Vector2.zero;
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
                return handState.GetPadAxis() - handState.GetPadAxis(true);
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
                return handState.GetPadAxis() - handState.GetPadAxis(true);
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
        public static Vector2 GetScrollDeltaEx<TRole>(TRole role, ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY)
        {
            return GetState(role).GetScrollDelta(scrollType, scale, xAxis, yAxis);
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
            VRModule.TriggerViveControllerHaptic(ViveRole.GetDeviceIndexEx(role), durationMicroSec);
        }

        /// <typeparam name="TRole">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </typeparam>
        /// <param name="role">
        /// TRole can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void TriggerHapticVibrationEx<TRole>(TRole role, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            VRModule.TriggerHapticVibration(ViveRole.GetDeviceIndexEx(role), durationSeconds, frequency, amplitude, startSecondsFromNow);
        }
        #endregion extend generic

        #region extend property role type & value
        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static bool GetPressEx(Type roleType, int roleValue, ControllerButton button)
        {
            return GetState(roleType, roleValue).GetPress(button);
        }

        public static ulong GetPressEx(Type roleType, int roleValue, bool usePrevState = false)
        {
            return usePrevState ? GetState(roleType, roleValue).PreviousButtonPressed : GetState(roleType, roleValue).CurrentButtonPressed;
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

        public static float GetAxisEx(Type roleType, int roleValue, ControllerAxis axis, bool usePrevState = false)
        {
            return GetState(roleType, roleValue).GetAxis(axis, usePrevState);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static float GetTriggerValueEx(Type roleType, int roleValue, bool usePrevState = false)
        {
            return GetState(roleType, roleValue).GetAxis(ControllerAxis.Trigger, usePrevState);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadAxisEx(Type roleType, int roleValue, bool usePrevState = false)
        {
            return GetState(roleType, roleValue).GetPadAxis(usePrevState);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadPressAxisEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            return handState.GetPress(ControllerButton.Pad) ? handState.GetPadAxis() : Vector2.zero;
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static Vector2 GetPadTouchAxisEx(Type roleType, int roleValue)
        {
            var handState = GetState(roleType, roleValue);
            return handState.GetPress(ControllerButton.PadTouch) ? handState.GetPadAxis() : Vector2.zero;
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
                return handState.GetPadAxis() - handState.GetPadAxis(true);
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
                return handState.GetPadAxis() - handState.GetPadAxis(true);
            }
            return Vector2.zero;
        }

        public static Vector2 GetScrollDeltaEx(Type roleType, int roleValue, ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY)
        {
            return GetState(roleType, roleValue).GetScrollDelta(scrollType, scale, xAxis, yAxis);
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
            VRModule.TriggerViveControllerHaptic(ViveRole.GetDeviceIndexEx(roleType, roleValue), durationMicroSec);
        }

        /// <param name="roleType">
        /// Can be DeviceRole, TrackerRole or any other enum type that have ViveRoleEnumAttribute.
        /// Use ViveRole.ValidateViveRoleEnum() to validate role type
        /// </param>
        public static void TriggerHapticVibrationEx(Type roleType, int roleValue, float durationSeconds = 0.01f, float frequency = 85f, float amplitude = 0.125f, float startSecondsFromNow = 0f)
        {
            VRModule.TriggerHapticVibration(ViveRole.GetDeviceIndexEx(roleType, roleValue), durationSeconds, frequency, amplitude, startSecondsFromNow);
        }
        #endregion extend general
    }
}