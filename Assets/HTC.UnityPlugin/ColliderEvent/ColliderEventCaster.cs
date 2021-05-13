//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections;
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
        Collider lastEnteredCollider { get; }

        Rigidbody rigid { get; }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class ColliderEventCaster : MonoBehaviour, IColliderEventCaster
    {
        private static HashSet<int> s_gos = new HashSet<int>();

        private bool isUpdating;
        private bool isDisabled;

        private StayingCollidersCollection stayingColliders = new StayingCollidersCollection();
        private StayingHandlersCollection hoveredObjects = new StayingHandlersCollection();
        private StayingHandlersCollection lastHoveredObjects = new StayingHandlersCollection();

        private Rigidbody m_rigid;
        private ColliderHoverEventData hoverEventData;
        private Predicate<GameObject> cannotHandlDragAnyMorePredicate = null;

        protected readonly List<ColliderButtonEventData> buttonEventDataList = new List<ColliderButtonEventData>();
        protected readonly List<ColliderAxisEventData> axisEventDataList = new List<ColliderAxisEventData>();

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
            get { return stayingColliders; }
        }

        public Collider lastEnteredCollider
        {
            get { return stayingColliders.LastEnteredCollider; }
        }

        public ColliderHoverEventData HoverEventData
        {
            get { return hoverEventData ?? (hoverEventData = new ColliderHoverEventData(this)); }
            protected set { hoverEventData = value; }
        }

        private Predicate<GameObject> CannotHandlDragAnyMorePredicate
        {
            get { return cannotHandlDragAnyMorePredicate ?? (cannotHandlDragAnyMorePredicate = CannotHandlDragAnymore); }
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

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnyMorePredicate);

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
            else
            {
                stayingColliders.ResetStayingFlags();
            }

            isUpdating = false;
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            stayingColliders.SetColliderStaying(other);
        }

        protected virtual void Update()
        {
            isUpdating = true;

            // process enter & exit
            if (stayingColliders.EnteredCount > 0 || stayingColliders.ExitedCount > 0)
            {
                stayingColliders.ExtractLeavedColliders();

                lastHoveredObjects.SetStayingObjHierarchy(stayingColliders.LastEnteredCollider);
                lastHoveredObjects.ExtractExitHandlers();
                lastHoveredObjects.ResetStayingFlag();

                for (int i = stayingColliders.Count - 1; i >= 0; --i)
                {
                    hoveredObjects.SetStayingObjHierarchy(stayingColliders[i]);
                }
                hoveredObjects.ExtractExitHandlers();
                hoveredObjects.ResetStayingFlag();
            }

            // process button events
            for (int i = 0, imax = buttonEventDataList.Count; i < imax; ++i)
            {
                var eventData = buttonEventDataList[i];
                var handlers = GetButtonHandlers(i);

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnyMorePredicate);

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

                eventData.draggingHandlers.RemoveAll(CannotHandlDragAnyMorePredicate);

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
            hoveredObjects.ExtractExitHandlers();
            lastHoveredObjects.ExtractExitHandlers();

            ExecuteAllEvents();

            stayingColliders.Clear();
            hoveredObjects.ClearStayingObj();
            lastHoveredObjects.ClearStayingObj();
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
            ExcuteAndClearHandlersEvents(hoveredObjects.exitHandlers, HoverEventData, ExecuteColliderEvents.HoverExitHandler);
            ExcuteAndClearHandlersEvents(lastHoveredObjects.exitHandlers, HoverEventData, ExecuteColliderEvents.LastHoverExitHandler);
            ExcuteAndClearHandlersEvents(lastHoveredObjects.enterHandlers, HoverEventData, ExecuteColliderEvents.LastHoverEnterHandler);
            ExcuteAndClearHandlersEvents(hoveredObjects.enterHandlers, HoverEventData, ExecuteColliderEvents.HoverEnterHandler);

            for (int i = buttonEventHandlerList.Count - 1; i >= 0; --i)
            {
                if (buttonEventHandlerList[i] == null) { continue; }

                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].pressEnterHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressEnterHandler);

                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].pressDownHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressDownHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].pressUpHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressUpHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].dragStartHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragStartHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].dragFixedUpdateHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragFixedUpdateHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].dragUpdateHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragUpdateHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].dragEndHandlers, buttonEventDataList[i], ExecuteColliderEvents.DragEndHandler);

                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].dropHandlers, buttonEventDataList[i], ExecuteColliderEvents.DropHandler);
                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].clickHandlers, buttonEventDataList[i], ExecuteColliderEvents.ClickHandler);

                ExcuteAndClearHandlersEvents(buttonEventHandlerList[i].pressExitHandlers, buttonEventDataList[i], ExecuteColliderEvents.PressExitHandler);
            }

            for (int i = axisEventHanderList.Count - 1; i >= 0; --i)
            {
                if (axisEventHanderList[i] == null) { continue; }

                ExcuteAndClearHandlersEvents(axisEventHanderList[i].axisChangedHandlers, axisEventDataList[i], ExecuteColliderEvents.AxisChangedHandler);
            }
        }

        private void ExcuteAndClearHandlersEvents<T>(List<GameObject> handlers, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            if (handlers.Count == 0) { return; }

            for (int i = handlers.Count - 1; i >= 0; --i)
            {
                ExecuteEvents.Execute(handlers[i], eventData, functor);
            }

            handlers.Clear();
        }

        private static void SwapRef<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        private class StayingCollidersCollection : IIndexedSetReadOnly<Collider>
        {
            private int addCount;
            private int stayCount;
            private IndexedTable<Collider, bool> colliderFlags = new IndexedTable<Collider, bool>();
            private Predicate<KeyValuePair<Collider, bool>> isLeavedPredicate;

            public Collider this[int index] { get { return colliderFlags.GetKeyByIndex(index); } }

            public int Count { get { return colliderFlags.Count; } }

            public int EnteredCount { get { return addCount; } }

            public int ExitedCount { get { return colliderFlags.Count - stayCount; } }

            public Collider LastEnteredCollider { get { return colliderFlags.Count > 0 ? colliderFlags.GetKeyByIndex(colliderFlags.Count - 1) : null; } }

            public bool Contains(Collider item) { return colliderFlags.ContainsKey(item); }

            public void CopyTo(Collider[] array, int arrayIndex) { colliderFlags.Keys.CopyTo(array, arrayIndex); }

            public IEnumerator<Collider> GetEnumerator() { return colliderFlags.Keys.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            public int IndexOf(Collider item) { return colliderFlags.IndexOf(item); }

            public void SetColliderStaying(Collider collider)
            {
                var index = colliderFlags.IndexOf(collider);
                if (index < 0)
                {
                    ++addCount;
                    ++stayCount;
                    colliderFlags.Add(collider, true);
                }
                else if (!colliderFlags.GetValueByIndex(index))
                {
                    ++stayCount;
                    colliderFlags.SetValueByIndex(index, true);
                }
            }

            public int ExtractLeavedColliders()
            {
                if (stayCount < colliderFlags.Count)
                {
                    if (isLeavedPredicate == null) { isLeavedPredicate = IsColliderLeaved; }
                    var removedCount = colliderFlags.RemoveAll(isLeavedPredicate);
                    stayCount = colliderFlags.Count;
                    addCount = 0;
                    return removedCount;
                }
                return 0;
            }

            public void ResetStayingFlags()
            {
                for (int i = colliderFlags.Count - 1; i >= 0; --i)
                {
                    colliderFlags.SetValueByIndex(i, false);
                }
                stayCount = 0;
                addCount = 0;
            }

            public void Clear()
            {
                colliderFlags.Clear();
                stayCount = 0;
                addCount = 0;
            }

            private bool IsColliderLeaved(KeyValuePair<Collider, bool> pair) { return !pair.Value; }
        }

        private class StayingHandlersCollection
        {
            public List<GameObject> enterHandlers = new List<GameObject>();
            public List<GameObject> exitHandlers = new List<GameObject>();

            private IndexedTable<GameObject, bool> stayingObjs = new IndexedTable<GameObject, bool>();
            private Predicate<KeyValuePair<GameObject, bool>> isUnsetAndFindExitObjPredicate;

            public int Count { get { return stayingObjs.Count; } }

            public GameObject this[int i] { get { return stayingObjs.GetKeyByIndex(i); } }

            public void SetStayingObjHierarchy(Collider collider) { if (collider != null) { SetStayingObjHierarchy(collider.gameObject); } }

            public void SetStayingObjHierarchy(GameObject obj)
            {
                if (obj == null) { return; }

                for (var tr = obj.transform; tr != null; tr = tr.parent)
                {
                    var trObj = tr.gameObject;

                    var trObjIndex = stayingObjs.IndexOf(trObj);

                    if (trObjIndex < 0)
                    {
                        enterHandlers.Add(trObj);
                        stayingObjs.Add(trObj, true);
                    }
                    else if (!stayingObjs.GetValueByIndex(trObjIndex))
                    {
                        stayingObjs.SetValueByIndex(trObjIndex, true);
                    }
                    else
                    {
                        // skip if root obj is already recorded
                        return;
                    }
                }
            }

            public void ExtractExitHandlers()
            {
                if (isUnsetAndFindExitObjPredicate == null) { isUnsetAndFindExitObjPredicate = IsUnsetAndMarkExitObj; }
                stayingObjs.RemoveAll(isUnsetAndFindExitObjPredicate);
            }

            public void ResetStayingFlag()
            {
                for (int i = stayingObjs.Count - 1; i >= 0; --i) { stayingObjs.SetValueByIndex(i, false); }
            }

            private bool IsUnsetAndMarkExitObj(KeyValuePair<GameObject, bool> pair)
            {
                if (pair.Key == null) { return true; }
                if (pair.Value) { return false; }

                exitHandlers.Add(pair.Key);
                return true;
            }

            public void ClearStayingObj()
            {
                stayingObjs.Clear();
            }
        }
    }
}
