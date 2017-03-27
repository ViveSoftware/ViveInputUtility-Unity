using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.UI;

public class ChangeMaterialToButtonsColor : MonoBehaviour
{
    private Material m_mat;

    public void ChangeColor(Button btn)
    {
        if (m_mat == null)
        {
            m_mat = new Material(Shader.Find("Diffuse"));
        }

        m_mat.SetColor("_Color", btn.colors.normalColor);

        var renderers = ListPool<MeshRenderer>.Get();
        GetComponentsInChildren(renderers);
        for (int i = renderers.Count - 1; i >= 0; --i) { renderers[i].sharedMaterial = m_mat; }
        ListPool<MeshRenderer>.Release(renderers);
    }
}