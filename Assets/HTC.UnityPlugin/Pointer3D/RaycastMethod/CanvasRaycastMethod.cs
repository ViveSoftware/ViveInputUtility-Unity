//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Pointer3D
{
    [DisallowMultipleComponent]
    public class CanvasRaycastMethod : BaseRaycastMethod
    {
        private static readonly IndexedSet<ICanvasRaycastTarget> canvases = new IndexedSet<ICanvasRaycastTarget>();

        public static IIndexedSetReadOnly<ICanvasRaycastTarget> TargetCanvases { get { return canvases.ReadOnly; } }

        public static bool AddTarget(ICanvasRaycastTarget obj) { return obj == null ? false : canvases.AddUnique(obj); }

        public static bool RemoveTarget(ICanvasRaycastTarget obj) { return obj == null ? false : canvases.Remove(obj); }

        public override void Raycast(Ray ray, float distance, List<RaycastResult> raycastResults)
        {
            var tempCanvases = ListPool<ICanvasRaycastTarget>.Get();
            tempCanvases.AddRange(canvases);
            for (int i = tempCanvases.Count - 1; i >= 0; --i)
            {
                var target = tempCanvases[i];
                if (target == null || !target.enabled) { continue; }
                Raycast(target.canvas, target.ignoreReversedGraphics, ray, distance, raycaster, raycastResults);
            }
            ListPool<ICanvasRaycastTarget>.Release(tempCanvases);
        }

        public static void Raycast(Canvas canvas, bool ignoreReversedGraphics, Ray ray, float distance, Pointer3DRaycaster raycaster, List<RaycastResult> raycastResults)
        {
            if (canvas == null) { return; }

            var eventCamera = raycaster.eventCamera;
            var screenCenterPoint = Pointer3DInputModule.ScreenCenterPoint;
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            // Pointer3DRaycaster should set tje eventCamera to correct position

            for (int i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget) { continue; }

                if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenCenterPoint, eventCamera)) { continue; }

                if (ignoreReversedGraphics && Vector3.Dot(ray.direction, graphic.transform.forward) <= 0f) { continue; }

                if (!graphic.Raycast(screenCenterPoint, eventCamera)) { continue; }

                //var dist = Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction);
                float dist;
                new Plane(graphic.transform.forward, graphic.transform.position).Raycast(ray, out dist);
                if (dist > distance) { continue; }

                raycastResults.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = raycaster,
                    distance = dist,
                    worldPosition = ray.GetPoint(dist),
                    worldNormal = -graphic.transform.forward,
                    screenPosition = screenCenterPoint,
                    index = raycastResults.Count,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder
                });
            }
        }
    }
}