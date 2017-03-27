//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

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
    }

    /// <summary>
    /// Singleton that manage and update controllers input
    /// </summary>
    public partial class ViveInput : MonoBehaviour
    {
        public const int CONTROLLER_BUTTON_COUNT = 7;
        public const int BUTTON_EVENT_COUNT = 4;

        private static ViveInput s_instance = null;
        private static bool s_isApplicationQuitting = false;

        private static readonly uint s_sizeOfControllerStats = (uint)Marshal.SizeOf(typeof(VRControllerState_t));
        private static VRControllerState_t[] s_controllerStats = new VRControllerState_t[ViveRole.MAX_DEVICE_COUNT];

        private float m_clickInterval = 0.3f;

        public static bool Active { get { return s_instance != null; } }

        public static float clickInterval
        {
            get { return Active ? s_instance.m_clickInterval : default(float); }
            set { if (Active) s_instance.m_clickInterval = Mathf.Max(0f, value); }
        }

        public static ViveInput Instance
        {
            get
            {
                Initialize();
                return s_instance;
            }
        }

        public static void Initialize()
        {
            if (Active || s_isApplicationQuitting) { return; }

            var instances = FindObjectsOfType<ViveInput>();
            if (instances.Length > 0)
            {
                s_instance = instances[0];
                if (instances.Length > 1) { Debug.LogWarning("Multiple ViveInput not supported!"); }
            }

            if (!Active)
            {
                s_instance = new GameObject("[ViveInput]").AddComponent<ViveInput>();
            }

            if (Active)
            {
                DontDestroyOnLoad(s_instance.gameObject);
            }
        }

        private void Update()
        {
            if (s_instance != this) { return; }

            var system = OpenVR.System;
            for (uint deviceIndex = 0; deviceIndex < ViveRole.MAX_DEVICE_COUNT; ++deviceIndex)
            {
                if (system == null || !system.GetControllerState(deviceIndex, ref s_controllerStats[deviceIndex], s_sizeOfControllerStats))
                {
                    s_controllerStats[deviceIndex] = default(VRControllerState_t);
                }
            }

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
        }

        private void OnApplicationQuit()
        {
            s_isApplicationQuitting = true;
        }
    }
}