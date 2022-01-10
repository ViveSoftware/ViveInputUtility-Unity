//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.ColliderEvent
{
    public static class ColliderEventDataExtension
    {
        public static TEventCaster GetEventCaster<TEventCaster>(this ColliderEventData eventData) where TEventCaster : MonoBehaviour, IColliderEventCaster
        {
            if (!(eventData.eventCaster is TEventCaster)) { return null; }

            return eventData.eventCaster as TEventCaster;
        }

        public static bool TryGetEventCaster<TEventCaster>(this ColliderEventData eventData, out TEventCaster eventCaster) where TEventCaster : MonoBehaviour, IColliderEventCaster
        {
            eventCaster = null;

            if (!(eventData.eventCaster is TEventCaster)) { return false; }

            eventCaster = eventData.eventCaster as TEventCaster;
            return true;
        }
    }

    public class ColliderEventData : BaseEventData
    {
        public readonly IColliderEventCaster eventCaster;

        public ColliderEventData(IColliderEventCaster eventCaster) : base(null)
        {
            this.eventCaster = eventCaster;
        }
    }

    public class ColliderHoverEventData : ColliderEventData
    {
        public ColliderHoverEventData(IColliderEventCaster eventCaster) : base(eventCaster) { }
    }

    public abstract class ColliderButtonEventData : ColliderEventData
    {
        public enum InputButton
        {
            None = -1,
            Trigger,
            PadOrStick,
            GripOrHandTrigger,
            FunctionKey,
        }

        public IndexedSet<GameObject> pressEnteredObjects = new IndexedSet<GameObject>(); // Includes full entered objects hierorchy
        public IndexedSet<GameObject> pressedRawObjects = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> lastPressedRawObjects = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> pressedHandlers = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> lastPressedHandlers = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> draggingHandlers = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> clickingHandlers = new IndexedSet<GameObject>();

        public InputButton button { get; private set; }
        public Vector3 pressPosition { get; set; }
        public Quaternion pressRotation { get; set; }

        public bool isDragging { get { return draggingHandlers.Count > 0; } }

        public bool isPressed { get; set; }

        public ColliderButtonEventData(IColliderEventCaster eventCaster, InputButton button = 0) : base(eventCaster)
        {
            this.button = button;
        }

        public abstract bool GetPress();

        public abstract bool GetPressDown();

        public abstract bool GetPressUp();
    }

    public abstract class ColliderAxisEventData : ColliderEventData
    {
        public enum InputAxis
        {
            Scroll2D,
            Trigger1D,
        }

        public enum Dim
        {
            D1,
            D2,
            D3,
            D4,
        }

        // raw delta values
        private float m_x;
        private float m_y;
        private float m_z;
        private float m_w;

        public InputAxis axis { get; private set; }
        public Dim dimention { get; private set; }

        // delta values
        public float x { get { return dimention >= Dim.D1 ? m_x : 0f; } set { if (dimention >= Dim.D1) m_x = value; } }
        public float y { get { return dimention >= Dim.D2 ? m_y : 0f; } set { if (dimention >= Dim.D2) m_y = value; } }
        public float z { get { return dimention >= Dim.D3 ? m_z : 0f; } set { if (dimention >= Dim.D3) m_z = value; } }
        public float w { get { return dimention >= Dim.D4 ? m_w : 0f; } set { if (dimention >= Dim.D4) m_w = value; } }

        public Vector2 v2 { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }
        public Vector3 v3 { get { return new Vector3(x, y, z); } set { x = value.x; y = value.y; z = value.z; } }
        public Vector4 v4 { get { return new Vector4(x, y, z, w); } set { x = value.x; y = value.y; z = value.z; w = value.w; } }

        public ColliderAxisEventData(IColliderEventCaster eventCaster, Dim dimention, InputAxis axis = 0) : base(eventCaster)
        {
            this.axis = axis;
            this.dimention = dimention;
        }

        public abstract Vector4 GetDelta();
    }
}