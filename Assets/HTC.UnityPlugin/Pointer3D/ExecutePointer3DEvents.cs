//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public static class ExecutePointer3DEvents
    {
        public static readonly ExecuteEvents.EventFunction<IPointer3DPressEnterHandler> PressEnterHandler = Execute;
        private static void Execute(IPointer3DPressEnterHandler handler, BaseEventData eventData)
        {
            handler.OnPointer3DPressEnter(ExecuteEvents.ValidateEventData<Pointer3DEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IPointer3DPressExitHandler> PressExitHandler = Execute;
        private static void Execute(IPointer3DPressExitHandler handler, BaseEventData eventData)
        {
            handler.OnPointer3DPressExit(ExecuteEvents.ValidateEventData<Pointer3DEventData>(eventData));
        }
    }
}