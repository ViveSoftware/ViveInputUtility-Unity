//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public class Pointer3DInputModule : BaseInputModule
    {
        private static Pointer3DInputModule instance;
        private static bool isApplicationQuitting = false;

        private static readonly IndexedSet<Pointer3DRaycaster> raycasters = new IndexedSet<Pointer3DRaycaster>();
        private static IndexedSet<Pointer3DRaycaster> processingRaycasters = new IndexedSet<Pointer3DRaycaster>();
        private static int validEventDataId = PointerInputModule.kFakeTouchesId - 1;

#if UNITY_5_5_OR_NEWER
        private bool m_hasFocus;
#endif
        private int m_processedFrame;

        // Pointer3DInputModule has it's own RaycasterManager and Pointer3DRaycaster doesn't share with other input modules.
        // So coexist with other input modules is by default and reasonable?
        public bool coexist = true;
        [NonSerialized]
        [Obsolete("Use Pointer3DRaycaster.dragThreshold instead")]
        public float dragThreshold = 0.02f;
        [NonSerialized]
        [Obsolete("Use Pointer3DRaycaster.clickInterval instead")]
        public float clickInterval = 0.3f;

        public static Vector2 ScreenCenterPoint { get { return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); } }

        public static bool Active { get { return instance != null; } }

        public static Pointer3DInputModule Instance
        {
            get
            {
                Initialize();
                return instance;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule()) { return false; }
            // if coexist with other inputmodule is enabled, tell EventSystem not to active and let other module active first
            return !coexist;
        }
#if UNITY_5_5_OR_NEWER
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            m_hasFocus = hasFocus;
        }

        protected virtual void Update()
        {
            // EventSystem is paused when application lost focus, so force ProcessRaycast here
            if (isActiveAndEnabled && !m_hasFocus)
            {
                if (EventSystem.current.currentInputModule == this || coexist)
                {
                    ProcessRaycast();
                }
            }
        }
#endif
        public override void UpdateModule()
        {
            Initialize();
            if (isActiveAndEnabled && EventSystem.current.currentInputModule != this && coexist)
            {
                ProcessRaycast();
            }
        }

        public static void Initialize()
        {
            if (Active || isApplicationQuitting) { return; }

            var instances = FindObjectsOfType<Pointer3DInputModule>();
            if (instances.Length > 0)
            {
                instance = instances[0];
                if (instances.Length > 1) { Debug.LogWarning("Multiple Pointer3DInputModule not supported!"); }
            }

            if (!Active)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystem = FindObjectOfType<EventSystem>();
                }
                if (eventSystem == null)
                {
                    eventSystem = new GameObject("[EventSystem]").AddComponent<EventSystem>();
                }
                if (eventSystem == null)
                {
                    Debug.LogWarning("EventSystem not found or create fail!");
                    return;
                }

                instance = eventSystem.gameObject.AddComponent<Pointer3DInputModule>();
            }
        }

        public static void AssignPointerId(Pointer3DEventData eventData)
        {
            eventData.pointerId = validEventDataId--;
        }

        public override void Process()
        {
            Initialize();
            if (isActiveAndEnabled)
            {
                ProcessRaycast();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Active && processingRaycasters.Count == 0)
            {
                for (var i = raycasters.Count - 1; i >= 0; --i)
                {
                    instance.CleanUpRaycaster(raycasters[i]);
                }
            }
        }

        public static readonly Comparison<RaycastResult> defaultRaycastComparer = RaycastComparer;
        private static int RaycastComparer(RaycastResult lhs, RaycastResult rhs)
        {
            if (lhs.module != rhs.module)
            {
                if (lhs.module.eventCamera != null && rhs.module.eventCamera != null && lhs.module.eventCamera.depth != rhs.module.eventCamera.depth)
                {
                    // need to reverse the standard compareTo
                    if (lhs.module.eventCamera.depth < rhs.module.eventCamera.depth) { return 1; }
                    if (lhs.module.eventCamera.depth == rhs.module.eventCamera.depth) { return 0; }
                    return -1;
                }

                if (lhs.module.sortOrderPriority != rhs.module.sortOrderPriority)
                {
                    return rhs.module.sortOrderPriority.CompareTo(lhs.module.sortOrderPriority);
                }

                if (lhs.module.renderOrderPriority != rhs.module.renderOrderPriority)
                {
                    return rhs.module.renderOrderPriority.CompareTo(lhs.module.renderOrderPriority);
                }
            }

            if (lhs.sortingLayer != rhs.sortingLayer)
            {
                // Uses the layer value to properly compare the relative order of the layers.
                var rid = SortingLayer.GetLayerValueFromID(rhs.sortingLayer);
                var lid = SortingLayer.GetLayerValueFromID(lhs.sortingLayer);
                return rid.CompareTo(lid);
            }

            if (lhs.sortingOrder != rhs.sortingOrder)
            {
                return rhs.sortingOrder.CompareTo(lhs.sortingOrder);
            }

            if (!Mathf.Approximately(lhs.distance, rhs.distance))
            {
                return lhs.distance.CompareTo(rhs.distance);
            }

            if (lhs.depth != rhs.depth)
            {
                return rhs.depth.CompareTo(lhs.depth);
            }

            return lhs.index.CompareTo(rhs.index);
        }

        public static void AddRaycaster(Pointer3DRaycaster raycaster)
        {
            if (raycaster == null) { return; }

            Initialize();
            raycasters.AddUnique(raycaster);
        }

        public static void RemoveRaycaster(Pointer3DRaycaster raycaster)
        {
            if (!raycasters.Remove(raycaster)) { return; }

            if (!processingRaycasters.Contains(raycaster) && Active)
            {
                Instance.CleanUpRaycaster(raycaster);
            }
        }

        [Obsolete("Use RemoveRaycaster instead")]
        public static void RemoveRaycasters(Pointer3DRaycaster raycaster) { RemoveRaycaster(raycaster); }

        protected void CleanUpRaycaster(Pointer3DRaycaster raycaster)
        {
            if (raycaster == null) { return; }

            var hoverEventData = raycaster.HoverEventData;

            // buttons event
            for (int i = 0, imax = raycaster.ButtonEventDataList.Count; i < imax; ++i)
            {
                var buttonEventData = raycaster.ButtonEventDataList[i];
                if (buttonEventData == null) { continue; }

                buttonEventData.Reset();

                if (buttonEventData.pressProcessed)
                {
                    ProcessPressUp(buttonEventData);
                    HandlePressExitAndEnter(buttonEventData, null);
                }

                if (buttonEventData.pointerEnter != null)
                {
                    if (buttonEventData == hoverEventData)
                    {
                        // perform exit event only for hover event data
                        HandlePointerExitAndEnter(buttonEventData, null);
                    }
                    else
                    {
                        buttonEventData.pointerEnter = null;
                    }
                }
            }

            raycaster.CleanUpRaycast();

            for (int i = 0, imax = raycaster.ButtonEventDataList.Count; i < imax; ++i)
            {
                raycaster.ButtonEventDataList[i].pointerPressRaycast = default(RaycastResult);
                raycaster.ButtonEventDataList[i].pointerCurrentRaycast = default(RaycastResult);
            }
        }

        protected virtual void ProcessRaycast()
        {
            if (m_processedFrame == Time.frameCount) { return; }
            m_processedFrame = Time.frameCount;

            // use another list to iterate raycasters
            // incase that raycasters may changed during this process cycle
            for (int i = 0, imax = raycasters.Count; i < imax; ++i)
            {
                var r = raycasters[i];

                if (r != null)
                {
                    processingRaycasters.Add(r);
                }
            }

            for (var i = processingRaycasters.Count - 1; i >= 0; --i)
            {
                var raycaster = processingRaycasters[i];
                if (raycaster == null) { continue; }

                raycaster.Raycast();
                var result = raycaster.FirstRaycastResult();

                // prepare raycaster value
                var scrollDelta = raycaster.GetScrollDelta();
                var raycasterPos = raycaster.transform.position;
                var raycasterRot = raycaster.transform.rotation;

                var hoverEventData = raycaster.HoverEventData;
                if (hoverEventData == null) { continue; }

                // gen shared data and put in hover event
                hoverEventData.Reset();
                hoverEventData.delta = Vector2.zero;
                hoverEventData.scrollDelta = scrollDelta;
                hoverEventData.position = ScreenCenterPoint;
                hoverEventData.pointerCurrentRaycast = result;

                hoverEventData.position3DDelta = raycasterPos - hoverEventData.position3D;
                hoverEventData.position3D = raycasterPos;
                hoverEventData.rotationDelta = Quaternion.Inverse(hoverEventData.rotation) * raycasterRot;
                hoverEventData.rotation = raycasterRot;

                // copy data to other button event
                for (int j = 0, jmax = raycaster.ButtonEventDataList.Count; j < jmax; ++j)
                {
                    var buttonEventData = raycaster.ButtonEventDataList[j];
                    if (buttonEventData == null || buttonEventData == hoverEventData) { continue; }

                    buttonEventData.Reset();
                    buttonEventData.delta = Vector2.zero;
                    buttonEventData.scrollDelta = scrollDelta;
                    buttonEventData.position = ScreenCenterPoint;
                    buttonEventData.pointerCurrentRaycast = result;

                    buttonEventData.position3DDelta = hoverEventData.position3DDelta;
                    buttonEventData.position3D = hoverEventData.position3D;
                    buttonEventData.rotationDelta = hoverEventData.rotationDelta;
                    buttonEventData.rotation = hoverEventData.rotation;
                }

                ProcessPress(hoverEventData);
                ProcessMove(hoverEventData);
                ProcessDrag(hoverEventData);

                // other buttons event
                for (int j = 1, jmax = raycaster.ButtonEventDataList.Count; j < jmax; ++j)
                {
                    var buttonEventData = raycaster.ButtonEventDataList[j];
                    if (buttonEventData == null || buttonEventData == hoverEventData) { continue; }

                    buttonEventData.pointerEnter = hoverEventData.pointerEnter;

                    ProcessPress(buttonEventData);
                    ProcessDrag(buttonEventData);
                }

                // scroll event
                if (result.isValid && !Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
                {
                    var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(result.gameObject);
                    ExecuteEvents.ExecuteHierarchy(scrollHandler, hoverEventData, ExecuteEvents.scrollHandler);
                }
            }

            if (isActiveAndEnabled)
            {
                for (var i = processingRaycasters.Count - 1; i >= 0; --i)
                {
                    var r = processingRaycasters[i];
                    if (!raycasters.Contains(r))
                    {
                        CleanUpRaycaster(r);
                    }
                }
            }
            else
            {
                for (var i = processingRaycasters.Count - 1; i >= 0; --i)
                {
                    CleanUpRaycaster(processingRaycasters[i]);
                }
            }

            processingRaycasters.Clear();
        }

        protected virtual void ProcessMove(PointerEventData eventData)
        {
            var hoverGO = eventData.pointerCurrentRaycast.gameObject;
            if (eventData.pointerEnter != hoverGO)
            {
                HandlePointerExitAndEnter(eventData, hoverGO);
            }
        }

        protected virtual void ProcessPress(Pointer3DEventData eventData)
        {
            if (eventData.GetPress())
            {
                if (!eventData.pressProcessed)
                {
                    ProcessPressDown(eventData);
                }

                HandlePressExitAndEnter(eventData, eventData.pointerCurrentRaycast.gameObject);
            }
            else if (eventData.pressProcessed)
            {
                ProcessPressUp(eventData);
                HandlePressExitAndEnter(eventData, null);
            }
        }

        protected void ProcessPressDown(Pointer3DEventData eventData)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            eventData.pressProcessed = true;
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.useDragThreshold = true;
            eventData.pressPosition = eventData.position;
            eventData.pressPosition3D = eventData.position3D;
            eventData.pressRotation = eventData.rotation;
            eventData.pressDistance = eventData.pointerCurrentRaycast.distance;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, eventData);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
            {
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
            }

            var time = Time.unscaledTime;

            if (newPressed == eventData.lastPress)
            {
                if (eventData.raycaster != null && time < (eventData.clickTime + eventData.raycaster.clickInterval))
                {
                    ++eventData.clickCount;
                }
                else
                {
                    eventData.clickCount = 1;
                }

                eventData.clickTime = time;
            }
            else
            {
                eventData.clickCount = 1;
            }

            eventData.pointerPress = newPressed;
            eventData.rawPointerPress = currentOverGo;

            eventData.clickTime = time;

            // Save the drag handler as well
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (eventData.pointerDrag != null)
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
            }
        }

        protected void ProcessPressUp(Pointer3DEventData eventData)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
            }
            else if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
            }

            eventData.pressProcessed = false;
            eventData.eligibleForClick = false;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;

            if (eventData.pointerDrag != null && eventData.dragging)
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);
            }

            eventData.dragging = false;
            eventData.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != eventData.pointerEnter)
            {
                HandlePointerExitAndEnter(eventData, null);
                HandlePointerExitAndEnter(eventData, currentOverGo);
            }
        }

        protected bool ShouldStartDrag(Pointer3DEventData eventData)
        {
            if (!eventData.useDragThreshold || eventData.raycaster == null) { return true; }
            var currentPos = eventData.position3D + (eventData.rotation * Vector3.forward) * eventData.pressDistance;
            var pressPos = eventData.pressPosition3D + (eventData.pressRotation * Vector3.forward) * eventData.pressDistance;
            var threshold = eventData.raycaster.dragThreshold;
            return (currentPos - pressPos).sqrMagnitude >= threshold * threshold;
        }

        protected void ProcessDrag(Pointer3DEventData eventData)
        {
            var moving = !Mathf.Approximately(eventData.position3DDelta.sqrMagnitude, 0f) || !Mathf.Approximately(Quaternion.Angle(Quaternion.identity, eventData.rotationDelta), 0f);

            if (moving && eventData.pointerDrag != null && !eventData.dragging && ShouldStartDrag(eventData))
            {
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                eventData.dragging = true;
            }

            // Drag notification
            if (eventData.dragging && moving && eventData.pointerDrag != null)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (eventData.pointerPress != eventData.pointerDrag)
                {
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                    eventData.eligibleForClick = false;
                    eventData.pointerPress = null;
                    eventData.rawPointerPress = null;
                }
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
            }
        }

        protected static void HandlePressExitAndEnter(Pointer3DEventData eventData, GameObject newEnterTarget)
        {
            if (eventData.pressEnter == newEnterTarget) { return; }

            var oldTarget = eventData.pressEnter == null ? null : eventData.pressEnter.transform;
            var newTarget = newEnterTarget == null ? null : newEnterTarget.transform;
            var commonRoot = default(Transform);

            for (var t = oldTarget; t != null; t = t.parent)
            {
                if (newTarget != null && newTarget.IsChildOf(t))
                {
                    commonRoot = t;
                    break;
                }
                else
                {
                    ExecuteEvents.Execute(t.gameObject, eventData, ExecutePointer3DEvents.PressExitHandler);
                }
            }

            eventData.pressEnter = newEnterTarget;

            for (var t = newTarget; t != commonRoot; t = t.parent)
            {
                ExecuteEvents.Execute(t.gameObject, eventData, ExecutePointer3DEvents.PressEnterHandler);
            }
        }

        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if (eventSystem != null && selectHandlerGO != eventSystem.currentSelectedGameObject)
            {
                eventSystem.SetSelectedGameObject(null, pointerEvent);
            }
        }

        public bool SendUpdateEventToSelectedObject()
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) { return false; }

            var data = GetBaseEventData();
            ExecuteEvents.Execute(selected, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        public bool SendSubmitEventToSelectedObject(bool submit, bool cencel)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) { return false; }

            var data = GetBaseEventData();
            if (submit) { ExecuteEvents.Execute(selected, data, ExecuteEvents.submitHandler); }
            if (cencel) { ExecuteEvents.Execute(selected, data, ExecuteEvents.cancelHandler); }
            return data.used;
        }

        public bool SendMoveEventToSelectedObject(float x, float y, float moveDeadZone)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) { return false; }

            var data = GetAxisEventData(x, y, moveDeadZone);
            ExecuteEvents.Execute(selected, data, ExecuteEvents.moveHandler);
            return data.used;
        }

        public static string PrintGOPath(GameObject go)
        {
            var str = string.Empty;

            if (go != null)
            {
                for (var t = go.transform; t != null; t = t.parent)
                {
                    if (!string.IsNullOrEmpty(str)) { str = "." + str; }
                    str = t.name + str;
                }
            }

            return str;
        }

        public override string ToString()
        {
            var str = string.Empty;
            if (raycasters.Count == 0)
            {
                str += "No raycaster registered";
            }
            else
            {
                for (int i = 0, imax = raycasters.Count; i < imax; ++i)
                {
                    var raycaster = raycasters[i];
                    if (raycaster == null) { continue; }

                    str += "<b>Raycaster: [" + i + "]</b>\n";
                    str += raycaster.ToString() + "\n";
                }
            }

            return str;
        }
    }
}