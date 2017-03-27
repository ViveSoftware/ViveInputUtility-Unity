//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.ColliderEvent
{
    public static class ExecuteColliderEvents
    {
        public static readonly ExecuteEvents.EventFunction<IColliderEventHoverEnterHandler> HoverEnterHandler = Execute;
        private static void Execute(IColliderEventHoverEnterHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventHoverEnter(ExecuteEvents.ValidateEventData<ColliderHoverEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventHoverExitHandler> HoverExitHandler = Execute;
        private static void Execute(IColliderEventHoverExitHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventHoverExit(ExecuteEvents.ValidateEventData<ColliderHoverEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventPressDownHandler> PressDownHandler = Execute;
        private static void Execute(IColliderEventPressDownHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventPressDown(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventPressUpHandler> PressUpHandler = Execute;
        private static void Execute(IColliderEventPressUpHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventPressUp(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventPressEnterHandler> PressEnterHandler = Execute;
        private static void Execute(IColliderEventPressEnterHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventPressEnter(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventPressExitHandler> PressExitHandler = Execute;
        private static void Execute(IColliderEventPressExitHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventPressExit(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventClickHandler> ClickHandler = Execute;
        private static void Execute(IColliderEventClickHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventClick(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventDragStartHandler> DragStartHandler = Execute;
        private static void Execute(IColliderEventDragStartHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventDragStart(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventDragFixedUpdateHandler> DragFixedUpdateHandler = Execute;
        private static void Execute(IColliderEventDragFixedUpdateHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventDragFixedUpdate(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventDragUpdateHandler> DragUpdateHandler = Execute;
        private static void Execute(IColliderEventDragUpdateHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventDragUpdate(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventDragEndHandler> DragEndHandler = Execute;
        private static void Execute(IColliderEventDragEndHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventDragEnd(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventDropHandler> DropHandler = Execute;
        private static void Execute(IColliderEventDropHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventDrop(ExecuteEvents.ValidateEventData<ColliderButtonEventData>(eventData));
        }

        public static readonly ExecuteEvents.EventFunction<IColliderEventAxisChangedHandler> AxisChangedHandler = Execute;
        private static void Execute(IColliderEventAxisChangedHandler handler, BaseEventData eventData)
        {
            handler.OnColliderEventAxisChanged(ExecuteEvents.ValidateEventData<ColliderAxisEventData>(eventData));
        }
    }
}