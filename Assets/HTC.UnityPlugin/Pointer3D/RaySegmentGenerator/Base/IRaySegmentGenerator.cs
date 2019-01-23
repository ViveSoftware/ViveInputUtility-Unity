//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Pointer3D
{
    public interface IRaySegmentGenerator
    {
        bool enabled { get; set; }
        
        void ResetSegments();
        bool NextSegment(out Vector3 direction, out float distance);
    }
}