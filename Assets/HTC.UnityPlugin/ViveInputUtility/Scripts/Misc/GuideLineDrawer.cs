//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using UnityEngine;

public class GuideLineDrawer : MonoBehaviour
{
    public const float MIN_SEGMENT_LENGTH = 0.01f;

    public Vector3 gravityDirection = Vector3.down;
    public bool showOnHitOnly;
    public float segmentLength = 0.05f;

    public Pointer3DRaycaster raycaster;
    public LineRenderer lineRenderer;

#if UNITY_EDITOR
    protected virtual void Reset()
    {
        for (var tr = transform; raycaster == null && tr != null; tr = tr.parent)
        {
            raycaster = tr.GetComponentInChildren<Pointer3DRaycaster>();
        }

        if (lineRenderer == null) { lineRenderer = GetComponentInChildren<LineRenderer>(); }
        if (lineRenderer == null && raycaster != null) { lineRenderer = gameObject.AddComponent<LineRenderer>(); }
        if (lineRenderer != null)
        {
#if UNITY_5_5_OR_NEWER
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0f;
#else
            lineRenderer.SetWidth(0.01f, 0f);
#endif
        }
    }
#endif
    protected virtual void LateUpdate()
    {
        var result = raycaster.FirstRaycastResult();
        if (showOnHitOnly && !result.isValid)
        {
            lineRenderer.enabled = false;
            return;
        }

        var points = raycaster.BreakPoints;
        var pointCount = points.Count;

        if (pointCount < 2)
        {
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = false;

        var startPoint = points[0];
        var endPoint = points[pointCount - 1];

        if (pointCount == 2)
        {
#if UNITY_5_6_OR_NEWER
            lineRenderer.positionCount = 2;
#elif UNITY_5_5_OR_NEWER
            lineRenderer.numPositions = 2;
#else
            lineRenderer.SetVertexCount(2);
#endif
            lineRenderer.SetPosition(0, transform.InverseTransformPoint(startPoint));
            lineRenderer.SetPosition(1, transform.InverseTransformPoint(endPoint));
        }
        else
        {
            var systemY = gravityDirection;
            var systemX = endPoint - startPoint;
            var systemZ = default(Vector3);

            Vector3.OrthoNormalize(ref systemY, ref systemX, ref systemZ);

            var initialV = Vector3.ProjectOnPlane(points[1] - points[0], systemZ); // initial projectile direction
            var initialVx = Vector3.Dot(initialV, systemX);
            var initialVy = Vector3.Dot(initialV, systemY);
            var initSlope = initialVy / initialVx;

            var approachV = endPoint - startPoint;
            var approachVx = Vector3.Dot(approachV, systemX);
            var approachVy = Vector3.Dot(approachV, systemY);

            var lenx = Mathf.Max(segmentLength, MIN_SEGMENT_LENGTH);
            var segments = Mathf.Max(Mathf.CeilToInt(approachVx / lenx), 0);

            var factor = (approachVy - initSlope * approachVx) / (approachVx * approachVx);

#if UNITY_5_6_OR_NEWER
            lineRenderer.positionCount = segments + 1;
#elif UNITY_5_5_OR_NEWER
            lineRenderer.numPositions = segments + 1;
#else
            lineRenderer.SetVertexCount(segments + 1);
#endif
            lineRenderer.SetPosition(0, transform.InverseTransformPoint(startPoint));
            for (int i = 1, imax = segments; i < imax; ++i)
            {
                var x = i * lenx;
                var y = factor * x * x + initSlope * x;
                lineRenderer.SetPosition(i, transform.InverseTransformPoint(systemX * x + systemY * y + startPoint));
            }
            lineRenderer.SetPosition(segments, transform.InverseTransformPoint(endPoint));
        }
    }

    protected virtual void OnDisable()
    {
        lineRenderer.enabled = false;
    }
}
