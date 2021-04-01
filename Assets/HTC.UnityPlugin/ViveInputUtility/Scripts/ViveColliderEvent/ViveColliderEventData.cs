//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
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

        public static bool IsViveTriggerAxis(this ColliderAxisEventData eventData)
        {
            if (eventData == null) { return false; }

            return eventData is ViveColliderTriggerAxisEventData;
        }

        public static bool IsViveTriggerAxis(this ColliderAxisEventData eventData, HandRole hand)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerAxisEventData)) { return false; }

            return (eventData as ViveColliderTriggerAxisEventData).viveRole.IsRole(hand);
        }

        public static bool IsViveTriggerAxisEx<TRole>(this ColliderAxisEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerAxisEventData)) { return false; }

            return (eventData as ViveColliderTriggerAxisEventData).viveRole.IsRole(role);
        }

        public static bool TryGetViveTriggerAxisEventData(this ColliderAxisEventData eventData, out ViveColliderTriggerAxisEventData viveEventData)
        {
            viveEventData = null;

            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderTriggerAxisEventData)) { return false; }

            viveEventData = eventData as ViveColliderTriggerAxisEventData;
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

            return (eventData as ViveColliderTriggerAxisEventData).viveRole.IsRole(hand);
        }

        public static bool IsVivePadAxisEx<TRole>(this ColliderAxisEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is ViveColliderPadAxisEventData)) { return false; }

            return (eventData as ViveColliderTriggerAxisEventData).viveRole.IsRole(role);
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
        public ViveColliderEventCaster viveEventCaster { get; private set; }
        public ControllerButton viveButton { get; private set; }

        public ViveRoleProperty viveRole { get { return viveEventCaster.viveRole; } }

        public ViveColliderButtonEventData(ViveColliderEventCaster eventCaster, ControllerButton viveButton, InputButton button) : base(eventCaster, button)
        {
            this.viveEventCaster = eventCaster;
            this.viveButton = viveButton;
        }

        public override bool GetPress() { return ViveInput.GetPressEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressDown() { return ViveInput.GetPressDownEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressUp() { return ViveInput.GetPressUpEx(viveRole.roleType, viveRole.roleValue, viveButton); }
    }

    public class ViveColliderTriggerAxisEventData : ColliderAxisEventData
    {
        public ViveColliderEventCaster viveEventCaster { get; private set; }
        public ViveRoleProperty viveRole { get { return viveEventCaster.viveRole; } }

        public ViveColliderTriggerAxisEventData(ViveColliderEventCaster eventCaster) : base(eventCaster, Dim.D1, InputAxis.Trigger1D)
        {
            viveEventCaster = eventCaster;
        }

        public override Vector4 GetDelta()
        {
            return new Vector4(ViveInput.GetTriggerValueEx(viveRole.roleType, viveRole.roleValue, false) - ViveInput.GetTriggerValueEx(viveRole.roleType, viveRole.roleValue, true), 0f);
        }
    }

    public class ViveColliderPadAxisEventData : ColliderAxisEventData
    {
        public ViveColliderEventCaster viveEventCaster { get; private set; }
        public ViveRoleProperty viveRole { get { return viveEventCaster.viveRole; } }

        public ViveColliderPadAxisEventData(ViveColliderEventCaster eventCaster) : base(eventCaster, Dim.D2, InputAxis.Scroll2D)
        {
            viveEventCaster = eventCaster;
        }

        public override Vector4 GetDelta()
        {
            return ViveInput.GetScrollDelta(viveRole, viveEventCaster.scrollType, viveEventCaster.scrollDeltaScale);
        }
    }
}