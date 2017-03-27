//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

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
        public int buttonId;
        public IndexedSet<GameObject> pressEnteredObjects = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> draggingHandlers = new IndexedSet<GameObject>();
        public IndexedSet<GameObject> clickingHandlers = new IndexedSet<GameObject>();

        public Vector3 pressPosition { get; set; }
        public Quaternion pressRotation { get; set; }

        public bool isDragging { get { return draggingHandlers.Count > 0; } }

        public bool isPressed { get; set; }

        public ColliderButtonEventData(IColliderEventCaster eventCaster, int buttonId = 0) : base(eventCaster)
        {
            this.buttonId = buttonId;
        }

        public abstract bool GetPress();

        public abstract bool GetPressDown();

        public abstract bool GetPressUp();
    }

    public abstract class ColliderAxisEventData : ColliderEventData
    {
        public enum Dim
        {
            d1,
            d2,
            d3,
            d4,
        }

        public int axisId;

        public bool xUsed = true;
        public bool yUsed = true;
        public bool zUsed = false;
        public bool wUsed = false;
        // raw delta values
        public float xRaw;
        public float yRaw;
        public float zRaw;
        public float wRaw;
        // delta values
        public float x { get { return xUsed ? xRaw : 0f; } set { if (xUsed) xRaw = value; } }
        public float y { get { return yUsed ? yRaw : 0f; } set { if (yUsed) yRaw = value; } }
        public float z { get { return zUsed ? zRaw : 0f; } set { if (zUsed) zRaw = value; } }
        public float w { get { return wUsed ? wRaw : 0f; } set { if (wUsed) wRaw = value; } }

        public Vector2 v2 { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }
        public Vector3 v3 { get { return new Vector3(x, y, z); } set { x = value.x; y = value.y; z = value.z; } }
        public Vector4 v4 { get { return new Vector4(x, y, z, w); } set { x = value.x; y = value.y; z = value.z; w = value.w; } }

        public ColliderAxisEventData(IColliderEventCaster eventCaster, int axisId = 0) : base(eventCaster)
        {
            this.axisId = axisId;
        }

        public ColliderAxisEventData(IColliderEventCaster eventCaster, Dim dimention, int axisId = 0) : base(eventCaster)
        {
            this.axisId = axisId;
            SetDimention(dimention);
        }

        public void SetDimention(Dim dimention)
        {
            switch (dimention)
            {
                case Dim.d1: { xUsed = true; yUsed = false; zUsed = false; wUsed = false; break; }
                case Dim.d2: { xUsed = true; yUsed = true; zUsed = false; wUsed = false; break; }
                case Dim.d3: { xUsed = true; yUsed = true; zUsed = true; wUsed = false; break; }
                case Dim.d4: { xUsed = true; yUsed = true; zUsed = true; wUsed = true; break; }
            }
        }

        public bool IsDimention(Dim dimention)
        {
            switch (dimention)
            {
                case Dim.d1: return xUsed && !yUsed && !zUsed && !wUsed;
                case Dim.d2: return xUsed && yUsed && !zUsed && !wUsed;
                case Dim.d3: return xUsed && yUsed && zUsed && !wUsed;
                case Dim.d4: return xUsed && yUsed && zUsed && wUsed;
                default: return false;
            }
        }

        public abstract bool IsValueChangedThisFrame();
    }
}