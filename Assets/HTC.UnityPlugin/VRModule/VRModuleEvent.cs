//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

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
        public class ControllerRoleChangedEvent : UnityEvent { }
        [Serializable]
        public class InputFocusEvent : UnityEvent<bool> { }
        [Serializable]
        public class DeviceConnectedEvent : UnityEvent<uint, bool> { }
        [Serializable]
        public class ActiveModuleChangedEvent : UnityEvent<VRModuleActiveEnum> { }

        private readonly static NewPosesEvent s_onNewPoses = new NewPosesEvent();
        private readonly static ControllerRoleChangedEvent s_onControllerRoleChanged = new ControllerRoleChangedEvent();
        private readonly static InputFocusEvent s_onInputFocus = new InputFocusEvent();
        private readonly static DeviceConnectedEvent s_onDeviceConnected = new DeviceConnectedEvent();
        private readonly static ActiveModuleChangedEvent s_onActiveModuleChanged = new ActiveModuleChangedEvent();

        public static NewPosesEvent onNewPoses { get { Initialize(); return s_onNewPoses; } } // invoke by manager
        public static ControllerRoleChangedEvent onControllerRoleChanged { get { Initialize(); return s_onControllerRoleChanged; } } // invoke by module
        public static InputFocusEvent onInputFocus { get { Initialize(); return s_onInputFocus; } } // invoke by module
        public static DeviceConnectedEvent onDeviceConnected { get { Initialize(); return s_onDeviceConnected; } } // invoke by manager
        public static ActiveModuleChangedEvent onActiveModuleChangedEvent { get { Initialize(); return s_onActiveModuleChanged; } } // invoke by manager

        private static void InvokeNewPosesEvent()
        {
            s_onNewPoses.Invoke();
            if (Active) { Instance.m_onNewPoses.Invoke(); }
        }

        private static void InvokeControllerRoleChangedEvent()
        {
            s_onControllerRoleChanged.Invoke();
            if (Active) { Instance.m_onControllerRoleChanged.Invoke(); }
        }

        private static void InvokeInputFocusEvent(bool value)
        {
            s_onInputFocus.Invoke(value);
            if (Active) { Instance.m_onInputFocus.Invoke(value); }
        }

        private static void InvokeDeviceConnectedEvent(uint deviceIndex, bool connected)
        {
            s_onDeviceConnected.Invoke(deviceIndex, connected);
            if (Active) { Instance.m_onDeviceConnected.Invoke(deviceIndex, connected); }
        }

        private static void InvokeActiveModuleChangedEvent(VRModuleActiveEnum activeModule)
        {
            s_onActiveModuleChanged.Invoke(activeModule);
            if (Active) { Instance.m_onActiveModuleChanged.Invoke(activeModule); }
        }
    }
}