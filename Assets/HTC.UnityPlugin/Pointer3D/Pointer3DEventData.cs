//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public static class Pointer3DEventDataExtension
    {
        public static Pointer3DRaycaster GetRaycaster3D(this PointerEventData eventData)
        {
            if (!(eventData is Pointer3DEventData)) { return null; }

            var eventData3D = eventData as Pointer3DEventData;

            return eventData3D.raycaster;
        }

        public static bool TryGetRaycaster3D(this PointerEventData eventData, out Pointer3DRaycaster raycaster)
        {
            raycaster = null;

            if (!(eventData is Pointer3DEventData)) { return false; }

            var eventData3D = eventData as Pointer3DEventData;
            raycaster = eventData3D.raycaster;
            return true;
        }

        public static TRaycaster3D GetRaycaster3D<TRaycaster3D>(this PointerEventData eventData) where TRaycaster3D : Pointer3DRaycaster
        {
            if (!(eventData is Pointer3DEventData)) { return null; }

            var eventData3D = eventData as Pointer3DEventData;
            if (!(eventData3D.raycaster is TRaycaster3D)) { return null; }

            return eventData3D.raycaster as TRaycaster3D;
        }

        public static bool TryGetRaycaster3D<TRaycaster3D>(this PointerEventData eventData, out TRaycaster3D raycaster) where TRaycaster3D : Pointer3DRaycaster
        {
            raycaster = null;

            if (!(eventData is Pointer3DEventData)) { return false; }

            var eventData3D = eventData as Pointer3DEventData;
            if (!(eventData3D.raycaster is TRaycaster3D)) { return false; }

            raycaster = eventData3D.raycaster as TRaycaster3D;
            return true;
        }
    }

    public class Pointer3DEventData : PointerEventData
    {
        public readonly Pointer3DRaycaster raycaster;

        public Vector3 position3D;
        public Quaternion rotation;

        public Vector3 position3DDelta;
        public Quaternion rotationDelta;

        public Vector3 pressPosition3D;
        public Quaternion pressRotation;

        public float pressDistance;
        public GameObject pressEnter;
        public bool pressPrecessed;

        public Pointer3DEventData(Pointer3DRaycaster ownerRaycaster, EventSystem eventSystem) : base(eventSystem)
        {
            raycaster = ownerRaycaster;
            Pointer3DInputModule.AssignPointerId(this);
        }

        public virtual bool GetPress() { return false; }

        public virtual bool GetPressDown() { return false; }

        public virtual bool GetPressUp() { return false; }

        public override string ToString()
        {
            var str = string.Empty;
            str += "eligibleForClick: " + eligibleForClick + "\n";
            str += "pointerEnter: " + Pointer3DInputModule.PrintGOPath(pointerEnter) + "\n";
            str += "pointerPress: " + Pointer3DInputModule.PrintGOPath(pointerPress) + "\n";
            str += "lastPointerPress: " + Pointer3DInputModule.PrintGOPath(lastPress) + "\n";
            str += "pressEnter: " + Pointer3DInputModule.PrintGOPath(pressEnter) + "\n";
            str += "pointerDrag: " + Pointer3DInputModule.PrintGOPath(pointerDrag) + "\n";
            return str;
        }
    }
}