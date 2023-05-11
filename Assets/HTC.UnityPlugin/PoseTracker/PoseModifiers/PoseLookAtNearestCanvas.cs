//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class PoseLookAtNearestCanvas : BasePoseModifier
    {
        [Tooltip("Use origin tracked pose up if set to false")]
        public bool useCanvasUp = false;
        [Tooltip("Find canvas include all canvases with CanvasRaycastTarget component")]
        public bool useCanvasRaycastTarget = true;
        public Transform[] canvases;

        private static List<Transform> tempTargets = new List<Transform>();

        public override void ModifyPose(ref RigidPose pose, bool useLocal)
        {
            if (useCanvasRaycastTarget && CanvasRaycastMethod.TargetCanvases.Count > 0)
            {
                for (int i = 0, imax = CanvasRaycastMethod.TargetCanvases.Count; i < imax; ++i)
                {
                    tempTargets.Add(CanvasRaycastMethod.TargetCanvases[i].canvas.transform);
                }
            }

            if (canvases != null && canvases.Length > 0)
            {
                tempTargets.AddRange(canvases);
            }

            if (tempTargets.Count > 0)
            {
                var worldPos = GetWroldPosePos(ref pose, useLocal);
                var nearestCanvasIndex = -1;
                var nearestCanvasDist = float.MaxValue;
                for (int i = 0, imax = tempTargets.Count; i < imax; ++i)
                {
                    var c = tempTargets[i];
                    var dist = Mathf.Abs(Vector3.Dot(-c.forward, worldPos - c.position));
                    if (dist < nearestCanvasDist)
                    {
                        nearestCanvasIndex = i;
                        nearestCanvasDist = dist;
                    }
                }

                if (nearestCanvasIndex >= 0)
                {
                    var c = tempTargets[nearestCanvasIndex];
                    var f = c.forward;
                    var u = useCanvasUp ? c.up : GetWroldPoseRot(ref pose, useLocal) * Vector3.up;

                    pose.rot = Quaternion.LookRotation(f, u);
                    if (useLocal && transform.parent != null) { pose.rot = Quaternion.Inverse(transform.parent.rotation) * pose.rot; }
                }

                tempTargets.Clear();
            }
        }

        private Vector3 GetWroldPosePos(ref RigidPose pose, bool useLocal)
        {
            if (useLocal && transform.parent != null)
            {
                return transform.parent.TransformPoint(pose.pos);
            }
            else
            {
                return pose.pos;
            }
        }

        private Quaternion GetWroldPoseRot(ref RigidPose pose, bool useLocal)
        {
            if (useLocal && transform.parent != null)
            {
                return transform.parent.rotation * pose.rot;
            }
            else
            {
                return pose.rot;
            }
        }
    }
}