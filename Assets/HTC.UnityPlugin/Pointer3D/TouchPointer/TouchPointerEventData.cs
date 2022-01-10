//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public class TouchPointerEventData : Pointer3DEventData
    {
        public TouchPointerRaycaster touchPointerRaycaster { get; private set; }

        public TouchPointerEventData(TouchPointerRaycaster ownerRaycaster, EventSystem eventSystem, InputButton btn) : base(ownerRaycaster, eventSystem)
        {
            touchPointerRaycaster = ownerRaycaster;
            button = btn;
        }

        public override bool GetPress()
        {
            var hitRange = touchPointerRaycaster.GetButtonHitRange(button);
            return touchPointerRaycaster.CurrentFrameHitRange <= hitRange;
        }

        public override bool GetPressDown()
        {
            var hitRange = touchPointerRaycaster.GetButtonHitRange(button);
            return touchPointerRaycaster.PreviousFrameHitRange > hitRange && touchPointerRaycaster.CurrentFrameHitRange <= hitRange;
        }

        public override bool GetPressUp()
        {
            var hitRange = touchPointerRaycaster.GetButtonHitRange(button);
            return touchPointerRaycaster.PreviousFrameHitRange <= hitRange && touchPointerRaycaster.CurrentFrameHitRange > hitRange;
        }
    }
}