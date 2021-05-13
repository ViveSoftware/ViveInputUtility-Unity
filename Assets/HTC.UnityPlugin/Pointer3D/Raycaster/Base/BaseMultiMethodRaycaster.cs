//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    [DisallowMultipleComponent]
    public abstract class BaseMultiMethodRaycaster : BaseRaycaster
    {
        protected readonly IndexedSet<IRaycastMethod> methods = new IndexedSet<IRaycastMethod>();
#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            if (GetComponent<PhysicsRaycastMethod>() == null) { gameObject.AddComponent<PhysicsRaycastMethod>(); }
            if (GetComponent<CanvasRaycastMethod>() == null) { gameObject.AddComponent<CanvasRaycastMethod>(); }
        }
#endif
        public void AddRaycastMethod(IRaycastMethod obj)
        {
            methods.AddUnique(obj);
        }

        public void RemoveRaycastMethod(IRaycastMethod obj)
        {
            methods.Remove(obj);
        }

        protected void ForeachRaycastMethods(Ray ray, float distance, List<RaycastResult> resultAppendList)
        {
            var results = ListPool<RaycastResult>.Get();

            for (int i = methods.Count - 1; i >= 0; --i)
            {
                var method = methods[i];
                if (!method.enabled) { continue; }
                method.Raycast(ray, distance, results);
            }

            var comparer = GetRaycasterResultComparer();
            if (comparer != null)
            {
                results.Sort(comparer);
            }

            for (int i = 0, imax = results.Count; i < imax; ++i)
            {
                resultAppendList.Add(results[i]);
            }

            ListPool<RaycastResult>.Release(results);
        }

        protected virtual Comparison<RaycastResult> GetRaycasterResultComparer()
        {
            return Pointer3DInputModule.defaultRaycastComparer;
        }
    }
}