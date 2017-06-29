//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using UnityEngine;
using UnityEngine.VR;

namespace HTC.UnityPlugin.Vive
{
    // Data structure for storing buttons status.
    public partial class ViveInput : SingletonBehaviour<ViveInput>
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
            bool GetPress(ControllerButton button, bool usePrevState = false);
            bool GetPressDown(ControllerButton button);
            bool GetPressUp(ControllerButton button);
            float LastPressDownTime(ControllerButton button);
            int ClickCount(ControllerButton button);
            float GetAxis(ControllerAxis axis, bool usePrevState = false);
            Vector2 GetPadAxis(bool usePrevState = false);
            Vector2 GetPadPressVector();
            Vector2 GetPadTouchVector();
            Vector2 GetScrollDelta(ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY);
        }

        private class CtrlState : ICtrlState
        {
            public virtual bool Update() { return true; } // return true if  frame skipped
            public virtual void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { }
            public virtual bool GetPress(ControllerButton button, bool usePrevState = false) { return false; }
            public virtual bool GetPressDown(ControllerButton button) { return false; }
            public virtual bool GetPressUp(ControllerButton button) { return false; }
            public virtual float LastPressDownTime(ControllerButton button) { return 0f; }
            public virtual int ClickCount(ControllerButton button) { return 0; }
            public virtual float GetAxis(ControllerAxis axis, bool usePrevState = false) { return 0f; }
            public virtual Vector2 GetPadAxis(bool usePrevState = false) { return Vector2.zero; }
            public virtual Vector2 GetPadPressVector() { return Vector2.zero; }
            public virtual Vector2 GetPadTouchVector() { return Vector2.zero; }
            public virtual Vector2 GetScrollDelta(ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY) { return Vector2.zero; }
        }

        private sealed class RCtrlState : CtrlState
        {
            public readonly ViveRole.IMap m_map;
            public readonly int m_roleValue;
            public readonly Type m_roleEnumType;

            private int updatedFrameCount = -1;
            private uint prevDeviceIndex;
            private VRModuleDeviceModel trackedDeviceModel = VRModuleDeviceModel.Unknown;

            private readonly bool[] prevButtonPress = new bool[CONTROLLER_BUTTON_COUNT];
            private readonly bool[] currButtonPress = new bool[CONTROLLER_BUTTON_COUNT];

            private readonly float[] prevAxisValue = new float[CONTROLLER_AXIS_COUNT];
            private readonly float[] currAxisValue = new float[CONTROLLER_AXIS_COUNT];

            private readonly float[] lastPressDownTime = new float[CONTROLLER_BUTTON_COUNT];
            private readonly int[] clickCount = new int[CONTROLLER_BUTTON_COUNT];

            private Action[][] listeners;
            private RoleValueEventListener[][] typeListeners;

            private Vector2 padDownAxis;
            private Vector2 padTouchDownAxis;

            private const float hairDelta = 0.1f; // amount trigger must be pulled or released to change state
            private float hairTriggerLimit;
            private float hairGripLimit;

            public RCtrlState(Type roleEnumType, int roleValue)
            {
                m_map = ViveRole.GetMap(roleEnumType);
                m_roleValue = roleValue;
                m_roleEnumType = roleEnumType;
            }

            // return true if frame skipped
            public override bool Update()
            {
                if (!ChangeProp.Set(ref updatedFrameCount, Time.frameCount)) { return true; }

                var deviceIndex = m_map.GetMappedDeviceByRoleValue(m_roleValue);

                // treat this frame as updated if both prevDeviceIndex and currentDeviceIndex are invalid
                if (!ViveRole.IsValidIndex(prevDeviceIndex) && !ViveRole.IsValidIndex(deviceIndex)) { return false; }

                // get device state
                var currState = VRModule.GetCurrentDeviceState(deviceIndex);

                // copy to previous states
                prevDeviceIndex = deviceIndex;
                for (int i = CONTROLLER_BUTTON_COUNT - 1; i >= 0; --i) { prevButtonPress[i] = currButtonPress[i]; }
                for (int i = CONTROLLER_AXIS_COUNT - 1; i >= 0; --i) { prevAxisValue[i] = currAxisValue[i]; }

                trackedDeviceModel = currState.deviceModel;

                // update axis values
                currAxisValue[(int)ControllerAxis.PadX] = currState.GetAxisValue(VRModuleRawAxis.PadOrStickX);
                currAxisValue[(int)ControllerAxis.PadY] = currState.GetAxisValue(VRModuleRawAxis.PadOrStickY);
                currAxisValue[(int)ControllerAxis.Trigger] = currState.GetAxisValue(VRModuleRawAxis.Trigger);
                currAxisValue[(int)ControllerAxis.Grip] = currState.GetAxisValue(VRModuleRawAxis.GripOrHandTrigger);

                // update button states
                currButtonPress[(int)ControllerButton.Pad] = currState.GetButtonPress(VRModuleRawButton.PadOrStickPress);
                currButtonPress[(int)ControllerButton.PadTouch] = currState.GetButtonPress(VRModuleRawButton.PadOrStickTouch);
                currButtonPress[(int)ControllerButton.Menu] = currState.GetButtonPress(VRModuleRawButton.FunctionKey);

                // update hair trigger
                currButtonPress[(int)ControllerButton.Trigger] = prevButtonPress[(int)ControllerButton.Trigger] ? currAxisValue[(int)ControllerAxis.Trigger] >= 0.45f : currAxisValue[(int)ControllerAxis.Trigger] >= 0.55f;
                currButtonPress[(int)ControllerButton.FullTrigger] = currAxisValue[(int)ControllerAxis.Trigger] == 1f;
                currButtonPress[(int)ControllerButton.HairTrigger] =
                    prevButtonPress[(int)ControllerButton.HairTrigger] ?
                        currAxisValue[(int)ControllerAxis.Trigger] >= (hairTriggerLimit - hairDelta) && currAxisValue[(int)ControllerAxis.Trigger] > 0.0f
                        :
                        currAxisValue[(int)ControllerAxis.Trigger] > (hairTriggerLimit + hairDelta) || currAxisValue[(int)ControllerAxis.Trigger] >= 1.0f
                        ;

                hairTriggerLimit = currButtonPress[(int)ControllerButton.HairTrigger] ? Mathf.Max(hairTriggerLimit, currAxisValue[(int)ControllerAxis.Trigger]) : Mathf.Min(hairTriggerLimit, currAxisValue[(int)ControllerAxis.Trigger]);

                // update hair grip
                currButtonPress[(int)ControllerButton.Grip] = prevButtonPress[(int)ControllerButton.Grip] ? currAxisValue[(int)ControllerAxis.Grip] >= 0.45f : currAxisValue[(int)ControllerAxis.Grip] >= 0.55f;
                currButtonPress[(int)ControllerButton.FullGrip] = currAxisValue[(int)ControllerAxis.Grip] == 1f;
                currButtonPress[(int)ControllerButton.HairGrip] =
                    prevButtonPress[(int)ControllerButton.HairGrip] ?
                        currAxisValue[(int)ControllerAxis.Grip] >= (hairGripLimit - hairDelta) && currAxisValue[(int)ControllerAxis.Grip] > 0.0f
                        :
                        currAxisValue[(int)ControllerAxis.Grip] > (hairGripLimit + hairDelta) || currAxisValue[(int)ControllerAxis.Grip] >= 1.0f
                        ;

                hairGripLimit = currButtonPress[(int)ControllerButton.HairGrip] ? Mathf.Max(hairGripLimit, currAxisValue[(int)ControllerAxis.Grip]) : Mathf.Min(hairTriggerLimit, currAxisValue[(int)ControllerAxis.Grip]);

                // record pad down axis values
                if (GetPressDown(ControllerButton.Pad))
                {
                    padDownAxis = new Vector2(currAxisValue[(int)ControllerAxis.PadX], currAxisValue[(int)ControllerAxis.PadY]);
                }

                if (GetPressDown(ControllerButton.PadTouch))
                {
                    padTouchDownAxis = new Vector2(currAxisValue[(int)ControllerAxis.PadX], currAxisValue[(int)ControllerAxis.PadY]);
                }

                // record press down time and click count
                var timeNow = Time.unscaledTime;
                for (int button = 0; button < CONTROLLER_BUTTON_COUNT; ++button)
                {
                    if (GetPressDown((ControllerButton)button))
                    {
                        if (timeNow - lastPressDownTime[button] < clickInterval)
                        {
                            ++clickCount[button];
                        }
                        else
                        {
                            clickCount[button] = 1;
                        }

                        lastPressDownTime[button] = timeNow;
                    }
                }

                // invoke event listeners
                for (ControllerButton button = 0; button < (ControllerButton)CONTROLLER_BUTTON_COUNT; ++button)
                {
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

                        if (timeNow - lastPressDownTime[(int)button] < clickInterval)
                        {
                            // Click event
                            TryInvokeListener(button, ButtonEventType.Click);
                            TryInvokeTypeListener(button, ButtonEventType.Click);
                        }
                    }
                }

                return false;
            }

            private void TryInvokeListener(ControllerButton button, ButtonEventType type)
            {
                if (listeners == null) { return; }
                if (listeners[(int)button] == null) { return; }
                if (listeners[(int)button][(int)type] == null) { return; }
                listeners[(int)button][(int)type].Invoke();
            }

            public override void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { listeners = new Action[CONTROLLER_BUTTON_COUNT][]; }
                if (listeners[(int)button] == null) { listeners[(int)button] = new Action[BUTTON_EVENT_COUNT]; }
                if (listeners[(int)button][(int)type] == null)
                {
                    listeners[(int)button][(int)type] = listener;
                }
                else
                {
                    listeners[(int)button][(int)type] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { return; }
                if (listeners[(int)button] == null) { return; }
                if (listeners[(int)button][(int)type] == null) { return; }
                listeners[(int)button][(int)type] -= listener;
            }

            private void TryInvokeTypeListener(ControllerButton button, ButtonEventType type)
            {
                if (typeListeners == null) { return; }
                if (typeListeners[(int)button] == null) { return; }
                if (typeListeners[(int)button][(int)type] == null) { return; }
                typeListeners[(int)button][(int)type].Invoke(m_roleEnumType, m_roleValue, button, type);
            }

            public override void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (typeListeners == null) { typeListeners = new RoleValueEventListener[CONTROLLER_BUTTON_COUNT][]; }
                if (typeListeners[(int)button] == null) { typeListeners[(int)button] = new RoleValueEventListener[BUTTON_EVENT_COUNT]; }
                if (typeListeners[(int)button][(int)type] == null)
                {
                    typeListeners[(int)button][(int)type] = listener;
                }
                else
                {
                    typeListeners[(int)button][(int)type] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (typeListeners == null) { return; }
                if (typeListeners[(int)button] == null) { return; }
                if (typeListeners[(int)button][(int)type] == null) { return; }
                typeListeners[(int)button][(int)type] -= listener;
            }

            public override bool GetPress(ControllerButton button, bool usePrevState = false)
            {
                return IsValidButton(button) && (usePrevState ? prevButtonPress[(int)button] : currButtonPress[(int)button]);
            }

            public override bool GetPressDown(ControllerButton button)
            {
                return IsValidButton(button) && !prevButtonPress[(int)button] && currButtonPress[(int)button];
            }

            public override bool GetPressUp(ControllerButton button)
            {
                return IsValidButton(button) && prevButtonPress[(int)button] && !currButtonPress[(int)button];
            }

            public override float LastPressDownTime(ControllerButton button)
            {
                return IsValidButton(button) ? lastPressDownTime[(int)button] : 0f;
            }

            public override int ClickCount(ControllerButton button)
            {
                return IsValidButton(button) ? clickCount[(int)button] : 0;
            }

            public override float GetAxis(ControllerAxis axis, bool usePrevState = false)
            {
                if (IsValidAxis(axis))
                {
                    return usePrevState ? prevAxisValue[(int)axis] : currAxisValue[(int)axis];
                }
                else
                {
                    return 0f;
                }
            }

            public override Vector2 GetPadAxis(bool usePrevState = false)
            {
                if (usePrevState)
                {
                    return new Vector2(prevAxisValue[(int)ControllerAxis.PadX], prevAxisValue[(int)ControllerAxis.PadY]);
                }
                else
                {
                    return new Vector2(currAxisValue[(int)ControllerAxis.PadX], currAxisValue[(int)ControllerAxis.PadY]);
                }
            }

            public override Vector2 GetPadPressVector()
            {
                return GetPress(ControllerButton.Pad) ? (GetPadAxis() - padDownAxis) : Vector2.zero;
            }

            public override Vector2 GetPadTouchVector()
            {
                return GetPress(ControllerButton.PadTouch) ? (GetPadAxis() - padTouchDownAxis) : Vector2.zero;
            }

            public override Vector2 GetScrollDelta(ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY)
            {
                if (scrollType == ScrollType.None) { return Vector2.zero; }

                // consider scroll mode depends on whitch device platform useed
                // OpenVR: finger dragging on the trackpad to scroll, drag faster to scroll faster
                // Oculus: leaning the thumbstick to scroll, lean larger angle to scroll faster
                ScrollType mode;
                if (scrollType == ScrollType.Auto)
                {
                    switch (trackedDeviceModel)
                    {
                        case VRModuleDeviceModel.ViveController:
                            mode = ScrollType.Trackpad; break;
                        case VRModuleDeviceModel.OculusTouchLeft:
                        case VRModuleDeviceModel.OculusTouchRight:
                            mode = ScrollType.Thumbstick; break;
                        default:
                            mode = ScrollType.None; break;
                    }
                }
                else
                {
                    mode = scrollType;
                }

                Vector2 scrollDelta;
                switch (mode)
                {
                    case ScrollType.Trackpad:
                        {
                            var prevX = GetAxis(xAxis, true);
                            var prevY = GetAxis(yAxis, true);
                            var currX = GetAxis(xAxis, false);
                            var currY = GetAxis(yAxis, false);

                            // filter out invalid axis values
                            // assume that valid axis value is never zero
                            // note: don't know why sometimes even trackpad touched (GetKey(Trackpad)==true), GetAxis(Trackpad) still get zero values
                            if ((prevX == 0f && prevY == 0f) || (currX == 0f && currY == 0f))
                            {
                                return Vector2.zero;
                            }
                            else
                            {
                                scrollDelta = new Vector2(prevX - currX, prevY - currY) * 50f;
                            }

                            break;
                        }
                    case ScrollType.Thumbstick:
                        {
                            var currX = GetAxis(xAxis, false);
                            var currY = GetAxis(yAxis, false);

                            scrollDelta = new Vector2(currX, currY) * 5f;

                            break;
                        }
                    default:
                        return Vector2.zero;
                }

                return Vector2.Scale(scrollDelta, scale);
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
            public virtual bool GetPress(ControllerButton button, bool usePrevState = false) { return false; }
            public virtual bool GetPressDown(ControllerButton button) { return false; }
            public virtual bool GetPressUp(ControllerButton button) { return false; }
            public virtual float LastPressDownTime(ControllerButton button) { return 0f; }
            public virtual int ClickCount(ControllerButton button) { return 0; }
            public virtual float GetAxis(ControllerAxis axis, bool usePrevState = false) { return 0f; }
            public virtual Vector2 GetPadAxis(bool usePrevState = false) { return Vector2.zero; }
            public virtual Vector2 GetPadPressVector() { return Vector2.zero; }
            public virtual Vector2 GetPadTouchVector() { return Vector2.zero; }
            public virtual Vector2 GetScrollDelta(ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY) { return Vector2.zero; }

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

                var timeNow = Time.unscaledTime;
                for (ControllerButton button = 0; button < (ControllerButton)CONTROLLER_BUTTON_COUNT; ++button)
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

                        if (timeNow - m_state.LastPressDownTime(button) < clickInterval)
                        {
                            // Click event
                            TryInvokeListener(button, ButtonEventType.Click);
                        }
                    }
                }

                return false;
            }

            public override void AddListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { m_state.AddListener(button, listener, type); }
            public override void RemoveListener(ControllerButton button, Action listener, ButtonEventType type = ButtonEventType.Click) { m_state.RemoveListener(button, listener, type); }
            public override void AddListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { m_state.AddListener(button, listener, type); }
            public override void RemoveListener(ControllerButton button, RoleValueEventListener listener, ButtonEventType type = ButtonEventType.Click) { m_state.RemoveListener(button, listener, type); }
            public override bool GetPress(ControllerButton button, bool usePrevState = false) { return m_state.GetPress(button, usePrevState); }
            public override bool GetPressDown(ControllerButton button) { return m_state.GetPressDown(button); }
            public override bool GetPressUp(ControllerButton button) { return m_state.GetPressUp(button); }
            public override float LastPressDownTime(ControllerButton button) { return m_state.LastPressDownTime(button); }
            public override int ClickCount(ControllerButton button) { return m_state.ClickCount(button); }
            public override float GetAxis(ControllerAxis axis, bool usePrevState = false) { return m_state.GetAxis(axis, usePrevState); }
            public override Vector2 GetPadAxis(bool usePrevState = false) { return m_state.GetPadAxis(usePrevState); }
            public override Vector2 GetPadPressVector() { return m_state.GetPadPressVector(); }
            public override Vector2 GetPadTouchVector() { return m_state.GetPadTouchVector(); }
            public override Vector2 GetScrollDelta(ScrollType scrollType, Vector2 scale, ControllerAxis xAxis = ControllerAxis.PadX, ControllerAxis yAxis = ControllerAxis.PadY) { return m_state.GetScrollDelta(scrollType, scale, xAxis, yAxis); }

            protected override void TryInvokeListener(ControllerButton button, ButtonEventType type)
            {
                if (listeners == null) { return; }
                if (listeners[(int)button] == null) { return; }
                if (listeners[(int)button][(int)type] == null) { return; }
                listeners[(int)button][(int)type].Invoke(m_role, button, type);
            }

            public override void AddListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { listeners = new RoleEventListener<TRole>[CONTROLLER_BUTTON_COUNT][]; }
                if (listeners[(int)button] == null) { listeners[(int)button] = new RoleEventListener<TRole>[BUTTON_EVENT_COUNT]; }
                if (listeners[(int)button][(int)type] == null)
                {
                    listeners[(int)button][(int)type] = listener;
                }
                else
                {
                    listeners[(int)button][(int)type] += listener;
                }
            }

            public override void RemoveListener(ControllerButton button, RoleEventListener<TRole> listener, ButtonEventType type = ButtonEventType.Click)
            {
                if (listeners == null) { return; }
                if (listeners[(int)button] == null) { return; }
                if (listeners[(int)button][(int)type] == null) { return; }
                listeners[(int)button][(int)type] -= listener;
            }
        }
    }
}