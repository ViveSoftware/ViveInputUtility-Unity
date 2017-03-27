using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ImageAlphaRaycastFilter : UIBehaviour, ICanvasRaycastFilter
{
    [NonSerialized]
    private RawImage m_rawImage;

    public float alphaHitTestMinimumThreshold;

    protected RawImage rawImage
    {
        get { return m_rawImage ?? (m_rawImage = GetComponent<RawImage>()); }
    }

    public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (alphaHitTestMinimumThreshold <= 0) { return true; }
        if (alphaHitTestMinimumThreshold > 1) { return false; }

        var texture = rawImage.mainTexture as Texture2D;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, screenPoint, eventCamera, out local))
        {
            return false;
        }

        var rect = rawImage.GetPixelAdjustedRect();

        // Convert to have lower left corner as reference point.
        local.x += rawImage.rectTransform.pivot.x * rect.width;
        local.y += rawImage.rectTransform.pivot.y * rect.height;

        // normalize
        local = new Vector2(local.x / rect.width, local.y / rect.height);

        try
        {
            return texture.GetPixelBilinear(local.x, local.y).a >= alphaHitTestMinimumThreshold;
        }
        catch (UnityException e)
        {
            Debug.LogError("Using alphaHitTestMinimumThreshold greater than 0 on Graphic whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
            return true;
        }
    }
}
