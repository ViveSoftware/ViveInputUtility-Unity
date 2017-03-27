using HTC.UnityPlugin.Vive;
using UnityEngine;
using UnityEngine.UI;

public class RoleMapStatusUIController : MonoBehaviour
{
    public Dropdown dropdownSelectRole;
    public Transform mappingItemsParent;
    public ToggleButtonUIController toggleBindAll;
    public GameObject btnCancelBind;
    public VivePoseTracker pointedReticle;

    private uint m_scannedDevice = ViveRole.INVALID_DEVICE_INDEX;
    private ViveRole.IMap m_selectedMap = null;
    private bool m_isUpdating = false;

    private void Start()
    {
        dropdownSelectRole.options.Clear();

        var defaultSelectIndex = 0;
        for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
        {
            var type = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i);
            dropdownSelectRole.options.Add(new Dropdown.OptionData(type.Name));

            if (type == typeof(DeviceRole))
            {
                defaultSelectIndex = i;
            }
        }

        dropdownSelectRole.RefreshShownValue();
        dropdownSelectRole.value = defaultSelectIndex;
    }

    public void OnDeviceScanned(uint scannedDevice)
    {
        m_scannedDevice = scannedDevice;
        UpdateStatus();
    }

    public void OnDropdownValueChanged(int selectedIndex)
    {
        UpdateStatus();

        if (m_selectedMap != null)
        {
            pointedReticle.viveRole.Set(m_selectedMap.RoleValueInfo.RoleEnumType, m_selectedMap.RoleValueInfo.InvalidRoleValue);
        }
    }

    public void OnToggleValueChanged(bool value)
    {
        if (m_selectedMap == null || m_isUpdating) { return; }

        if (value)
        {
            m_selectedMap.BindAll();
            UpdateStatus();
        }
        else
        {
            m_selectedMap.UnbindAll();
            UpdateStatus();
        }
    }

    public void UpdateStatus()
    {
        if (m_isUpdating) { return; }
        m_isUpdating = true;

        var selectedType = ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(dropdownSelectRole.value);
        m_selectedMap = ViveRole.GetMap(selectedType);
        var info = m_selectedMap.RoleValueInfo;

        // update mapping list
        var boundCount = 0;
        var mappedCount = 0;
        var mappedUnboundCount = 0;
        var itemIndex = 0;
        for (int roleValue = info.MinValidRoleValue, roleValueMax = info.MaxValidRoleValue; roleValue <= roleValueMax; ++roleValue)
        {
            if (!info.IsValidRoleValue(roleValue)) { continue; }

            var isBound = m_selectedMap.IsRoleValueBound(roleValue);
            var isMapped = m_selectedMap.IsRoleValueMapped(roleValue);

            if (isBound) { ++boundCount; }
            if (isMapped) { ++mappedCount; }
            if (isMapped && !isBound) { ++mappedUnboundCount; }

            Transform itemTrans = null;
            if (itemIndex < mappingItemsParent.childCount)
            {
                // reuse item
                itemTrans = mappingItemsParent.GetChild(itemIndex);
            }
            else
            {
                // create new item
                itemTrans = Instantiate(mappingItemsParent.GetChild(0));
                itemTrans.SetParent(mappingItemsParent, false);
            }

            itemTrans.gameObject.SetActive(true);

            var item = itemTrans.GetComponent<MappingItemUIController>();
            item.SetRole(selectedType, roleValue, m_scannedDevice, roleValue == pointedReticle.viveRole.roleValue);

            ++itemIndex;
        }
        // trim mapping list
        while (itemIndex < mappingItemsParent.childCount)
        {
            mappingItemsParent.GetChild(itemIndex++).gameObject.SetActive(false);
        }
        // update toggle button
        if (ViveRole.IsValidIndex(m_scannedDevice) || (mappedCount == 0 && boundCount == 0))
        {
            toggleBindAll.gameObject.SetActive(false);
        }
        else
        {
            toggleBindAll.gameObject.SetActive(true);
            toggleBindAll.isOn = mappedUnboundCount == 0;
        }
        // update cancel button
        btnCancelBind.SetActive(ViveRole.IsValidIndex(m_scannedDevice));

        m_isUpdating = false;
    }

    public void SetPointedReticle(int roleValue)
    {
        pointedReticle.viveRole.roleValue = roleValue;
        UpdateStatus();
    }

    public void InitializeRoleMap()
    {
        m_selectedMap.Handler.OnInitialize();
    }
}
