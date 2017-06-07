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

        private static OnNewPosesEvent s_onNewPoses = new OnNewPosesEvent();
        private static ControllerRoleChangedEvent s_onControllerRoleChanged = new ControllerRoleChangedEvent();
        private static InputFocusEvent s_onInputFocus = new InputFocusEvent();
        private static DeviceConnectedEvent s_onDeviceConnected = new DeviceConnectedEvent();
        private static ModuleActivatedEvent s_moduleActivatedEvent = new ModuleActivatedEvent();
        private static ModuleDeactivatedEvent s_moduleDeactivatedEvent = new ModuleDeactivatedEvent();

        public static OnNewPosesEvent onNewPoses { get { Initialize(); return s_onNewPoses; } } // invoke by manager
        public static ControllerRoleChangedEvent onControllerRoleChanged { get { Initialize(); return s_onControllerRoleChanged; } } // invoke by module
        public static InputFocusEvent onInputFocus { get { Initialize(); return s_onInputFocus; } } // invoke by module
        public static DeviceConnectedEvent onDeviceConnected { get { Initialize(); return s_onDeviceConnected; } } // invoke by manager
        public static ModuleActivatedEvent onModuleActivatedEvent { get { Initialize(); return s_moduleActivatedEvent; } } // invoke by manager
        public static ModuleDeactivatedEvent onModuleDeactivatedEvent { get { Initialize(); return s_moduleDeactivatedEvent; } } // invoke by manager
    }
}