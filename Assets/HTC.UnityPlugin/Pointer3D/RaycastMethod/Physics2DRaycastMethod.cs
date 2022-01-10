//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public class Physics2DRaycastMethod : PhysicsRaycastMethod
    {
        private static readonly RaycastHit2D[] hits = new RaycastHit2D[64];

        public override void Raycast(Ray ray, float distance, List<RaycastResult> raycastResults)
        {
            var hitCount = Physics2D.GetRayIntersectionNonAlloc(ray, hits, distance, RaycastMask);

            for (int i = 0; i < hitCount; ++i)
            {
                var sr = hits[i].collider.gameObject.GetComponent<SpriteRenderer>();

                raycastResults.Add(new RaycastResult
                {
                    gameObject = hits[i].collider.gameObject,
                    module = raycaster,
                    distance = Vector3.Distance(ray.origin, hits[i].transform.position),
                    worldPosition = hits[i].point,
                    worldNormal = hits[i].normal,
                    screenPosition = Pointer3DInputModule.ScreenCenterPoint,
                    index = raycastResults.Count,
                    sortingLayer = sr != null ? sr.sortingLayerID : 0,
                    sortingOrder = sr != null ? sr.sortingOrder : 0
                });
            }
        }
    }
}