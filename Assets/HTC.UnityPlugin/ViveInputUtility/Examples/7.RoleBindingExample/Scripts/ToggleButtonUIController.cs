using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// This script works just like UI Toggle, but uses 2 on/off images
// instead of only 1 checkmark image
public class ToggleButtonUIController : MonoBehaviour
{
    [Serializable]
    public class UnityEventBool : UnityEvent<bool> { }

    public Button m_button;
    public Image m_imgOn;
    public Image m_imgOff;
    [SerializeField]
    private bool m_isOn;
    [SerializeField]
    private UnityEventBool m_onValueChanged = new UnityEventBool();

    private bool m_isOnInternal;
#if UNITY_EDITOR
    private bool m_enabled;
#endif

    public bool isOn
    {
        get { return m_isOnInternal; }
        set
        {
            if (ChangeProp.Set(ref m_isOnInternal, value))
            {
                m_isOn = value;
                UpdateStatus();
                if (m_onValueChanged != null)
                {
                    m_onValueChanged.Invoke(value);
                }
            }
        }
    }

    public UnityEventBool onValueChanged
    {
        get { return m_onValueChanged; }
        set { m_onValueChanged = value; }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (m_enabled)
        {
            isOn = m_isOn;
        }
    }
#endif
    private void OnEnable()
    {
#if UNITY_EDITOR
        m_enabled = true;
#endif
        m_isOnInternal = m_isOn;
        m_button.onClick.AddListener(OnButtonClicked);
        UpdateStatus();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        m_enabled = false;
#endif
        m_button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        isOn = !isOn;
    }

    private void UpdateStatus()
    {
        m_imgOn.gameObject.SetActive(isOn);
        m_imgOff.gameObject.SetActive(!isOn);
    }
}
