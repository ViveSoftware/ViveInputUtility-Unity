using HTC.UnityPlugin.Vive;
using System;
using UnityEngine;
using UnityEngine.UI;

public class DeviceInfoUIController : MonoBehaviour
{
    public Text textDeviceIndex;
    public Text textDeviceClass;
    public Text textSerialNum;
    public Text textModelNum;

    [SerializeField]
    private uint m_deviceIndex = ViveRole.INVALID_DEVICE_INDEX;
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateStatus();
        }
    }
#endif
    public void OnDeviceScanned(uint deviceIndex)
    {
        m_deviceIndex = deviceIndex;
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (!VivePose.IsConnected(m_deviceIndex))
        {
            m_deviceIndex = ViveRole.INVALID_DEVICE_INDEX;

            textDeviceIndex.text = string.Empty;
            textDeviceClass.text = string.Empty;
            textSerialNum.text = string.Empty;
            textModelNum.text = string.Empty;
        }
        else
        {
            textDeviceIndex.text = m_deviceIndex.ToString();
            textDeviceClass.text = ViveRole.GetDeviceClass(m_deviceIndex).ToString();
            textSerialNum.text = ViveRole.GetSerialNumber(m_deviceIndex);
            textModelNum.text = ViveRole.GetModelNumber(m_deviceIndex);
        }
    }
}
