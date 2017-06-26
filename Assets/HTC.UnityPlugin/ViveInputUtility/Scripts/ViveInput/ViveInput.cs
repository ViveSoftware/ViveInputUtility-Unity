//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public enum ButtonEventType
    {
        /// <summary>
        /// Button unpressed at last frame, pressed at this frame
        /// </summary>
        Down,
        /// <summary>
        /// Button pressed at this frame
        /// </summary>
        Press,
        /// <summary>
        /// Button pressed at last frame, unpressed at the frame
        /// </summary>
        Up,
        /// <summary>
        /// Button up at this frame, and last button down time is in certain interval
        /// </summary>
        Click,
    }

    /// <summary>
    /// Defines virtual buttons for Vive controller
    /// </summary>
    public enum ControllerButton
    {
        None = -1,
        Trigger,
        Pad,
        Grip,
        PadTouch,
        Menu,
        /// <summary>
        /// Pressed if trigger button is pressing, unpressed if trigger button is releasing
        /// </summary>
        HairTrigger,
        /// <summary>
        /// Pressed if only trigger button is fully held(tirgger value equals to 1.0f)
        /// </summary>
        FullTrigger,
        HairGrip,
        FullGrip,
    }

    public enum ControllerAxis
    {
        None = -1,
        PadX,
        PadY,
        Trigger,
        Grip,
    }

    public enum ScrollType
    {
        None = -1,
        Auto,
        Trackpad,
        Thumbstick,
    }

    public class RawControllerState
    {
        public readonly bool[] buttonPress = new bool[ViveInput.CONTROLLER_BUTTON_COUNT];
        public readonly float[] axisValue = new float[ViveInput.CONTROLLER_AXIS_COUNT];
    }

    /// <summary>
    /// Singleton that manage and update controllers input
    /// </summary>
    public partial class ViveInput : SingletonBehaviour<ViveInput>
    {
        public static readonly int CONTROLLER_BUTTON_COUNT = EnumUtils.GetMaxValue(typeof(ControllerButton)) + 1;
        public static readonly int CONTROLLER_AXIS_COUNT = EnumUtils.GetMaxValue(typeof(ControllerAxis)) + 1;
        public static readonly int BUTTON_EVENT_COUNT = EnumUtils.GetMaxValue(typeof(ButtonEventType)) + 1;

        private static readonly CtrlState s_defaultState = new CtrlState();
        private static readonly IndexedTable<Type, ICtrlState[]> s_roleStateTable = new IndexedTable<Type, ICtrlState[]>();

        [SerializeField]
        private float m_clickInterval = 0.3f;
        [SerializeField]
        private bool m_dontDestroyOnLoad = true;
        [SerializeField]
        private UnityEvent m_onInputStateUpdated = new UnityEvent();

        public static float clickInterval
        {
            get { return Instance.m_clickInterval; }
            set { Instance.m_clickInterval = Mathf.Max(0f, value); }
        }

        public static UnityEvent onInputStateUpdated { get { return Instance == null ? null : Instance.m_onInputStateUpdated; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_clickInterval = Mathf.Max(m_clickInterval, 0f);
        }
#endif

        protected override void OnSingletonBehaviourInitialized()
        {
            if (m_dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (!IsInstance) { return; }

            for (int i = 0, imax = s_roleStateTable.Count; i < imax; ++i)
            {
                var states = s_roleStateTable.GetValueByIndex(i);
                if (states == null) { continue; }

                foreach (var state in states)
                {
                    if (state == null) { continue; }
                    state.Update();
                }
            }

            if (m_onInputStateUpdated != null) { m_onInputStateUpdated.Invoke(); }
        }

        private static bool IsValidButton(ControllerButton button) { return button >= 0 && (int)button < CONTROLLER_BUTTON_COUNT; }

        private static bool IsValidAxis(ControllerAxis axis) { return axis >= 0 && (int)axis < CONTROLLER_BUTTON_COUNT; }

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

            stateList[roleOffset].Update();
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

            RGCtrolState<TRole>.s_roleStates[roleOffset].Update();
            return RGCtrolState<TRole>.s_roleStates[roleOffset];
        }
    }
}