//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public static class ViveColliderEventDataExtension
    {
        public static bool IsViveButton(this ColliderButtonEventData eventData, HandRole hand)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            return (eventData as ViveColliderButtonEventData).viveRole.IsRole(hand);
        }

        public static bool IsViveButtonEx<TRole>(this ColliderButtonEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            return (eventData as ViveColliderButtonEventData).viveRole.IsRole(role);
        }

        public static bool IsViveButton(this ColliderButtonEventData eventData, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            return (eventData as ViveColliderButtonEventData).viveButton == button;
        }

        public static bool IsViveButton(this ColliderButtonEventData eventData, HandRole hand, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            var viveEvent = eventData as ViveColliderButtonEventData;
            return viveEvent.viveRole.IsRole(hand) && viveEvent.viveButton == button;
        }

        public static bool IsViveButtonEx<TRole>(this ColliderButtonEventData eventData, TRole role, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            var viveEvent = eventData as ViveColliderButtonEventData;
            return viveEvent.viveRole.IsRole(role) && viveEvent.viveButton == button;
        }

        public static bool TryGetViveButtonEventData(this ColliderButtonEventData eventData, out ViveColliderButtonEventData viveEventData)
        {
            viveEventData = null;

            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderButtonEventData)) { return false; }

            viveEventData = eventData as ViveColliderButtonEventData;
            return true;
        }

        public static bool IsViveTriggerValue(this ColliderAxisEventData eventData)
        {
            if (eventData == null) { return false; }

            return eventData is ViveColliderTriggerValueEventData;
        }

        public static bool IsViveTriggerValue(this ColliderAxisEventData eventData, HandRole hand)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerValueEventData)) { return false; }

            return (eventData as ViveColliderTriggerValueEventData).viveRole.IsRole(hand);
        }

        public static bool IsViveTriggerValueEx<TRole>(this ColliderAxisEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerValueEventData)) { return false; }

            return (eventData as ViveColliderTriggerValueEventData).viveRole.IsRole(role);
        }

        public static bool TryGetViveTriggerValueEventData(this ColliderAxisEventData eventData, out ViveColliderTriggerValueEventData viveEventData)
        {
            viveEventData = null;

            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerValueEventData)) { return false; }

            viveEventData = eventData as ViveColliderTriggerValueEventData;
            return true;
        }

        public static bool IsVivePadAxis(this ColliderAxisEventData eventData)
        {
            if (eventData == null) { return false; }

            return eventData is ViveColliderPadAxisEventData;
        }

        public static bool IsVivePadAxis(this ColliderAxisEventData eventData, HandRole hand)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderPadAxisEventData)) { return false; }

            return (eventData as ViveColliderTriggerValueEventData).viveRole.IsRole(hand);
        }

        public static bool IsVivePadAxisEx<TRole>(this ColliderAxisEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderPadAxisEventData)) { return false; }

            return (eventData as ViveColliderTriggerValueEventData).viveRole.IsRole(role);
        }

        public static bool TryGetVivePadAxisEventData(this ColliderAxisEventData eventData, out ViveColliderPadAxisEventData viveEventData)
        {
            viveEventData = null;

            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderPadAxisEventData)) { return false; }

            viveEventData = eventData as ViveColliderPadAxisEventData;
            return true;
        }
    }

    public class ViveColliderButtonEventData : ColliderButtonEventData
    {
        [Obsolete]
        public HandRole hand;

        public readonly ViveRoleProperty viveRole;
        public readonly ControllerButton viveButton;

        public ViveColliderButtonEventData(IColliderEventCaster eventCaster, ViveRoleProperty role, ControllerButton viveButton, int buttonId = 0) : base(eventCaster, buttonId)
        {
            viveRole = role;
            this.viveButton = viveButton;
        }

        [Obsolete]
        public ViveColliderButtonEventData(IColliderEventCaster eventCaster, HandRole hand, ControllerButton viveButton, int buttonId = 0) : base(eventCaster, buttonId)
        {
            viveRole = ViveRoleProperty.New(hand);
            this.viveButton = viveButton;
        }

        public override bool GetPress() { return ViveInput.GetPressEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressDown() { return ViveInput.GetPressDownEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressUp() { return ViveInput.GetPressUpEx(viveRole.roleType, viveRole.roleValue, viveButton); }
    }

    public class ViveColliderTriggerValueEventData : ColliderAxisEventData
    {
        [Obsolete]
        public HandRole hand;

        public readonly ViveRoleProperty viveRole;

        public ViveColliderTriggerValueEventData(IColliderEventCaster eventCaster, ViveRoleProperty role, int axisId = 0) : base(eventCaster, Dim.d1, axisId)
        {
            viveRole = role;
        }

        [Obsolete]
        public ViveColliderTriggerValueEventData(IColliderEventCaster eventCaster, HandRole hand, int axisId = 0) : base(eventCaster, Dim.d1, axisId)
        {
            viveRole = ViveRoleProperty.New(hand);
        }

        public override bool IsValueChangedThisFrame()
        {
            xRaw = ViveInput.GetTriggerValueEx(viveRole.roleType, viveRole.roleValue, false) - ViveInput.GetTriggerValueEx(viveRole.roleType, viveRole.roleValue, true);
            return !Mathf.Approximately(xRaw, 0f);
        }

        public float GetCurrentValue()
        {
            return ViveInput.GetTriggerValueEx(viveRole.roleType, viveRole.roleValue);
        }
    }

    public class ViveColliderPadAxisEventData : ColliderAxisEventData
    {
        [Obsolete]
        public HandRole hand;

        public readonly ViveRoleProperty viveRole;

        public ViveColliderPadAxisEventData(IColliderEventCaster eventCaster, ViveRoleProperty role, int axisId = 0) : base(eventCaster, Dim.d2, axisId)
        {
            viveRole = role;
        }

        [Obsolete]
        public ViveColliderPadAxisEventData(IColliderEventCaster eventCaster, HandRole hand, int axisId = 0) : base(eventCaster, Dim.d2, axisId)
        {
            viveRole = ViveRoleProperty.New(hand);
        }

        public override bool IsValueChangedThisFrame()
        {
            v2 = ViveInput.GetPadTouchDeltaEx(viveRole.roleType, viveRole.roleValue);
            return !Mathf.Approximately(v2.sqrMagnitude, 0f);
        }

        public Vector2 GetCurrentValue()
        {
            return ViveInput.GetPadTouchAxisEx(viveRole.roleType, viveRole.roleValue);
        }
    }
}