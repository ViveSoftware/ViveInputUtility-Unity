using UnityEngine;
using System.Collections;

public class SpawnObjectOnTriggerExit : MonoBehaviour
{
    public GameObject effectTarget;
    public float delay = 1.0f;

    private Vector3 originPosition;
    private Quaternion originRotation;

    private GameObject clonedTarget;

    private void Start()
    {
        clonedTarget = effectTarget;
        originPosition = effectTarget.transform.localPosition;
        originRotation = effectTarget.transform.localRotation;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == clonedTarget)
        {
            StopAllCoroutines();
            StartCoroutine(CopyTarget());
        }
    }

    private IEnumerator CopyTarget()
    {
        yield return new WaitForSeconds(delay);

        var copy = Instantiate(effectTarget);
        copy.transform.SetParent(effectTarget.transform.parent);
        copy.transform.localPosition = originPosition;
        copy.transform.localRotation = originRotation;
        copy.name = effectTarget.name;

        clonedTarget = copy;
    }
}
