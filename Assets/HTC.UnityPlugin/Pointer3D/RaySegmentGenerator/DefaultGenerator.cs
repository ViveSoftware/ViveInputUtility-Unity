//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Pointer3D
{
    [Obsolete]
    public class DefaultGenerator : BaseRaySegmentGenerator
    {
        public override void ResetSegments() { }

        public override bool NextSegment(out Vector3 direction, out float distance)
        {
            direction = transform.forward;
            distance = float.PositiveInfinity;

            return true;
        }
    }
}