//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public class TouchPointerRaycaster : Pointer3DRaycaster
    {
        [SerializeField]
        private float mouseButtonLeftRange = 0.02f;
        [SerializeField]
        private float mouseButtonRightRange = 0f;
        [SerializeField]
        private float mouseButtonMiddleRange = 0f;

        private float prevHitRange = float.MaxValue;
        private float currHitRange = float.MaxValue;

        public float MouseButtonLeftRange { get { return mouseButtonLeftRange; } set { mouseButtonLeftRange = value; } }
        public float MouseButtonRightRange { get { return mouseButtonRightRange; } set { mouseButtonRightRange = value; } }
        public float MouseButtonMiddleRange { get { return mouseButtonMiddleRange; } set { mouseButtonMiddleRange = value; } }
        public float PreviousFrameHitRange { get { return prevHitRange; } }
        public float CurrentFrameHitRange { get { return currHitRange; } }

        public float GetButtonHitRange(PointerEventData.InputButton btn)
        {
            switch (btn)
            {
                default:
                case PointerEventData.InputButton.Left: return mouseButtonLeftRange;
                case PointerEventData.InputButton.Right: return mouseButtonRightRange;
                case PointerEventData.InputButton.Middle: return mouseButtonMiddleRange;
            }
        }

        protected override void Start()
        {
            base.Start();
            buttonEventDataList.Add(new TouchPointerEventData(this, EventSystem.current, PointerEventData.InputButton.Left));
            buttonEventDataList.Add(new TouchPointerEventData(this, EventSystem.current, PointerEventData.InputButton.Right));
            buttonEventDataList.Add(new TouchPointerEventData(this, EventSystem.current, PointerEventData.InputButton.Middle));
        }

        public override void Raycast()
        {
            base.Raycast();

            prevHitRange = currHitRange;

            var hitResult = FirstRaycastResult();
            currHitRange = hitResult.isValid ? hitResult.distance : float.MaxValue;
        }
    }
}