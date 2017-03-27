//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    public static partial class ViveRole
    {
        public interface IMap
        {
            ViveRoleEnum.IInfo RoleValueInfo { get; }
            IMapHandler Handler { get; }
            int BindingCount { get; }

            bool IsRoleValueMapped(int roleValue);
            bool IsDeviceMapped(uint deviceIndex);
            uint GetMappedDeviceByRoleValue(int roleValue);
            int GetMappedRoleValueByDevice(uint deviceIndex);

            void BindRoleValue(int roleValue, string deviceSN);
            void BindAll();
            bool UnbindRoleValue(int roleValue); // return true if role is ready for bind
            bool UnbindDevice(string deviceSN); // return true if device is ready for bind
            bool UnbindConnectedDevice(uint deviceIndex); // return true if device is ready for bind
            void UnbindAll();

            bool IsRoleValueBound(int roleValue);
            bool IsDeviceBound(string deviceSN);
            bool IsDeviceConnectedAndBound(uint deviceIndex);
            string GetBoundDeviceByRoleValue(int roleValue);
            int GetBoundRoleValueByDevice(string deviceSN);
            int GetBoundRoleValueByConnectedDevice(uint deviceSN);
        }

        private sealed class Map : IMap
        {
            private readonly ViveRoleEnum.IInfo m_info;
            private IMapHandler m_handler;

            // mapping table
            private readonly uint[] m_role2index;
            private readonly int[] m_index2role;

            // binding table
            private readonly string[] m_role2sn;
            private readonly Dictionary<string, int> m_sn2role;

            public Map(Type roleType)
            {
                m_info = ViveRoleEnum.GetInfo(roleType);

                m_role2index = new uint[m_info.ValidRoleLength];
                m_index2role = new int[MAX_DEVICE_COUNT];

                m_role2sn = new string[m_info.ValidRoleLength];
                m_sn2role = new Dictionary<string, int>(Mathf.Min(m_info.ValidRoleLength, (int)MAX_DEVICE_COUNT));

                for (int i = 0; i < m_role2index.Length; ++i)
                {
                    m_role2index[i] = INVALID_DEVICE_INDEX;
                    m_role2sn[i] = string.Empty;
                }

                for (int i = 0; i < m_index2role.Length; ++i)
                {
                    m_index2role[i] = m_info.InvalidRoleValue;
                }
            }

            public ViveRoleEnum.IInfo RoleValueInfo { get { return m_info; } }
            public int BindingCount { get { return m_sn2role.Count; } }

            public IMapHandler Handler
            {
                get { return m_handler; }
                set
                {
                    if (ChangeProp.Set(ref m_handler, value) && m_handler != null)
                    {
                        m_handler.OnInitialize();
                    }
                }
            }

            public void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected)
            {
                var boundRoleValue = GetBoundRoleValueByDevice(deviceSN);
                if (m_info.IsValidRoleValue(boundRoleValue)) // if device is bound
                {
                    if (connected)
                    {
                        InternalMapping(boundRoleValue, deviceIndex);
                    }
                    else
                    {
                        InternalUnmapping(boundRoleValue, deviceIndex);
                    }
                }

                if (m_handler != null)
                {
                    m_handler.OnConnectedDeviceChanged(deviceIndex, deviceClass, deviceSN, connected);
                }
            }

            public void OnTrackedDeviceRoleChanged()
            {
                if (m_handler != null)
                {
                    m_handler.OnTrackedDeviceRoleChanged();
                }
            }

            #region mapping
            public void MappingRoleValue(int roleValue, uint deviceIndex)
            {
                if (!m_info.IsValidRoleValue(roleValue))
                {
                    throw new ArgumentException("Cannot mapping invalid roleValue(" + m_info.RoleEnumType.Name + "[" + roleValue + "])");
                }

                if (!IsValidIndex(deviceIndex))
                {
                    throw new ArgumentException("Cannot mapping invalid deviceIndex(" + deviceIndex + ")");
                }

                if (IsRoleValueMapped(roleValue))
                {
                    throw new ArgumentException("roleValue(" + m_info.RoleEnumType.Name + "[" + roleValue + "]) is already mapped, unmapping first.");
                }

                if (IsDeviceMapped(deviceIndex))
                {
                    throw new ArgumentException("deviceIndex(" + deviceIndex + ") is already mapped, unmapping first");
                }

                if (IsRoleValueBound(roleValue))
                {
                    throw new ArgumentException("roleValue(" + m_info.RoleEnumType.Name + "[" + roleValue + "]) is already bound, unbind first.");
                }

                if (IsDeviceConnectedAndBound(deviceIndex))
                {
                    throw new ArgumentException("deviceIndex(" + deviceIndex + ") is already bound, unbind first");
                }

                InternalMapping(roleValue, deviceIndex);
            }

            private void InternalMapping(int roleValue, uint deviceIndex)
            {
                m_role2index[m_info.ToRoleOffset(roleValue)] = deviceIndex;
                m_index2role[deviceIndex] = roleValue;
            }

            // return true if role is ready for mapping
            public bool UnmappingRoleValue(int roleValue)
            {
                if (!m_info.IsValidRoleValue(roleValue)) { return false; }

                var roleOffset = m_info.ToRoleOffset(roleValue);

                // is bound?
                if (!string.IsNullOrEmpty(m_role2sn[roleOffset])) { return false; }

                // is mapped?
                var deviceIndex = m_role2index[roleOffset];
                if (IsValidIndex(deviceIndex))
                {
                    InternalUnmapping(roleValue, deviceIndex);
                }

                return true;
            }

            // return true if device is ready for mapping
            public bool UnmappingDevice(uint deviceIndex)
            {
                if (!IsValidIndex(deviceIndex)) { return false; }

                // is bound?
                var deviceSN = GetSerialNumber(deviceIndex);
                if (!string.IsNullOrEmpty(deviceSN) && m_sn2role.ContainsKey(deviceSN)) { return false; }

                // is mapped?
                var roleValue = m_index2role[deviceIndex];
                if (m_info.IsValidRoleValue(roleValue))
                {
                    InternalUnmapping(roleValue, deviceIndex);
                }

                return true;
            }

            private void InternalUnmapping(int roleValue, uint deviceIndex)
            {
                m_role2index[m_info.ToRoleOffset(roleValue)] = INVALID_DEVICE_INDEX;
                m_index2role[deviceIndex] = m_info.InvalidRoleValue;
            }

            public void UnmappingAll()
            {
                for (int i = m_role2index.Length - 1; i >= 0; --i)
                {
                    if (!string.IsNullOrEmpty(m_role2sn[i])) { continue; } // skip bound role

                    var mappedDeviceIndex = m_role2index[i];
                    if (IsValidIndex(mappedDeviceIndex))
                    {
                        m_role2index[i] = INVALID_DEVICE_INDEX;
                        m_index2role[mappedDeviceIndex] = m_info.InvalidRoleValue;
                    }
                }
            }

            public bool IsRoleValueMapped(int roleValue)
            {
                return m_info.IsValidRoleValue(roleValue) && IsValidIndex(m_role2index[m_info.ToRoleOffset(roleValue)]);
            }

            public bool IsDeviceMapped(uint deviceIndex)
            {
                return IsValidIndex(deviceIndex) && m_info.IsValidRoleValue(m_index2role[deviceIndex]);
            }

            public uint GetMappedDeviceByRoleValue(int roleValue)
            {
                if (m_info.IsValidRoleValue(roleValue))
                {
                    return m_role2index[m_info.ToRoleOffset(roleValue)];
                }
                else
                {
                    return INVALID_DEVICE_INDEX;
                }
            }

            public int GetMappedRoleValueByDevice(uint deviceIndex)
            {
                if (IsValidIndex(deviceIndex))
                {
                    return m_index2role[deviceIndex];
                }
                else
                {
                    return m_info.InvalidRoleValue;
                }
            }
            #endregion mapping

            #region bind
            public void BindRoleValue(int roleValue, string deviceSN)
            {
                if (!m_info.IsValidRoleValue(roleValue))
                {
                    throw new ArgumentException("roleValue must be valid value. Use IInfo.IsValidRoleValue to validate."); ;
                }

                if (string.IsNullOrEmpty(deviceSN))
                {
                    throw new ArgumentException("deviceSN cannot be null or empty.");
                }

                if (IsRoleValueBound(roleValue))
                {
                    throw new ArgumentException("roleValue(" + roleValue + ") is already bound, unbind first.");
                }

                if (IsDeviceBound(deviceSN))
                {
                    throw new ArgumentException("deviceSN(" + deviceSN + ") is already bound, unbind first.");
                }

                UnmappingRoleValue(roleValue);

                uint deviceIndex;
                if (TryGetDeviceIndexBySerialNumber(deviceSN, out deviceIndex))
                {
                    UnmappingDevice(deviceIndex);
                    InternalMapping(roleValue, deviceIndex);
                }

                InternalBind(roleValue, deviceSN);
            }

            private void InternalBind(int roleValue, string deviceSN)
            {
                m_sn2role[deviceSN] = roleValue;
                m_role2sn[m_info.ToRoleOffset(roleValue)] = deviceSN;

                if (m_handler != null)
                {
                    m_handler.OnBindingRoleValueChanged(roleValue, deviceSN, true);
                }
            }

            // bind all mapped roles & devices
            public void BindAll()
            {
                for (int i = m_role2sn.Length - 1; i >= 0; --i)
                {
                    if (!string.IsNullOrEmpty(m_role2sn[i]) || !IsValidIndex(m_role2index[i])) { continue; }

                    // if role is unbound but mapped
                    var roleValue = i + m_info.MinValidRoleValue;
                    var deviceSN = GetSerialNumber(m_role2index[i]);
                    m_sn2role[deviceSN] = roleValue;
                    m_role2sn[i] = deviceSN;

                    if (m_handler != null)
                    {
                        m_handler.OnBindingRoleValueChanged(roleValue, deviceSN, true);
                    }
                }
            }

            public bool UnbindRoleValue(int roleValue)
            {
                if (!m_info.IsValidRoleValue(roleValue)) { return false; }

                // is bound?
                var roleOffset = m_info.ToRoleOffset(roleValue);
                var deviceSN = m_role2sn[roleOffset];
                if (!string.IsNullOrEmpty(deviceSN))
                {
                    InternalUnbind(roleValue, deviceSN);
                }

                return true;
            }

            public bool UnbindDevice(string deviceSN)
            {
                if (string.IsNullOrEmpty(deviceSN)) { return false; }

                // is bound
                int roleValue;
                if (m_sn2role.TryGetValue(deviceSN, out roleValue))
                {
                    InternalUnbind(roleValue, deviceSN);
                }

                return true;
            }

            public bool UnbindConnectedDevice(uint deviceIndex)
            {
                return UnbindDevice(GetSerialNumber(deviceIndex));
            }

            private void InternalUnbind(int roleValue, string deviceSN)
            {
                m_role2sn[m_info.ToRoleOffset(roleValue)] = string.Empty;
                m_sn2role.Remove(deviceSN);

                if (m_handler != null)
                {
                    m_handler.OnBindingRoleValueChanged(roleValue, deviceSN, false);
                }
            }

            public void UnbindAll()
            {
                if (m_handler == null)
                {
                    for (int i = m_role2sn.Length - 1; i >= 0; --i) { m_role2sn[i] = string.Empty; }
                    m_sn2role.Clear();
                    return;
                }
                else
                {
                    for (int i = m_role2sn.Length - 1; i >= 0; --i)
                    {
                        var boundDeviceSN = m_role2sn[i];
                        if (string.IsNullOrEmpty(boundDeviceSN)) { continue; }

                        m_role2sn[i] = string.Empty;
                        m_sn2role.Remove(boundDeviceSN);

                        m_handler.OnBindingRoleValueChanged(i + m_info.MinValidRoleValue, boundDeviceSN, false);
                    }
                }
            }

            public bool IsRoleValueBound(int roleValue)
            {
                return m_info.IsValidRoleValue(roleValue) && !string.IsNullOrEmpty(m_role2sn[m_info.ToRoleOffset(roleValue)]);
            }

            public bool IsDeviceBound(string deviceSN)
            {
                return string.IsNullOrEmpty(deviceSN) ? false : m_sn2role.ContainsKey(deviceSN);
            }

            public bool IsDeviceConnectedAndBound(uint deviceIndex)
            {
                return IsDeviceBound(GetSerialNumber(deviceIndex));
            }


            public string GetBoundDeviceByRoleValue(int roleValue)
            {
                if (m_info.IsValidRoleValue(roleValue))
                {
                    return m_role2sn[m_info.ToRoleOffset(roleValue)];
                }
                else
                {
                    return string.Empty;
                }
            }

            public int GetBoundRoleValueByDevice(string deviceSN)
            {
                int roleValue;
                if (!string.IsNullOrEmpty(deviceSN) && m_sn2role.TryGetValue(deviceSN, out roleValue))
                {
                    return roleValue;
                }
                else
                {
                    return m_info.InvalidRoleValue;
                }
            }

            public int GetBoundRoleValueByConnectedDevice(uint deviceIndex)
            {
                return GetBoundRoleValueByDevice(GetSerialNumber(deviceIndex));
            }
            #endregion bind
        }

        public interface IMap<TRole> : IMap
        {
            ViveRoleEnum.IInfo<TRole> RoleInfo { get; }

            bool IsRoleMapped(TRole role);
            uint GetMappedDeviceByRole(TRole role);
            TRole GetMappedRoleByDevice(uint deviceIndex);

            void BindRole(TRole role, string deviceSN);
            bool UnbindRole(TRole role); // return true if role is ready for bind

            bool IsRoleBound(TRole role);
            string GetBoundDeviceByRole(TRole role);
            TRole GetBoundRoleByDevice(string deviceSN);
            TRole GetBoundRoleByConnectedDevice(uint deviceIndex);
        }

        private sealed class GenericMap<TRole> : IMap<TRole>
        {
            public static GenericMap<TRole> s_instance;

            private readonly ViveRoleEnum.IInfo<TRole> m_info;
            private readonly Map m_map;

            public GenericMap()
            {
                m_info = ViveRoleEnum.GetInfo<TRole>();
                m_map = GetInternalMap(typeof(TRole));

                if (s_instance == null)
                {
                    s_instance = this;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("duplicated instance for RoleInfo<" + typeof(TRole).Name + ">");
                }
            }

            public ViveRoleEnum.IInfo RoleValueInfo { get { return m_map.RoleValueInfo; } }
            public ViveRoleEnum.IInfo<TRole> RoleInfo { get { return m_info; } }
            public IMapHandler Handler { get { return m_map.Handler; } }
            public int BindingCount { get { return m_map.BindingCount; } }

            public void MappingRole(TRole role, uint deviceIndex) { m_map.MappingRoleValue(m_info.ToRoleValue(role), deviceIndex); }
            public void MappingRoleValue(int roleValue, uint deviceIndex) { m_map.MappingRoleValue(roleValue, deviceIndex); }
            public bool UnmappingRole(TRole role) { return m_map.UnmappingRoleValue(m_info.ToRoleValue(role)); }
            public bool UnmappingRoleValue(int roleValue) { return m_map.UnmappingRoleValue(roleValue); }
            public bool UnmappingDevice(uint deviceIndex) { return m_map.UnmappingDevice(deviceIndex); }
            public void UnmappingAll() { m_map.UnmappingAll(); }

            public bool IsRoleMapped(TRole role) { return m_map.IsRoleValueMapped(m_info.ToRoleValue(role)); }
            public bool IsRoleValueMapped(int roleValue) { return m_map.IsRoleValueMapped(roleValue); }
            public bool IsDeviceMapped(uint deviceIndex) { return m_map.IsDeviceMapped(deviceIndex); }
            public uint GetMappedDeviceByRole(TRole role) { return m_map.GetMappedDeviceByRoleValue(m_info.ToRoleValue(role)); }
            public TRole GetMappedRoleByDevice(uint deviceIndex) { return m_info.ToRole(m_map.GetMappedRoleValueByDevice(deviceIndex)); }
            public uint GetMappedDeviceByRoleValue(int roleValue) { return m_map.GetMappedDeviceByRoleValue(roleValue); }
            public int GetMappedRoleValueByDevice(uint deviceIndex) { return m_map.GetMappedRoleValueByDevice(deviceIndex); }

            public void BindRole(TRole role, string deviceSN) { m_map.BindRoleValue(m_info.ToRoleValue(role), deviceSN); }
            public void BindRoleValue(int roleValue, string deviceSN) { m_map.BindRoleValue(roleValue, deviceSN); }
            public void BindAll() { m_map.BindAll(); }
            public bool UnbindRole(TRole role) { return m_map.UnbindRoleValue(m_info.ToRoleValue(role)); }
            public bool UnbindRoleValue(int roleValue) { return m_map.UnbindRoleValue(roleValue); }
            public bool UnbindDevice(string deviceSN) { return m_map.UnbindDevice(deviceSN); }
            public bool UnbindConnectedDevice(uint deviceIndex) { return UnbindConnectedDevice(deviceIndex); }
            public void UnbindAll() { m_map.UnbindAll(); }

            public bool IsRoleBound(TRole role) { return m_map.IsRoleValueBound(m_info.ToRoleValue(role)); }
            public bool IsRoleValueBound(int roleValue) { return m_map.IsRoleValueBound(roleValue); }
            public bool IsDeviceBound(string deviceSN) { return m_map.IsDeviceBound(deviceSN); }
            public bool IsDeviceConnectedAndBound(uint deviceIndex) { return m_map.IsDeviceConnectedAndBound(deviceIndex); }
            public TRole GetBoundRoleByDevice(string deviceSN) { return m_info.ToRole(m_map.GetBoundRoleValueByDevice(deviceSN)); }
            public TRole GetBoundRoleByConnectedDevice(uint deviceIndex) { return m_info.ToRole(m_map.GetBoundRoleValueByConnectedDevice(deviceIndex)); }
            public string GetBoundDeviceByRole(TRole role) { return m_map.GetBoundDeviceByRoleValue(m_info.ToRoleValue(role)); }
            public string GetBoundDeviceByRoleValue(int roleValue) { return m_map.GetBoundDeviceByRoleValue(roleValue); }
            public int GetBoundRoleValueByDevice(string deviceSN) { return m_map.GetBoundRoleValueByDevice(deviceSN); }
            public int GetBoundRoleValueByConnectedDevice(uint deviceIndex) { return m_map.GetBoundRoleValueByConnectedDevice(deviceIndex); }
        }

        private static IndexedTable<Type, Map> s_mapTable;

        private static Map GetInternalMap(Type roleType)
        {
            if (s_mapTable == null)
            {
                s_mapTable = new IndexedTable<Type, Map>();
            }

            Map map;
            if (!s_mapTable.TryGetValue(roleType, out map))
            {
                var validateResult = ViveRoleEnum.ValidateViveRoleEnum(roleType);
                if (validateResult != ViveRoleEnumValidateResult.Valid)
                {
                    UnityEngine.Debug.LogWarning(roleType.Name + " is not valid ViveRole type. " + validateResult);
                    return null;
                }

                map = new Map(roleType);
                s_mapTable.Add(roleType, map);
            }

            return map;
        }

        public static IMap GetMap(Type roleType)
        {
            return GetInternalMap(roleType);
        }

        private static GenericMap<TRole> GetInternalMap<TRole>()
        {
            var roleEnumType = typeof(TRole);
            if (GenericMap<TRole>.s_instance == null)
            {
                var validateResult = ViveRoleEnum.ValidateViveRoleEnum(roleEnumType);
                if (validateResult != ViveRoleEnumValidateResult.Valid)
                {
                    UnityEngine.Debug.LogWarning(roleEnumType.Name + " is not valid ViveRole type. " + validateResult);
                    return null;
                }

                new GenericMap<TRole>();
            }

            return GenericMap<TRole>.s_instance;
        }

        public static IMap<TRole> GetMap<TRole>()
        {
            return GetInternalMap<TRole>();
        }

        public static void AssignMapHandler<TRole>(MapHandler<TRole> mapHandler)
        {
            GetInternalMap(typeof(TRole)).Handler = mapHandler;
        }
    }
}