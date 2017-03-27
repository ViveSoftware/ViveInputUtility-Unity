//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    // Data structure for storing buttons status.
    public partial class ViveInput : MonoBehaviour
    {
        public delegate void RoleEventListener<TRole>(TRole role, ControllerButton button, ButtonEventType eventType);
        public delegate void RoleValueEventListener(Type roleType, int roleValue, ControllerButton button, ButtonEventType eventType);

        private interface ICtrlState
        {
            bool Update(); // return true if  frame skipped
            void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click);
            void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click);
            void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click);
            void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click);
            bool GetPress(ControllerButton button);
            bool GetPressDown(ControllerButton button);
            bool GetPressUp(ControllerButton button);
            float LastPressDownTime(ControllerButton button);
            int ClickCount(ControllerButton button);
            bool GetPress(ulong buttonMask);
            bool GetPressDown(ulong buttonMask);
            bool GetPressUp(ulong buttonMask);
            bool GetPress(EVRButtonId buttonId);
            bool GetPressDown(EVRButtonId buttonId);
            bool GetPressUp(EVRButtonId buttonId);
            bool GetTouch(ulong buttonMask);
            bool GetTouchDown(ulong buttonMask);
            bool GetTouchUp(ulong buttonMask);
            bool GetTouch(EVRButtonId buttonId);
            bool GetTouchDown(EVRButtonId buttonId);
            bool GetTouchUp(EVRButtonId buttonId);
            Vector2 GetAxis(bool usePrevState, EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad);
            Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad, bool usePrevState = false);
            float GetTriggerValue(bool usePrevState = false);
            bool GetHairTrigger();
            bool GetHairTriggerDown();
            bool GetHairTriggerUp();
            Vector2 GetPadPressVector();
            Vector2 GetPadTouchVector();
            VRControllerState_t GetCurrentRawState();
            VRControllerState_t GetPreviousRawState();
        }

        private class CtrlState : ICtrlState
        {
            public virtual bool Update() { return true; } // return true if  frame skipped
            public virtual void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual bool GetPress(ControllerButton button) { return false; }
            public virtual bool GetPressDown(ControllerButton button) { return false; }
            public virtual bool GetPressUp(ControllerButton button) { return false; }
            public virtual float LastPressDownTime(ControllerButton button) { return 0f; }
            public virtual int ClickCount(ControllerButton button) { return 0; }
            public virtual bool GetPress(ulong buttonMask) { return false; }
            public virtual bool GetPressDown(ulong buttonMask) { return false; }
            public virtual bool GetPressUp(ulong buttonMask) { return false; }
            public virtual bool GetPress(EVRButtonId buttonId) { return false; }
            public virtual bool GetPressDown(EVRButtonId buttonId) { return false; }
            public virtual bool GetPressUp(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouch(ulong buttonMask) { return false; }
            public virtual bool GetTouchDown(ulong buttonMask) { return false; }
            public virtual bool GetTouchUp(ulong buttonMask) { return false; }
            public virtual bool GetTouch(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouchDown(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouchUp(EVRButtonId buttonId) { return false; }
            public virtual Vector2 GetAxis(bool usePrevState, EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad) { return Vector2.zero; }
            public virtual Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad, bool usePrevState = false) { return Vector2.zero; }
            public virtual float GetTriggerValue(bool usePrevState = false) { return 0f; }
            public virtual bool GetHairTrigger() { return false; }
            public virtual bool GetHairTriggerDown() { return false; }
            public virtual bool GetHairTriggerUp() { return false; }
            public virtual Vector2 GetPadPressVector() { return Vector2.zero; }
            public virtual Vector2 GetPadTouchVector() { return Vector2.zero; }
            public virtual VRControllerState_t GetCurrentRawState() { return default(VRControllerState_t); }
            public virtual VRControllerState_t GetPreviousRawState() { return default(VRControllerState_t); }
        }

        private sealed class RCtrlState : CtrlState
        {
            public readonly ViveRole.IMap m_map;
            public readonly int m_roleValue;
            public readonly Type m_roleEnumType;

            private int updatedFrameCount;
            private uint updatedDeviceIndex;
            private VRControllerState_t currentState;
            private VRControllerState_t previousState;

            private readonly float[] lastPressedTimes = new float[CONTROLLER_BUTTON_COUNT];
            private readonly int[] clickCount = new int[CONTROLLER_BUTTON_COUNT];

            private Action[][] listeners;
            private RoleValueEventListener[][] typeListeners;

            private Vector2 padDownAxis;
            private Vector2 padTouchDownAxis;

            private float hairTriggerDelta = 0.1f; // amount trigger must be pulled or released to change state
            private float hairTriggerLimit;
            private bool hairTriggerState;
            private bool hairTriggerPrevState;

            public RCtrlState(Type roleEnumType, int roleValue)
            {
                m_map = ViveRole.GetMap(roleEnumType);
                m_roleValue = roleValue;
                m_roleEnumType = roleEnumType;
            }

            // return true if  frame skipped
            public override bool Update()
            {
                if (Time.frameCount == updatedFrameCount) { return true; }
                updatedFrameCount = Time.frameCount;

                var deviceIndex = m_map.GetMappedDeviceByRoleValue(m_roleValue);
                if (deviceIndex == updatedDeviceIndex && !ViveRole.IsValidIndex(deviceIndex)) { return false; }
                updatedDeviceIndex = deviceIndex;

                previousState = currentState;
                currentState = ViveRole.IsValidIndex(deviceIndex) ? s_controllerStats[deviceIndex] : default(VRControllerState_t);

                this.UpdateHairTrigger();

                if (GetPressDown(ControllerButton.Pad)) { padDownAxis = GetAxis(); }
                if (GetPressDown(ControllerButton.PadTouch)) { padTouchDownAxis = GetAxis(); }

                for (int i = 0; i < CONTROLLER_BUTTON_COUNT; ++i)
                {
                    UpdateClickCount((ControllerButton)i);
                }

                for (int i = 0; i < CONTROLLER_BUTTON_COUNT; ++i)
                {
                    InvokeEvent((ControllerButton)i);
                }

                return false;
            }

            private void TryInvokeListener(ControllerButton button, ButtonEventType type)
            {
                if (listeners == null) { return; }
                if (listeners[(int)type] == null) { return; }
                if (listeners[(int)type][(int)button] == null) { return; }
                listeners[(int)type][(int)button].Invoke();
            }

            public override void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { listeners = new Action[BUTTON_EVENT_COUNT][]; }
                if (listeners[(int)type] == null) { listeners[(int)type] = new Action[CONTROLLER_BUTTON_COUNT]; }
                if (listeners[(int)type][(int)button] == null)
                {
                    listeners[(int)type][(int)button] = listener;
                }
                else
                {
                    listeners[(int)type][(int)button] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { return; }
                if (listeners[(int)type] == null) { return; }
                if (listeners[(int)type][(int)button] == null) { return; }
                listeners[(int)type][(int)button] -= listener;
            }

            private void TryInvokeTypeListener(ControllerButton button, ButtonEventType type)
            {
                if (typeListeners == null) { return; }
                if (typeListeners[(int)type] == null) { return; }
                if (typeListeners[(int)type][(int)button] == null) { return; }
                typeListeners[(int)type][(int)button].Invoke(m_roleEnumType, m_roleValue, button, type);
            }

            public override void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (typeListeners == null) { typeListeners = new RoleValueEventListener[BUTTON_EVENT_COUNT][]; }
                if (typeListeners[(int)type] == null) { typeListeners[(int)type] = new RoleValueEventListener[CONTROLLER_BUTTON_COUNT]; }
                if (typeListeners[(int)type][(int)button] == null)
                {
                    typeListeners[(int)type][(int)button] = listener;
                }
                else
                {
                    typeListeners[(int)type][(int)button] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (typeListeners == null) { return; }
                if (typeListeners[(int)type] == null) { return; }
                if (typeListeners[(int)type][(int)button] == null) { return; }
                typeListeners[(int)type][(int)button] -= listener;
            }


            private void UpdateClickCount(ControllerButton button)
            {
                var index = (int)button;
                if (GetPressDown(button))
                {
                    if (Time.time - lastPressedTimes[index] < ViveInput.clickInterval)
                    {
                        ++clickCount[index];
                    }
                    else
                    {
                        clickCount[index] = 1;
                    }

                    lastPressedTimes[index] = Time.time;
                }
            }

            private void InvokeEvent(ControllerButton button)
            {
                var index = (int)button;
                if (GetPress(button))
                {
                    if (GetPressDown(button))
                    {
                        // PressDown event
                        TryInvokeListener(button, ButtonEventType.Down);
                        TryInvokeTypeListener(button, ButtonEventType.Down);
                    }

                    // Press event
                    TryInvokeListener(button, ButtonEventType.Press);
                    TryInvokeTypeListener(button, ButtonEventType.Press);
                }
                else if (GetPressUp(button))
                {
                    // PressUp event
                    TryInvokeListener(button, ButtonEventType.Up);
                    TryInvokeTypeListener(button, ButtonEventType.Up);

                    if (Time.time - lastPressedTimes[index] < ViveInput.clickInterval)
                    {
                        // Click event
                        TryInvokeListener(button, ButtonEventType.Click);
                        TryInvokeTypeListener(button, ButtonEventType.Click);
                    }
                }
            }

            public override bool GetPress(ControllerButton button)
            {
                Update();
                switch (button)
                {
                    case ControllerButton.Trigger: return GetPress(EVRButtonId.k_EButton_SteamVR_Trigger);
                    case ControllerButton.FullTrigger: return currentState.rAxis1.x >= 1f;
                    case ControllerButton.HairTrigger: return GetHairTrigger();
                    case ControllerButton.Pad: return GetPress(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.PadTouch: return GetTouch(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.Grip: return GetPress(EVRButtonId.k_EButton_Grip);
                    case ControllerButton.Menu: return GetPress(EVRButtonId.k_EButton_ApplicationMenu);
                    default: return false;
                }
            }

            public override bool GetPressDown(ControllerButton button)
            {
                Update();
                switch (button)
                {
                    case ControllerButton.Trigger: return GetPressDown(EVRButtonId.k_EButton_SteamVR_Trigger);
                    case ControllerButton.FullTrigger: return currentState.rAxis1.x >= 1f && previousState.rAxis1.x < 1f;
                    case ControllerButton.HairTrigger: return GetHairTriggerDown();
                    case ControllerButton.Pad: return GetPressDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.PadTouch: return GetTouchDown(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.Grip: return GetPressDown(EVRButtonId.k_EButton_Grip);
                    case ControllerButton.Menu: return GetPressDown(EVRButtonId.k_EButton_ApplicationMenu);
                    default: return false;
                }
            }

            public override bool GetPressUp(ControllerButton button)
            {
                Update();
                switch (button)
                {
                    case ControllerButton.Trigger: return GetPressUp(EVRButtonId.k_EButton_SteamVR_Trigger);
                    case ControllerButton.FullTrigger: return currentState.rAxis1.x < 1f && previousState.rAxis1.x >= 1f;
                    case ControllerButton.HairTrigger: return GetHairTriggerUp();
                    case ControllerButton.Pad: return GetPressUp(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.PadTouch: return GetTouchUp(EVRButtonId.k_EButton_SteamVR_Touchpad);
                    case ControllerButton.Grip: return GetPressUp(EVRButtonId.k_EButton_Grip);
                    case ControllerButton.Menu: return GetPressUp(EVRButtonId.k_EButton_ApplicationMenu);
                    default: return false;
                }
            }

            public override float LastPressDownTime(ControllerButton button)
            {
                var index = (int)button;
                if (index >= 0 && index < CONTROLLER_BUTTON_COUNT)
                {
                    Update();
                    return lastPressedTimes[index];
                }
                else
                {
                    return 0f;
                }
            }

            public override int ClickCount(ControllerButton button)
            {
                var index = (int)button;
                if (index >= 0 && index < CONTROLLER_BUTTON_COUNT)
                {
                    Update();
                    return clickCount[index];
                }
                else
                {
                    return 0;
                }
            }

            public override bool GetPress(ulong buttonMask) { Update(); return (currentState.ulButtonPressed & buttonMask) != 0; }
            public override bool GetPressDown(ulong buttonMask) { Update(); return (currentState.ulButtonPressed & buttonMask) != 0 && (previousState.ulButtonPressed & buttonMask) == 0; }
            public override bool GetPressUp(ulong buttonMask) { Update(); return (currentState.ulButtonPressed & buttonMask) == 0 && (previousState.ulButtonPressed & buttonMask) != 0; }

            public override bool GetPress(EVRButtonId buttonId) { return GetPress(1ul << (int)buttonId); }
            public override bool GetPressDown(EVRButtonId buttonId) { return GetPressDown(1ul << (int)buttonId); }
            public override bool GetPressUp(EVRButtonId buttonId) { return GetPressUp(1ul << (int)buttonId); }

            public override bool GetTouch(ulong buttonMask) { Update(); return (currentState.ulButtonTouched & buttonMask) != 0; }
            public override bool GetTouchDown(ulong buttonMask) { Update(); return (currentState.ulButtonTouched & buttonMask) != 0 && (previousState.ulButtonTouched & buttonMask) == 0; }
            public override bool GetTouchUp(ulong buttonMask) { Update(); return (currentState.ulButtonTouched & buttonMask) == 0 && (previousState.ulButtonTouched & buttonMask) != 0; }

            public override bool GetTouch(EVRButtonId buttonId) { return GetTouch(1ul << (int)buttonId); }
            public override bool GetTouchDown(EVRButtonId buttonId) { return GetTouchDown(1ul << (int)buttonId); }
            public override bool GetTouchUp(EVRButtonId buttonId) { return GetTouchUp(1ul << (int)buttonId); }

            public override Vector2 GetAxis(bool usePrevState, EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad)
            {
                return GetAxis(buttonId, usePrevState);
            }

            public override Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad, bool usePrevState = false)
            {
                Update();
                var state = usePrevState ? previousState : currentState;
                var axisId = (uint)buttonId - (uint)EVRButtonId.k_EButton_Axis0;
                switch (axisId)
                {
                    case 0: return new Vector2(state.rAxis0.x, state.rAxis0.y);
                    case 1: return new Vector2(state.rAxis1.x, state.rAxis1.y);
                    case 2: return new Vector2(state.rAxis2.x, state.rAxis2.y);
                    case 3: return new Vector2(state.rAxis3.x, state.rAxis3.y);
                    case 4: return new Vector2(state.rAxis4.x, state.rAxis4.y);
                    default: return Vector2.zero;
                }
            }

            private void UpdateHairTrigger()
            {
                hairTriggerPrevState = hairTriggerState;
                var value = currentState.rAxis1.x; // trigger
                if (hairTriggerState)
                {
                    if (value < hairTriggerLimit - hairTriggerDelta || value <= 0.0f)
                        hairTriggerState = false;
                }
                else
                {
                    if (value > hairTriggerLimit + hairTriggerDelta || value >= 1.0f)
                        hairTriggerState = true;
                }
                hairTriggerLimit = hairTriggerState ? Mathf.Max(hairTriggerLimit, value) : Mathf.Min(hairTriggerLimit, value);
            }

            public override float GetTriggerValue(bool usePrevState = false) { Update(); return usePrevState ? previousState.rAxis1.x : currentState.rAxis1.x; }
            public override bool GetHairTrigger() { Update(); return hairTriggerState; }
            public override bool GetHairTriggerDown() { Update(); return hairTriggerState && !hairTriggerPrevState; }
            public override bool GetHairTriggerUp() { Update(); return !hairTriggerState && hairTriggerPrevState; }

            public override Vector2 GetPadPressVector()
            {
                return GetPress(ControllerButton.Pad) ? (GetAxis() - padDownAxis) : Vector2.zero;
            }

            public override Vector2 GetPadTouchVector()
            {
                return GetPress(ControllerButton.PadTouch) ? (GetAxis() - padTouchDownAxis) : Vector2.zero;
            }

            public override VRControllerState_t GetCurrentRawState()
            {
                return currentState;
            }

            public override VRControllerState_t GetPreviousRawState()
            {
                return previousState;
            }
        }

        private interface ICtrlState<TRole> : ICtrlState
        {
            void AddListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click);
            void RemoveListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click);
        }

        private class GCtrlState<TRole> : ICtrlState<TRole>
        {
            public virtual bool Update() { return true; } // return true if  frame skipped
            public virtual void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual bool GetPress(ControllerButton button) { return false; }
            public virtual bool GetPressDown(ControllerButton button) { return false; }
            public virtual bool GetPressUp(ControllerButton button) { return false; }
            public virtual float LastPressDownTime(ControllerButton button) { return 0f; }
            public virtual int ClickCount(ControllerButton button) { return 0; }
            public virtual bool GetPress(ulong buttonMask) { return false; }
            public virtual bool GetPressDown(ulong buttonMask) { return false; }
            public virtual bool GetPressUp(ulong buttonMask) { return false; }
            public virtual bool GetPress(EVRButtonId buttonId) { return false; }
            public virtual bool GetPressDown(EVRButtonId buttonId) { return false; }
            public virtual bool GetPressUp(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouch(ulong buttonMask) { return false; }
            public virtual bool GetTouchDown(ulong buttonMask) { return false; }
            public virtual bool GetTouchUp(ulong buttonMask) { return false; }
            public virtual bool GetTouch(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouchDown(EVRButtonId buttonId) { return false; }
            public virtual bool GetTouchUp(EVRButtonId buttonId) { return false; }
            public virtual Vector2 GetAxis(bool usePrevState, EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad) { return Vector2.zero; }
            public virtual Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad, bool usePrevState = false) { return Vector2.zero; }
            public virtual float GetTriggerValue(bool usePrevState = false) { return 0f; }
            public virtual bool GetHairTrigger() { return false; }
            public virtual bool GetHairTriggerDown() { return false; }
            public virtual bool GetHairTriggerUp() { return false; }
            public virtual Vector2 GetPadPressVector() { return Vector2.zero; }
            public virtual Vector2 GetPadTouchVector() { return Vector2.zero; }
            public virtual VRControllerState_t GetCurrentRawState() { return default(VRControllerState_t); }
            public virtual VRControllerState_t GetPreviousRawState() { return default(VRControllerState_t); }

            public virtual void AddListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click) { }

            protected virtual void InvokeEvent(ControllerButton button) { }
            protected virtual void TryInvokeListener(ControllerButton button, ButtonEventType type) { }
        }

        private sealed class RGCtrolState<TRole> : GCtrlState<TRole>
        {
            public readonly static GCtrlState<TRole> s_defaultState = new GCtrlState<TRole>();
            public static RGCtrolState<TRole>[] s_roleStates;

            private readonly ICtrlState m_state;
            private readonly TRole m_role;

            private RoleEventListener<TRole>[][] listeners;

            public RGCtrolState(TRole role)
            {
                var info = ViveRoleEnum.GetInfo<TRole>();
                m_state = GetState(typeof(TRole), info.ToRoleValue(role));
                m_role = role;
            }

            // return true if  frame skipped
            public override bool Update()
            {
                if (m_state.Update()) { return true; }
                //Debug.Log("RGCtrolState<" + typeof(TRole).Name + "> Update");
                for (int i = 0; i < CONTROLLER_BUTTON_COUNT; ++i)
                {
                    InvokeEvent((ControllerButton)i);
                }

                return false;
            }

            public override void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { m_state.AddListener(button, listener, type); }
            public override void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { m_state.RemoveListener(button, listener, type); }
            public override void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { m_state.AddListener(button, listener, type); }
            public override void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { m_state.RemoveListener(button, listener, type); }
            public override bool GetPress(ControllerButton button) { return m_state.GetPress(button); }
            public override bool GetPressDown(ControllerButton button) { return m_state.GetPressDown(button); }
            public override bool GetPressUp(ControllerButton button) { return m_state.GetPressUp(button); }
            public override float LastPressDownTime(ControllerButton button) { return m_state.LastPressDownTime(button); }
            public override int ClickCount(ControllerButton button) { return m_state.ClickCount(button); }
            public override bool GetPress(ulong buttonMask) { return m_state.GetPress(buttonMask); }
            public override bool GetPressDown(ulong buttonMask) { return m_state.GetPressDown(buttonMask); }
            public override bool GetPressUp(ulong buttonMask) { return m_state.GetPressUp(buttonMask); }
            public override bool GetPress(EVRButtonId buttonId) { return m_state.GetPress(buttonId); }
            public override bool GetPressDown(EVRButtonId buttonId) { return m_state.GetPressDown(buttonId); }
            public override bool GetPressUp(EVRButtonId buttonId) { return m_state.GetPressUp(buttonId); }
            public override bool GetTouch(ulong buttonMask) { return m_state.GetTouch(buttonMask); }
            public override bool GetTouchDown(ulong buttonMask) { return m_state.GetTouchDown(buttonMask); }
            public override bool GetTouchUp(ulong buttonMask) { return m_state.GetTouchUp(buttonMask); }
            public override bool GetTouch(EVRButtonId buttonId) { return m_state.GetTouch(buttonId); }
            public override bool GetTouchDown(EVRButtonId buttonId) { return m_state.GetTouchDown(buttonId); }
            public override bool GetTouchUp(EVRButtonId buttonId) { return m_state.GetTouchUp(buttonId); }
            public override Vector2 GetAxis(bool usePrevState, EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad) { return m_state.GetAxis(usePrevState, buttonId); }
            public override Vector2 GetAxis(EVRButtonId buttonId = EVRButtonId.k_EButton_SteamVR_Touchpad, bool usePrevState = false) { return m_state.GetAxis(buttonId, usePrevState); }
            public override float GetTriggerValue(bool usePrevState = false) { return m_state.GetTriggerValue(usePrevState); }
            public override bool GetHairTrigger() { return m_state.GetHairTrigger(); }
            public override bool GetHairTriggerDown() { return m_state.GetHairTriggerDown(); }
            public override bool GetHairTriggerUp() { return m_state.GetHairTriggerUp(); }
            public override Vector2 GetPadPressVector() { return m_state.GetPadPressVector(); }
            public override Vector2 GetPadTouchVector() { return m_state.GetPadTouchVector(); }
            public override VRControllerState_t GetCurrentRawState() { return m_state.GetCurrentRawState(); }
            public override VRControllerState_t GetPreviousRawState() { return m_state.GetPreviousRawState(); }

            protected override void TryInvokeListener(ControllerButton button, ButtonEventType type)
            {
                if (listeners == null) { return; }
                if (listeners[(int)type] == null) { return; }
                if (listeners[(int)type][(int)button] == null) { return; }
                listeners[(int)type][(int)button].Invoke(m_role, button, type);
            }

            public override void AddListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { listeners = new RoleEventListener<TRole>[BUTTON_EVENT_COUNT][]; }
                if (listeners[(int)type] == null) { listeners[(int)type] = new RoleEventListener<TRole>[CONTROLLER_BUTTON_COUNT]; }
                if (listeners[(int)type][(int)button] == null)
                {
                    listeners[(int)type][(int)button] = listener;
                }
                else
                {
                    listeners[(int)type][(int)button] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { return; }
                if (listeners[(int)type] == null) { return; }
                if (listeners[(int)type][(int)button] == null) { return; }
                listeners[(int)type][(int)button] -= listener;
            }

            protected override void InvokeEvent(ControllerButton button)
            {
                if (GetPress(button))
                {
                    if (GetPressDown(button))
                    {
                        // PressDown event
                        TryInvokeListener(button, ButtonEventType.Down);
                    }

                    // Press event
                    TryInvokeListener(button, ButtonEventType.Press);
                }
                else if (GetPressUp(button))
                {
                    // PressUp event
                    TryInvokeListener(button, ButtonEventType.Up);

                    if (Time.time - m_state.LastPressDownTime(button) < ViveInput.clickInterval)
                    {
                        // Click event
                        TryInvokeListener(button, ButtonEventType.Click);
                    }
                }
            }
        }

        private static readonly CtrlState s_defaultState = new CtrlState();
        private static readonly IndexedTable<Type, ICtrlState[]> s_roleStateTable = new IndexedTable<Type, ICtrlState[]>();

        private static ICtrlState GetState(Type roleType, int roleValue)
        {
            Initialize();
            var info = ViveRoleEnum.GetInfo(roleType);

            if (!info.IsValidRoleValue(roleValue)) { return s_defaultState; }

            ICtrlState[] stateList;
            if (!s_roleStateTable.TryGetValue(roleType, out stateList) || stateList == null)
            {
                s_roleStateTable[roleType] = stateList = new ICtrlState[info.ValidRoleLength];
            }

            var roleOffset = info.ToRoleOffset(roleValue);
            if (stateList[roleOffset] == null)
            {
                stateList[roleOffset] = new RCtrlState(roleType, roleValue);
            }

            return stateList[roleOffset];
        }

        private static ICtrlState<TRole> GetState<TRole>(TRole role)
        {
            Initialize();
            var info = ViveRoleEnum.GetInfo<TRole>();

            if (!info.IsValidRole(role)) { return RGCtrolState<TRole>.s_defaultState; }

            if (RGCtrolState<TRole>.s_roleStates == null)
            {
                RGCtrolState<TRole>.s_roleStates = new RGCtrolState<TRole>[info.ValidRoleLength];
            }

            var roleOffset = info.ToRoleOffsetFromRole(role);
            if (RGCtrolState<TRole>.s_roleStates[roleOffset] == null)
            {
                RGCtrolState<TRole>.s_roleStates[roleOffset] = new RGCtrolState<TRole>(role);
                s_roleStateTable[typeof(TRole)][roleOffset] = RGCtrolState<TRole>.s_roleStates[roleOffset];
            }

            return RGCtrolState<TRole>.s_roleStates[roleOffset];
        }
    }
}