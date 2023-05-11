//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public interface IPointer3DPressEnterHandler : IEventSystemHandler
    {
        void OnPointer3DPressEnter(Pointer3DEventData eventData);
    }

    public interface IPointer3DPressExitHandler : IEventSystemHandler
    {
        void OnPointer3DPressExit(Pointer3DEventData eventData);
    }
}