//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.ColliderEvent
{
    public interface IColliderEventHoverEnterHandler : IEventSystemHandler
    {
        void OnColliderEventHoverEnter(ColliderHoverEventData eventData);
    }

    public interface IColliderEventHoverExitHandler : IEventSystemHandler
    {
        void OnColliderEventHoverExit(ColliderHoverEventData eventData);
    }

    public interface IColliderEventPressDownHandler : IEventSystemHandler
    {
        void OnColliderEventPressDown(ColliderButtonEventData eventData);
    }

    public interface IColliderEventPressUpHandler : IEventSystemHandler
    {
        void OnColliderEventPressUp(ColliderButtonEventData eventData);
    }

    public interface IColliderEventPressEnterHandler : IEventSystemHandler
    {
        void OnColliderEventPressEnter(ColliderButtonEventData eventData);
    }

    public interface IColliderEventPressExitHandler : IEventSystemHandler
    {
        void OnColliderEventPressExit(ColliderButtonEventData eventData);
    }

    public interface IColliderEventClickHandler : IEventSystemHandler
    {
        void OnColliderEventClick(ColliderButtonEventData eventData);
    }

    public interface IColliderEventDragStartHandler : IEventSystemHandler
    {
        void OnColliderEventDragStart(ColliderButtonEventData eventData);
    }

    public interface IColliderEventDragFixedUpdateHandler : IEventSystemHandler
    {
        void OnColliderEventDragFixedUpdate(ColliderButtonEventData eventData);
    }

    public interface IColliderEventDragUpdateHandler : IEventSystemHandler
    {
        void OnColliderEventDragUpdate(ColliderButtonEventData eventData);
    }

    public interface IColliderEventDragEndHandler : IEventSystemHandler
    {
        void OnColliderEventDragEnd(ColliderButtonEventData eventData);
    }

    public interface IColliderEventDropHandler : IEventSystemHandler
    {
        void OnColliderEventDrop(ColliderButtonEventData eventData);
    }

    public interface IColliderEventAxisChangedHandler : IEventSystemHandler
    {
        void OnColliderEventAxisChanged(ColliderAxisEventData eventData);
    }
}