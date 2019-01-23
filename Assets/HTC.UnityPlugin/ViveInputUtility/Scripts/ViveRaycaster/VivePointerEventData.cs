//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using System;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Vive
{
    public static class VivePointerEventDataExtension
    {
        public static bool IsViveButton(this PointerEventData eventData, HandRole hand)
        {
            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            return (eventData as VivePointerEventData).viveRole.IsRole(hand);
        }

        public static bool IsViveButtonEx<TRole>(this PointerEventData eventData, TRole role)
        {
            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            return (eventData as VivePointerEventData).viveRole.IsRole(role);
        }

        public static bool IsViveButton(this PointerEventData eventData, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            var viveEvent = eventData as VivePointerEventData;
            return viveEvent.viveButton == button;
        }

        public static bool IsViveButton(this PointerEventData eventData, HandRole hand, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            var viveEvent = eventData as VivePointerEventData;
            return viveEvent.viveRole.IsRole(hand) && viveEvent.viveButton == button;
        }

        public static bool IsViveButtonEx<TRole>(this PointerEventData eventData, TRole role, ControllerButton button)
        {
            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            var viveEvent = eventData as VivePointerEventData;
            return viveEvent.viveRole.IsRole(role) && viveEvent.viveButton == button;
        }

        public static bool TryGetViveButtonEventData(this PointerEventData eventData, out VivePointerEventData viveEventData)
        {
            viveEventData = null;

            if (eventData == null) { return false; }

            if (!(eventData is VivePointerEventData)) { return false; }

            viveEventData = eventData as VivePointerEventData;
            return true;
        }
    }

    // Custom PointerEventData implement for Vive controller.
    public class VivePointerEventData : Pointer3DEventData
    {
        public ViveRaycaster viveRaycaster { get; private set; }
        public ControllerButton viveButton { get; private set; }

        public ViveRoleProperty viveRole { get { return viveRaycaster.viveRole; } }

        public VivePointerEventData(ViveRaycaster ownerRaycaster, EventSystem eventSystem, ControllerButton viveButton, InputButton mouseButton) : base(ownerRaycaster, eventSystem)
        {
            this.viveRaycaster = ownerRaycaster;
            this.viveButton = viveButton;
            this.button = mouseButton;
        }

        public override bool GetPress() { return ViveInput.GetPressEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressDown() { return ViveInput.GetPressDownEx(viveRole.roleType, viveRole.roleValue, viveButton); }

        public override bool GetPressUp() { return ViveInput.GetPressUpEx(viveRole.roleType, viveRole.roleValue, viveButton); }
    }
}