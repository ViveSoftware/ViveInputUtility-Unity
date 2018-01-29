//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
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

        // classic buttons
        System = 14,
        Menu = 4,
        MenuTouch = 7,
        Trigger = 0, // on:0.55 off:0.45
        TriggerTouch = 8, // on:0.25 off:0.20
        Pad = 1,
        PadTouch = 3,
        Grip = 2,
        GripTouch = 9,
        CapSenseGrip = 10, // on:1.00 off:0.90 // Knuckles, Oculus Touch only
        CapSenseGripTouch = 11, // on:0.25 off:0.20 // Knuckles, Oculus Touch only
        AKey = 12, // Oculus Touch only, RightHandA or LeftHandX pressed
        AKeyTouch = 13, // Oculus Touch only, RightHandA or LeftHandX touched

        // button alias
        OuterFaceButton = Menu, // 7
        OuterFaceButtonTouch = MenuTouch, // 9
        InnerFaceButton = Grip, // 2
        InnerFaceButtonTouch = GripTouch, // 11
        [HideInInspector]
        Axis0 = Pad,
        [HideInInspector]
        Axis1 = Trigger,
        [HideInInspector]
        Axis2 = CapSenseGrip,
        [HideInInspector]
        Axis3 = 14,
        [HideInInspector]
        Axis4 = 15,
        [HideInInspector]
        Axis0Touch = PadTouch,
        [HideInInspector]
        Axis1Touch = TriggerTouch,
        [HideInInspector]
        Axis2Touch = CapSenseGripTouch,
        [HideInInspector]
        Axis3Touch = 16,
        [HideInInspector]
        Axis4Touch = 17,

        // virtual buttons
        HairTrigger = 5, // Pressed if trigger button is pressing, unpressed if trigger button is releasing
        FullTrigger = 6, // on:1.00 off:1.00
    }

    public enum ControllerAxis
    {
        None = -1,
        PadX,
        PadY,
        Trigger,
        CapSenseGrip, // Knuckles, Oculus Touch only
        IndexCurl, // Knuckles only
        MiddleCurl, // Knuckles only
        RingCurl, // Knuckles only
        PinkyCurl, // Knuckles only
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
        private static UnityAction s_onUpdate;

        [SerializeField]
        private float m_clickInterval = 0.3f;
        [SerializeField]
        private bool m_dontDestroyOnLoad = true;
        [SerializeField]
        private UnityEvent m_onUpdate = new UnityEvent();

        public static float clickInterval
        {
            get { return Instance.m_clickInterval; }
            set { Instance.m_clickInterval = Mathf.Max(0f, value); }
        }

        public static event UnityAction onUpdate { add { s_onUpdate += value; } remove { s_onUpdate -= value; } }

        static ViveInput()
        {
            SetDefaultInitGameObjectGetter(VRModule.GetInstanceGameObject);
        }

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

            if (s_onUpdate != null) { s_onUpdate(); }
            if (m_onUpdate != null) { m_onUpdate.Invoke(); }
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

            var roleOffset = info.RoleValueToRoleOffset(roleValue);
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

            var roleOffset = info.RoleToRoleOffset(role);
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