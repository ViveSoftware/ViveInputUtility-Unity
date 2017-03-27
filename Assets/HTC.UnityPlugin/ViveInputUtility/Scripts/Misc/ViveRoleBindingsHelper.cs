using HTC.UnityPlugin.Vive;
using System;
using System.IO;
using UnityEngine;
using Valve.VR;

public class ViveRoleBindingsHelper : MonoBehaviour
{
    [Serializable]
    public struct Binding
    {
        public int rv;
        public string sn;
    }

    [Serializable]
    public struct RoleData
    {
        public string type;
        public Binding[] bindings;
    }

    [Serializable]
    public struct RoleBindings
    {
        public RoleData[] roles;
    }

    [SerializeField]
    private string m_viveRoleBindingsConfigPath = "./vive_role_bindings.cfg";
    [SerializeField]
    private bool m_loadConfigOnAwkake = true;

    protected virtual void Awake()
    {
        if (m_loadConfigOnAwkake)
        {
            LoadRoleBindings(m_viveRoleBindingsConfigPath);
        }
    }

    public static void BindAllCurrentDeviceClassMappings(ETrackedDeviceClass deviceClass)
    {
        for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
        {
            var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
            var roleInfo = roleMap.RoleValueInfo;
            for (int rv = roleInfo.MinValidRoleValue, rvmax = roleInfo.MaxValidRoleValue; rv <= rvmax; ++rv)
            {
                if (!roleInfo.IsValidRoleValue(rv)) { continue; }
                if (roleMap.IsRoleValueBound(rv)) { continue; }

                var mappedDevice = roleMap.GetMappedDeviceByRoleValue(rv);
                if (ViveRole.GetDeviceClass(mappedDevice) != deviceClass) { continue; }

                roleMap.BindRoleValue(rv, ViveRole.GetSerialNumber(mappedDevice));
            }
        }
    }

    public static void BindAllCurrentMappings()
    {
        for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
        {
            var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
            roleMap.BindAll();
        }
    }

    public static void UnbindAllCurrentBindings()
    {
        for (int i = 0, imax = ViveRoleEnum.ValidViveRoleTable.Count; i < imax; ++i)
        {
            var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
            roleMap.UnbindAll();
        }
    }

    public static void SaveRoleBindings(string filePath, bool prettyPrint = false)
    {
        using (var outputFile = new StreamWriter(filePath))
        {
            outputFile.Write(JsonUtility.ToJson(ExportRoleBindings(), prettyPrint));
        }
    }

    public static void LoadRoleBindings(string filePath)
    {
        using (var inputFile = new StreamReader(filePath))
        {
            ImportBindings(JsonUtility.FromJson<RoleBindings>(inputFile.ReadToEnd()));
        }
    }

    public static RoleBindings ExportRoleBindings()
    {
        var roleCount = ViveRoleEnum.ValidViveRoleTable.Count;

        // parse role bindings from all role maps
        var roleBindings = new RoleBindings();

        if (roleCount > 0)
        {
            roleBindings.roles = new RoleData[roleCount];
            for (int i = 0; i < roleCount; ++i)
            {
                var roleMap = ViveRole.GetMap(ViveRoleEnum.ValidViveRoleTable.GetValueByIndex(i));
                var roleInfo = roleMap.RoleValueInfo;
                var bindingCount = roleMap.BindingCount;

                var roleData = new RoleData();
                roleData.type = ViveRoleEnum.ValidViveRoleTable.GetKeyByIndex(i);

                if (bindingCount > 0)
                {
                    roleData.bindings = new Binding[bindingCount];

                    var bindingsIndex = 0;
                    for (int roleValue = roleInfo.MinValidRoleValue; roleValue <= roleInfo.MaxValidRoleValue; ++roleValue)
                    {
                        if (!roleInfo.IsValidRoleValue(roleValue)) { continue; }

                        var boundDevice = roleMap.GetBoundDeviceByRoleValue(roleValue);
                        if (string.IsNullOrEmpty(boundDevice)) { continue; }

                        var binding = new Binding();
                        binding.rv = roleValue;
                        binding.sn = boundDevice;

                        roleData.bindings[bindingsIndex++] = binding;
                    }
                }

                roleBindings.roles[i] = roleData;
            }
        }

        return roleBindings;
    }

    public static void ImportBindings(RoleBindings roleBindings)
    {
        foreach (var roleData in roleBindings.roles)
        {
            Type roleType;
            if (string.IsNullOrEmpty(roleData.type) || !ViveRoleEnum.ValidViveRoleTable.TryGetValue(roleData.type, out roleType)) { continue; }

            var roleMap = ViveRole.GetMap(roleType);
            roleMap.UnbindAll();

            foreach (var binding in roleData.bindings)
            {
                if (roleMap.IsRoleValueBound(binding.rv)) { continue; }
                if (roleMap.IsDeviceBound(binding.sn)) { continue; }

                roleMap.BindRoleValue(binding.rv, binding.sn);
            }
        }
    }
}