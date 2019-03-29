//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using UnityEngine;
using UnityEngine.Serialization;

public class ReticlePoser : MonoBehaviour
{
    public interface IMaterialChanger
    {
        Material reticleMaterial { get; }
    }

    public Pointer3DRaycaster raycaster;
    [FormerlySerializedAs("Target")]
    public Transform reticleForDefaultRay;
    public Transform reticleForCurvedRay;
    public bool showOnHitOnly = true;

    public GameObject hitTarget;
    public float hitDistance;
    public Material defaultReticleMaterial;
    public MeshRenderer[] reticleRenderer;

    public bool autoScaleReticle = false;
    public int sizeInPixels = 50;

    private Material m_matFromChanger;
#if UNITY_EDITOR
    protected virtual void Reset()
    {
        for (var tr = transform; raycaster == null && tr != null; tr = tr.parent)
        {
            raycaster = tr.GetComponentInChildren<Pointer3DRaycaster>(true);
        }

        reticleRenderer = GetComponentsInChildren<MeshRenderer>(true);
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
                if (autoScaleReticle)
                {
                    // Set the reticle size based on sizeInPixels, references:
                    // https://answers.unity.com/questions/268611/with-a-perspective-camera-distance-independent-siz.html
                    Vector3 a = Camera.main.WorldToScreenPoint(targetReticle.position);
                    Vector3 b = new Vector3(a.x, a.y + sizeInPixels, a.z);
                    Vector3 aa = Camera.main.ScreenToWorldPoint(a);
                    Vector3 bb = Camera.main.ScreenToWorldPoint(b);
                    targetReticle.localScale = Vector3.one * (aa - bb).magnitude;
                }
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

        // Change reticle material according to IReticleMaterialChanger
        var matChanger = hitTarget == null ? null : hitTarget.GetComponentInParent<IMaterialChanger>();
        var newMat = matChanger == null ? null : matChanger.reticleMaterial;
        if (m_matFromChanger != newMat)
        {
            m_matFromChanger = newMat;

            if (newMat != null)
            {
                SetReticleMaterial(newMat);
            }
            else if (defaultReticleMaterial != null)
            {
                SetReticleMaterial(defaultReticleMaterial);
            }
        }
    }

    private void SetReticleMaterial(Material mat)
    {
        if (reticleRenderer == null || reticleRenderer.Length == 0) { return; }

        foreach (MeshRenderer mr in reticleRenderer)
        {
            mr.material = mat;
        }
    }

    protected virtual void OnDisable()
    {
        reticleForDefaultRay.gameObject.SetActive(false);
        reticleForCurvedRay.gameObject.SetActive(false);
    }
}
