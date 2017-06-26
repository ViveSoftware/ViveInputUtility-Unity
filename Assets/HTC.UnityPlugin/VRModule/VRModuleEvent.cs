//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public partial class VRModule : SingletonBehaviour<VRModule>
    {
        [Serializable]
        public class OnNewPosesEvent : UnityEvent { }
        [Serializable]
        public class ControllerRoleChangedEvent : UnityEvent { }
        [Serializable]
        public class InputFocusEvent : UnityEvent<bool> { }
        [Serializable]
        public class DeviceConnectedEvent : UnityEvent<uint, bool> { }
        [Serializable]
        public class ModuleActivatedEvent : UnityEvent<SupportedVRModule> { }
        [Serializable]
        public class ModuleDeactivatedEvent : UnityEvent<SupportedVRModule> { }

        public static OnNewPosesEvent onNewPoses { get { return Instance == null ? null : Instance.m_onNewPoses; } } // invoke by manager
        public static ControllerRoleChangedEvent onControllerRoleChanged { get { return Instance == null ? null : Instance.m_onControllerRoleChanged; } } // invoke by module
        public static InputFocusEvent onInputFocus { get { return Instance == null ? null : Instance.m_onInputFocus; } } // invoke by module
        public static DeviceConnectedEvent onDeviceConnected { get { return Instance == null ? null : Instance.m_onDeviceConnected; } } // invoke by manager
        public static ModuleActivatedEvent onModuleActivatedEvent { get { return Instance == null ? null : Instance.m_moduleActivatedEvent; } } // invoke by manager
        public static ModuleDeactivatedEvent onModuleDeactivatedEvent { get { return Instance == null ? null : Instance.m_moduleDeactivatedEvent; } } // invoke by manager
    }
}