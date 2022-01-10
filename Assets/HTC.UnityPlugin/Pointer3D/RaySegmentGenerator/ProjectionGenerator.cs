//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Pointer3D
{
    public class ProjectionGenerator : BaseRaySegmentGenerator
    {
        public float velocity = 2f;
        public Vector3 gravity = Vector3.down;

        private bool isFirstSegment = true;

        public override void ResetSegments()
        {
            isFirstSegment = true;
        }

        public override bool NextSegment(out Vector3 direction, out float distance)
        {
            if (isFirstSegment && velocity > Pointer3DRaycaster.MIN_SEGMENT_DISTANCE)
            {
                isFirstSegment = false;
                direction = raycaster.transform.forward;
                distance = velocity;
            }
            else
            {
                direction = gravity;
                distance = float.PositiveInfinity;
            }

            return true;
        }
    }
}