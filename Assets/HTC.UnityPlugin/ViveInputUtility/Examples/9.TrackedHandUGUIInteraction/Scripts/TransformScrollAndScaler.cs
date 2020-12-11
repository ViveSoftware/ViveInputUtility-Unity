using UnityEngine;

public class TransformScrollAndScaler : MonoBehaviour
{
    public float scaleFrom;
    public float scaleTo;
    public Vector3 eulerFrom;
    public Vector3 eulerTo;

    public void RotateNormalized(float value)
    {
        transform.localRotation = Quaternion.Euler(new Vector3()
        {
            x = Mathf.Lerp(eulerFrom.x, eulerTo.x, value),
            y = Mathf.Lerp(eulerFrom.y, eulerTo.y, value),
            z = Mathf.Lerp(eulerFrom.z, eulerTo.z, value),
        });
    }

    public void ScaleNormalized(float value)
    {
        transform.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, value);
    }
}
