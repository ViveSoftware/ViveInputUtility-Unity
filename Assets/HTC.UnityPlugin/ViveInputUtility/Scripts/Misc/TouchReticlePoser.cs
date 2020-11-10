using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchReticlePoser : ReticlePoser
{
    [SerializeField]
    float MaxDistance = 0.15f;
    //[SerializeField]
    //int reticleSizePixelMin = 0;
    [SerializeField]
    int reticleSizePixelMax = 3000;
    [SerializeField]
    float reticleAlphaFar = 0.3f;
    //[SerializeField]
    //float reticleAlphaNear = 0;

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (hitTarget == null)
            return;

        float ratio = (hitDistance - ViveFingerPoseTracker.HitOffset) / MaxDistance;
        if (ratio > 1)
        {
            if (reticleForDefaultRay != null) { reticleForDefaultRay.gameObject.SetActive(false); }
            if (reticleForCurvedRay != null) { reticleForCurvedRay.gameObject.SetActive(false); }
            return;
        }

        autoScaleReticle = true;
        sizeInPixels = (int)Mathf.Lerp(0, reticleSizePixelMax, ratio);

        float alpha = Mathf.Lerp(reticleAlphaFar, 0, ratio);
        setReticleMaterialAlpha(alpha);
    }

    private void setReticleMaterialAlpha(float alpha)
    {
        if (reticleRenderer == null || reticleRenderer.Length == 0) { return; }

        foreach (MeshRenderer mr in reticleRenderer)
        {
            Color color = mr.material.color;
            color.a = alpha;
            mr.material.color = color;
        }
    }
}
