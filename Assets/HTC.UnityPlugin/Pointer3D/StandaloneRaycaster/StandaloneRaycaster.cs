//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    [AddComponentMenu("HTC/Pointer3D/Standalone Raycaster")]
    public class StandaloneRaycaster : Pointer3DRaycaster
    {
        protected override void Start()
        {
            base.Start();
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.Fire1, PointerEventData.InputButton.Left));
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.Fire2, PointerEventData.InputButton.Middle));
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.Fire3, PointerEventData.InputButton.Right));
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.MouseLeft, PointerEventData.InputButton.Left));
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.MouseMiddle, PointerEventData.InputButton.Middle));
            buttonEventDataList.Add(new StandaloneEventData(this, EventSystem.current, StandaloneEventData.StandaloneButton.MouseRight, PointerEventData.InputButton.Right));
        }

        public override Vector2 GetScrollDelta()
        {
            return Input.mouseScrollDelta;
        }
    }
}