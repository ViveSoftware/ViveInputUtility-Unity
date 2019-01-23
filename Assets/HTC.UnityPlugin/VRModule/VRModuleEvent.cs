//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine.Events;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        [Serializable]
        public class NewPosesEvent : UnityEvent { }
        [Serializable]
        public class NewInputEvent : UnityEvent { }
        [Serializable]
        public class ControllerRoleChangedEvent : UnityEvent { }
        [Serializable]
        public class InputFocusEvent : UnityEvent<bool> { }
        [Serializable]
        public class DeviceConnectedEvent : UnityEvent<uint, bool> { }
        [Serializable]
        public class ActiveModuleChangedEvent : UnityEvent<VRModuleActiveEnum> { }

        public delegate void NewPosesListener();
        public delegate void NesInputListener();
        public delegate void ControllerRoleChangedListener();
        public delegate void InputFocusListener(bool value);
        public delegate void DeviceConnectedListener(uint deviceIndex, bool connected);
        public delegate void ActiveModuleChangedListener(VRModuleActiveEnum activeModule);

        private static NewPosesListener s_onNewPoses;
        private static NesInputListener s_onNewInput;
        private static ControllerRoleChangedListener s_onControllerRoleChanged;
        private static InputFocusListener s_onInputFocus;
        private static DeviceConnectedListener s_onDeviceConnected;
        private static ActiveModuleChangedListener s_onActiveModuleChanged;

        public static event NewPosesListener onNewPoses { add { s_onNewPoses += value; } remove { s_onNewPoses -= value; } } // invoke by manager
        public static event NesInputListener onNewInput { add { s_onNewInput += value; } remove { s_onNewInput -= value; } } // invoke by manager
        public static event ControllerRoleChangedListener onControllerRoleChanged { add { s_onControllerRoleChanged += value; } remove { s_onControllerRoleChanged -= value; } } // invoke by module
        public static event InputFocusListener onInputFocus { add { s_onInputFocus += value; } remove { s_onInputFocus -= value; } } // invoke by module
        public static event DeviceConnectedListener onDeviceConnected { add { s_onDeviceConnected += value; } remove { s_onDeviceConnected -= value; } }// invoke by manager
        public static event ActiveModuleChangedListener onActiveModuleChanged { add { s_onActiveModuleChanged += value; } remove { s_onActiveModuleChanged -= value; } } // invoke by manager

        private static void InvokeNewPosesEvent()
        {
            if (s_onNewPoses != null) { s_onNewPoses(); }
            if (Active) { Instance.m_onNewPoses.Invoke(); }
        }

        private static void InvokeNewInputEvent()
        {
            if (s_onNewInput != null) { s_onNewInput(); }
            if (Active) { Instance.m_onNewInput.Invoke(); }
        }

        private static void InvokeControllerRoleChangedEvent()
        {
            if (s_onControllerRoleChanged != null) { s_onControllerRoleChanged(); }
            if (Active) { Instance.m_onControllerRoleChanged.Invoke(); }
        }

        private static void InvokeInputFocusEvent(bool value)
        {
            if (s_onInputFocus != null) { s_onInputFocus(value); }
            if (Active) { Instance.m_onInputFocus.Invoke(value); }
        }

        private static void InvokeDeviceConnectedEvent(uint deviceIndex, bool connected)
        {
            if (s_onDeviceConnected != null) { s_onDeviceConnected(deviceIndex, connected); }
            if (Active) { Instance.m_onDeviceConnected.Invoke(deviceIndex, connected); }
        }

        private static void InvokeActiveModuleChangedEvent(VRModuleActiveEnum activeModule)
        {
            if (s_onActiveModuleChanged != null) { s_onActiveModuleChanged(activeModule); }
            if (Active) { Instance.m_onActiveModuleChanged.Invoke(activeModule); }
        }
    }
}