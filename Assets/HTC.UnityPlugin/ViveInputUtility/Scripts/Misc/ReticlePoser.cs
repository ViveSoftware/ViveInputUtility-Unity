using HTC.UnityPlugin.Pointer3D;
using UnityEngine;
using UnityEngine.Serialization;

public class ReticlePoser : MonoBehaviour
{
    public Pointer3DRaycaster raycaster;
    [FormerlySerializedAs("Target")]
    public Transform reticleForDefaultRay;
    public Transform reticleForCurvedRay;
    public bool showOnHitOnly = true;

    public GameObject hitTarget;
    public float hitDistance;
#if UNITY_EDITOR
    protected virtual void Reset()
    {
        for (var tr = transform; raycaster == null && tr != null; tr = tr.parent)
        {
            raycaster = tr.GetComponentInChildren<Pointer3DRaycaster>();
        }
    }
#endif
    protected virtual void LateUpdate()
    {
        var points = raycaster.BreakPoints;
        var pointCount = points.Count;
        var result = raycaster.FirstRaycastResult();

        if ((showOnHitOnly && !result.isValid) || pointCount <= 1)
        {
            reticleForDefaultRay.gameObject.SetActive(false);
            reticleForCurvedRay.gameObject.SetActive(false);
            return;
        }

        var isCurvedRay = raycaster.CurrentSegmentGenerator() != null;

        if (reticleForDefaultRay != null) { reticleForDefaultRay.gameObject.SetActive(!isCurvedRay); }
        if (reticleForCurvedRay != null) { reticleForCurvedRay.gameObject.SetActive(isCurvedRay); }

        var targetReticle = isCurvedRay ? reticleForCurvedRay : reticleForDefaultRay;
        if (result.isValid)
        {
            if (targetReticle != null)
            {
                targetReticle.position = result.worldPosition;
                targetReticle.rotation = Quaternion.LookRotation(result.worldNormal, raycaster.transform.forward);
            }

            hitTarget = result.gameObject;
            hitDistance = result.distance;
        }
        else
        {
            if (targetReticle != null)
            {
                targetReticle.position = points[pointCount - 1];
                targetReticle.rotation = Quaternion.LookRotation(points[pointCount - 1] - points[pointCount - 2], raycaster.transform.forward);
            }

            hitTarget = null;
            hitDistance = 0f;
        }
    }

    protected virtual void OnDisable()
    {
        reticleForDefaultRay.gameObject.SetActive(false);
        reticleForCurvedRay.gameObject.SetActive(false);
    }
}
