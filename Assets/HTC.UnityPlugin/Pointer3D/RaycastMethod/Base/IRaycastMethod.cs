//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public interface IRaycastMethod
    {
        bool enabled { get; }
        void Raycast(Ray ray, float distance, List<RaycastResult> raycastResults);
    }
}