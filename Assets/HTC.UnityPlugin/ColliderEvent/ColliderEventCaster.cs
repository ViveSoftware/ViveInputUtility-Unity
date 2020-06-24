//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.ColliderEvent
{
    public interface IColliderEventCaster
    {
        GameObject gameObject { get; }
        Transform transform { get; }
        MonoBehaviour monoBehaviour { get; }

        IIndexedSetReadOnly<Collider> enteredColliders { get; }

        Rigidbody rigid { get; }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class ColliderEventCaster : MonoBehaviour, IColliderEventCaster
    {
        private static HashSet<int> s_gos = new HashSet<int>();

        private bool isUpdating;
        private bool isDisabled;

        private IndexedSet<Collider> stayingColliders = new IndexedSet<Collider>();
        private IndexedSet<GameObject> hoveredObjects = new IndexedSet<GameObject>();

        private Rigidbody m_rigid;
        private ColliderHoverEventData hoverEventData;

        protected readonly List<ColliderButtonEventData> buttonEventDataList = new List<ColliderButtonEventData>();
        protected readonly List<ColliderAxisEventData> axisEventDataList = new List<ColliderAxisEventData>();

        private List<GameObject> hoverEnterHandlers = new List<GameObject>();
        private List<GameObject> hoverExitHandlers = new List<GameObject>();

        protected class ButtonHandlers
        {
            public List<GameObject> pressEnterHandlers = new List<GameObject>();
            public List<GameObject> pressExitHandlers = new List<GameObject>();
            public List<GameObject> pressDownHandlers = new List<GameObject>();
            public List<GameObject> pressUpHandlers = new List<GameObject>();
            public List<GameObject> clickHandlers = new List<GameObject>();
            public List<GameObject> dragStartHandlers = new List<GameObject>();
            public List<GameObject> dragFixedUpdateHandlers = new List<GameObject>();
            public List<GameObject> dragUpdateHandlers = new List<GameObject>();
            public List<GameObject> dragEndHandlers = new List<GameObject>();
            public List<GameObject> dropHandlers = new List<GameObject>();
        }

        protected class AxisHandlers
        {
            public List<GameObject> axisChangedHandlers = new List<GameObject>();
        }

        private List<ButtonHandlers> buttonEventHandlerList = new List<ButtonHandlers>();
        private List<AxisHandlers> axisEventHanderList = new List<AxisHandlers>();

        public MonoBehaviour monoBehaviour
        {
            get { return this; }
        }

        public Rigidbody rigid
        {
            get { return m_rigid ?? (m_rigid = GetComponent<Rigidbody>()); }
        }

        public IIndexedSetReadOnly<Collider> enteredColliders
        {
            get { return stayingColliders.ReadOnly; }
        }

        public ColliderHoverEventData HoverEventData
        {
            get { return hoverEventData ?? (hoverEventData = new ColliderHoverEventData(this)); }
            protected set { hoverEventData = value; }
        }

        private bool CannotHandlDragAnymore(GameObject handler)
        {
            return !ExecuteEvents.CanHandleEvent<IColliderEventDragStartHandler>(handler);
        }

        protected virtual void OnEnable()
        {
            isDisabled = false;
        }

        protected virtual void FixedUpdate()
        {
            isUpdating = true;

            // fixed dragging
            for (int i = 0, imax = buttonEventDataList.Count; i < imax; ++i)
            {
                var eventData = buttonEventDataList[i];
                var handlers = GetButtonHandlers(i);

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnymore);

                if (!eventData.isPressed) { continue; }

                for (int j = eventData.draggingHandlers.Count - 1; j >= 0; --j)
                {
                    handlers.dragFixedUpdateHandlers.Add(eventData.draggingHandlers[j]);
                }
            }

            ExecuteAllEvents();

            if (isDisabled)
            {
                CleanUp();
            }

            stayingColliders.Clear();

            isUpdating = false;
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            stayingColliders.AddUnique(other);
        }

        protected virtual void Update()
        {
            isUpdating = true;

            // process enter
            var hoveredObjectsPrev = hoveredObjects;
            hoveredObjects = IndexedSetPool<GameObject>.Get();

            for (int i = stayingColliders.Count - 1; i >= 0; --i)
            {
                var collider = stayingColliders[i];

                if (collider == null) { continue; }

                // travel from collider's gameObject to its root
                for (var tr = collider.transform; !ReferenceEquals(tr, null); tr = tr.parent)
                {
                    var go = tr.gameObject;

                    if (!hoveredObjects.AddUnique(go)) { break; } // hit traveled gameObject, break and travel from the next collider

                    if (hoveredObjectsPrev.Remove(go)) { continue; } // gameObject already existed in last frame, no need to execute enter event

                    hoverEnterHandlers.Add(go);
                }
            }

            // process leave
            for (int i = hoveredObjectsPrev.Count - 1; i >= 0; --i)
            {
                hoverExitHandlers.Add(hoveredObjectsPrev[i]);
            }

            IndexedSetPool<GameObject>.Release(hoveredObjectsPrev);

            // process button events
            for (int i = 0, imax = buttonEventDataList.Count; i < imax; ++i)
            {
                var eventData = buttonEventDataList[i];
                var handlers = GetButtonHandlers(i);

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnymore);

                // process button press
                if (!eventData.isPressed)
                {
                    if (eventData.GetPress())
                    {
                        ProcessPressDown(eventData, handlers);
                        ProcessPressing(eventData, handlers);
                    }
                }
                else
                {
                    if (eventData.GetPress())
                    {
                        ProcessPressing(eventData, handlers);
                    }
                    else
                    {
                        ProcessPressUp(eventData, handlers);
                    }
                }

                // process pressed button enter/exit
                if (eventData.isPressed)
                {
                    var pressEnteredObjectsPrev = eventData.pressEnteredObjects;
                    eventData.pressEnteredObjects = IndexedSetPool<GameObject>.Get();

                    for (int j = hoveredObjects.Count - 1; j >= 0; --j)
                    {
                        eventData.pressEnteredObjects.Add(hoveredObjects[j]);

                        if (pressEnteredObjectsPrev.Remove(hoveredObjects[j])) { continue; } // gameObject already existed in last frame, no need to execute enter event

                        handlers.pressEnterHandlers.Add(hoveredObjects[j]);
                    }

                    for (int j = pressEnteredObjectsPrev.Count - 1; j >= 0; --j)
                    {
                        eventData.clickingHandlers.Remove(pressEnteredObjectsPrev[j]); // remove the obj from clicking obj if it leaved

                        handlers.pressExitHandlers.Add(pressEnteredObjectsPrev[j]);
                    }

                    IndexedSetPool<GameObject>.Release(pressEnteredObjectsPrev);
                }
                else
                {
                    for (int j = eventData.pressEnteredObjects.Count - 1; j >= 0; --j)
                    {
                        handlers.pressExitHandlers.Add(eventData.pressEnteredObjects[j]);
                    }

                    eventData.pressEnteredObjects.Clear();
                }
            }

            // process axis events
            for (int i = 0, imax = axisEventDataList.Count; i < imax; ++i)
            {
                var eventData = axisEventDataList[i];

                if ((eventData.v4 = eventData.GetDelta()) == Vector4.zero) { continue; }

                var handlers = GetAxisHandlers(i);

                GetEventHandlersFromHoveredColliders<IColliderEventAxisChangedHandler>(handlers.axisChangedHandlers);
            }

            ExecuteAllEvents();

            if (isDisabled)
            {
                CleanUp();
            }

            isUpdating = false;
        }

        protected void ProcessPressDown(ColliderButtonEventData eventData, ButtonHandlers handlers)
        {
            eventData.isPressed = true;
            eventData.pressPosition = transform.position;
            eventData.pressRotation = transform.rotation;

            for (int i = stayingColliders.Count - 1; i >= 0; --i)
            {
                if (stayingColliders[i] != null) { eventData.pressedRawObjects.AddUnique(stayingColliders[i].gameObject); }
            }

            // press down
            GetEventHandlersFromHoveredColliders<IColliderEventPressDownHandler>(eventData.pressedHandlers, handlers.pressDownHandlers);
            // click start
            GetEventHandlersFromHoveredColliders<IColliderEventClickHandler>(eventData.clickingHandlers);
            // drag start
            GetEventHandlersFromHoveredColliders<IColliderEventDragStartHandler>(eventData.draggingHandlers, handlers.dragStartHandlers);
        }

        protected void ProcessPressing(ColliderButtonEventData eventData, ButtonHandlers handlers)
        {
            // dragging
            handlers.dragUpdateHandlers.AddRange(eventData.draggingHandlers);
        }

        protected void ProcessPressUp(ColliderButtonEventData eventData, ButtonHandlers handlers)
        {
            IndexedSet<GameObject> tmp;
            eventData.isPressed = false;

            tmp = eventData.lastPressedRawObjects;
            eventData.lastPressedRawObjects = eventData.pressedRawObjects;
            eventData.pressedRawObjects = tmp;

            // press up
            handlers.pressUpHandlers.AddRange(eventData.pressedHandlers);

            tmp = eventData.lastPressedHandlers;
            eventData.lastPressedHandlers = eventData.pressedHandlers;
            eventData.pressedHandlers = tmp;

            // drag end
            handlers.dragEndHandlers.AddRange(eventData.draggingHandlers);
            // drop
            if (eventData.isDragging)
            {
                GetEventHandlersFromHoveredColliders<IColliderEventDropHandler>(handlers.dropHandlers);
            }

            // click end (execute only if pressDown handler and pressUp handler are the same)
            GetMatchedEventHandlersFromHoveredColliders<IColliderEventClickHandler>(h => eventData.clickingHandlers.Remove(h), handlers.clickHandlers);

            eventData.pressedRawObjects.Clear();
            eventData.pressedHandlers.Clear();
            eventData.clickingHandlers.Clear();
            eventData.draggingHandlers.Clear();
        }

        protected virtual void OnDisable()
        {
            isDisabled = true;

            if (!isUpdating)
            {
                CleanUp();
            }
        }

        private void CleanUp()
        {
            // release all
            for (int i = 0, imax = buttonEventDataList.Count; i < imax; ++i)
            {
                var eventData = buttonEventDataList[i];
                var handlers = GetButtonHandlers(i);

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnymore);

                if (eventData.isPressed)
                {
                    ProcessPressUp(eventData, handlers);
                }

                for (int j = eventData.pressEnteredObjects.Count - 1; j >= 0; --j)
                {
                    handlers.pressExitHandlers.Add(eventData.pressEnteredObjects[j]);
                }
            }

            // exit all
            for (int i = hoveredObjects.Count - 1; i >= 0; --i)
            {
                hoverExitHandlers.Add(hoveredObjects[i]);
            }

            hoveredObjects.Clear();

            stayingColliders.Clear();

            ExecuteAllEvents();
        }

        private ButtonHandlers GetButtonHandlers(int i)
        {
            while (i >= buttonEventHandlerList.Count) { buttonEventHandlerList.Add(null); }
            return buttonEventHandlerList[i] ?? (buttonEventHandlerList[i] = new ButtonHandlers());
        }

        private AxisHandlers GetAxisHandlers(int i)
        {
            while (i >= axisEventHanderList.Count) { axisEventHanderList.Add(null); }
            return axisEventHanderList[i] ?? (axisEventHanderList[i] = new AxisHandlers());
        }

        private void GetEventHandlersFromHoveredColliders<T>(IList<GameObject> appendHandler, IList<GameObject> appendHandler2 = null) where T : IEventSystemHandler
        {
            GetMatchedEventHandlersFromHoveredColliders<T>(null, appendHandler, appendHandler2);
        }

        private void GetMatchedEventHandlersFromHoveredColliders<T>(System.Predicate<GameObject> match, IList<GameObject> appendHandler, IList<GameObject> appendHandler2 = null) where T : IEventSystemHandler
        {
            for (int i = stayingColliders.Count - 1; i >= 0; --i)
            {
                var collider = stayingColliders[i];

                if (collider == null) { continue; }

                var handler = ExecuteEvents.GetEventHandler<T>(collider.gameObject);

                if (ReferenceEquals(handler, null)) { continue; }

                if (!s_gos.Add(handler.GetInstanceID())) { continue; }

                if (match != null && !match(handler)) { continue; }

                if (appendHandler != null) { appendHandler.Add(handler); }
                if (appendHandler2 != null) { appendHandler2.Add(handler); }
            }

            s_gos.Clear();
        }

        private void ExecuteAllEvents()
        {
            ExcuteHandlersEvents(hoverEnterHandlers, HoverEventData, ExecuteColliderEvents.HoverEnterHandler);

            for (int i = buttonEventHandlerList.Count - 1; i >= 0; --i)
            {
                if (buttonEventHandlerList[i] == null) { continue; }

                ExcuteHandlersEvents(buttonEventHandlerList[i].pressEnterHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressEnterHandler);

                ExcuteHandlersEvents(buttonEventHandlerList[i].pressDownHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressDownHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].pressUpHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressUpHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].dragStartHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragStartHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].dragFixedUpdateHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragFixedUpdateHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].dragUpdateHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragUpdateHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].dragEndHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragEndHandler);

                ExcuteHandlersEvents(buttonEventHandlerList[i].dropHandlers, buttonEventDataList[i], ExecuteColliderEvents.DropHandler);
                ExcuteHandlersEvents(buttonEventHandlerList[i].clickHandlers, buttonEventDataList[i], ExecuteColliderEvents.ClickHandler);

                ExcuteHandlersEvents(buttonEventHandlerList[i].pressExitHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressExitHandler);
            }

            for (int i = axisEventHanderList.Count - 1; i >= 0; --i)
            {
                if (axisEventHanderList[i] == null) { continue; }

                ExcuteHandlersEvents(axisEventHanderList[i].axisChangedHandlers, axisEventDataList[i], ExecuteColliderEvents.AxisChangedHandler);
            }

            ExcuteHandlersEvents(hoverExitHandlers, HoverEventData, ExecuteColliderEvents.HoverExitHandler);
        }

        private void ExcuteHandlersEvents<T>(List<GameObject> handlers, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            if (handlers.Count == 0) { return; }

            for (int i = handlers.Count - 1; i >= 0; --i)
            {
                ExecuteEvents.Execute(handlers[i], eventData, functor);
            }

            handlers.Clear();
        }
    }
}