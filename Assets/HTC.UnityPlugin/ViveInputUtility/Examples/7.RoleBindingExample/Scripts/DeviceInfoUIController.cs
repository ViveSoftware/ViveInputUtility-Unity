using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.VRModuleManagement;
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
    private uint m_deviceIndex = VRModule.INVALID_DEVICE_INDEX;
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && VRModule.Active)
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
            m_deviceIndex = VRModule.INVALID_DEVICE_INDEX;

            textDeviceIndex.text = string.Empty;
            textDeviceClass.text = string.Empty;
            textSerialNum.text = string.Empty;
            textModelNum.text = string.Empty;
        }
        else
        {
            var deviceState = VRModule.GetCurrentDeviceState(m_deviceIndex);
            textDeviceIndex.text = m_deviceIndex.ToString();
            textDeviceClass.text = deviceState.deviceClass.ToString(); ;
            textSerialNum.text = deviceState.serialNumber;
            textModelNum.text = deviceState.modelNumber;
        }
    }
}
