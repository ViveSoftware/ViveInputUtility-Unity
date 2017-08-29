using HTC.UnityPlugin.Vive;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HTC.UnityPlugin.VRModuleManagement;

public class MappingItemUIController : MonoBehaviour, IPointerEnterHandler
{
    [Serializable]
    public class UnityEventInt : UnityEvent<int> { }

    public Image imgBG;
    public Image imgInnerBG;
    public ToggleButtonUIController toggleBind;
    public GameObject buttonBind;
    public Text textRoleName;
    public Text textArrow;
    public Image imgDeviceIcon;
    public Text textDeviceName;

    // HMD = 0,
    // Controller = 1,
    // GenericTracker = 2,
    // TrackingReference = 3,
    public Sprite[] deviceIconSprites;

    public UnityEventInt onPointed = new UnityEventInt();

    private int m_roleValue;
    private ViveRole.IMap m_map;
    private bool m_isPointed;

    private uint m_selectedDevice;
    private uint m_mappedDevice;

    private string m_selectedDeviceSN;
    private string m_mappedDeviceSN;
    private string m_boundDeviceSN;

    private bool m_isUpdating;

    public void SetRole(Type roleType, int roleValue, uint selectedDevice, bool isPointed)
    {
        m_roleValue = roleValue;
        m_map = ViveRole.GetMap(roleType);
        m_isPointed = isPointed;

        m_selectedDevice = selectedDevice;
        m_selectedDeviceSN = VRModule.GetCurrentDeviceState(selectedDevice).serialNumber;

        UpdateState();
    }

    public void UpdateState()
    {
        if (m_isUpdating) { return; }

        m_isUpdating = true;

        m_mappedDevice = m_map.GetMappedDeviceByRoleValue(m_roleValue);
        m_boundDeviceSN = m_map.GetBoundDeviceByRoleValue(m_roleValue);

        var mappedDeviceState = VRModule.GetCurrentDeviceState(m_mappedDevice);
        m_mappedDeviceSN = mappedDeviceState.serialNumber;

        var isMapped = VRModule.IsValidDeviceIndex(m_mappedDevice);
        var isBound = !string.IsNullOrEmpty(m_boundDeviceSN);
        var isSelectedValid = VivePose.IsConnected(m_selectedDevice);
        var isSelectingThisItem = isSelectedValid && (m_selectedDeviceSN == m_boundDeviceSN || m_selectedDevice == m_mappedDevice);

        // set background color
        imgBG.color = new Color(0f, 1f, 0f, isBound ? 1f : 0f);

        if (isMapped)
        {
            if (m_isPointed)
            {
                imgInnerBG.color = Color.yellow;
            }
            else
            {
                imgInnerBG.color = Color.white;
            }
        }
        else
        {
            imgInnerBG.color = Color.gray;
        }

        if (VRModule.IsValidDeviceIndex(m_selectedDevice))
        {
            toggleBind.gameObject.SetActive(false);
        }
        else
        {
            toggleBind.gameObject.SetActive(isBound || isMapped);
            toggleBind.isOn = isBound;
        }

        buttonBind.SetActive(!isBound && isSelectedValid);

        textRoleName.text = m_map.RoleValueInfo.GetNameByRoleValue(m_roleValue);

        // update device icon
        if (mappedDeviceState.deviceClass == VRModuleDeviceClass.Invalid)
        {
            imgDeviceIcon.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            imgDeviceIcon.transform.parent.gameObject.SetActive(true);
            imgDeviceIcon.sprite = deviceIconSprites[(int)mappedDeviceState.deviceClass - 1];
        }


        if (isMapped)
        {
            textDeviceName.text = "[" + m_mappedDevice + "]" + " " + m_mappedDeviceSN;
        }
        else if (isBound)
        {
            textDeviceName.text = m_boundDeviceSN;
        }
        else
        {
            textDeviceName.text = string.Empty;
        }

        // heighLight selected item
        if (isSelectingThisItem)
        {
            textRoleName.color = Color.blue;
            textArrow.color = Color.blue;
            textDeviceName.color = Color.blue;
        }
        else
        {
            textRoleName.color = Color.black;
            textArrow.color = Color.black;
            textDeviceName.color = Color.black;
        }

        m_isUpdating = false;
    }

    public void OnToggleBindChanged(bool value)
    {
        if (m_isUpdating) { return; }

        if (value)
        {
            m_map.BindDeviceToRoleValue(m_mappedDeviceSN, m_roleValue);
        }
        else
        {
            m_map.UnbindRoleValue(m_roleValue);
        }
    }

    public void OnButtonBind()
    {
        m_map.BindDeviceToRoleValue(m_selectedDeviceSN, m_roleValue);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (onPointed != null && VRModule.IsValidDeviceIndex(m_mappedDevice))
        {
            onPointed.Invoke(m_roleValue);
        }
    }
}
