//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    public static partial class ViveRole
    {
        public struct MappingChangedEventArg
        {
            public int roleValue;
            public uint previousDeviceIndex;
            public uint currentDeviceIndex;
        }

        public struct MappingChangedEventArg<TRole>
        {
            public TRole role;
            public uint previousDeviceIndex;
            public uint currentDeviceIndex;
        }

        public interface IMap
        {
            ViveRoleEnum.IInfo RoleValueInfo { get; }
            IMapHandler Handler { get; }
            int BindingCount { get; }
            IIndexedTableReadOnly<string, int> BindingTable { get; }

            bool IsRoleValueMapped(int roleValue);
            bool IsDeviceMapped(uint deviceIndex);
            uint GetMappedDeviceByRoleValue(int roleValue);
            int GetMappedRoleValueByDevice(uint deviceIndex);

            [Obsolete("Use BindDeviceToRoleValue instead")]
            void BindRoleValue(int roleValue, string deviceSN);
            void BindDeviceToRoleValue(string deviceSN, int roleValue);
            void BindAll();
            bool UnbindRoleValue(int roleValue); // return true if role is ready for bind
            bool UnbindDevice(string deviceSN); // return true if device is ready for bind
            bool UnbindConnectedDevice(uint deviceIndex); // return true if device is ready for bind
            void UnbindAll();

            bool IsRoleValueBound(int roleValue);
            bool IsDeviceBound(string deviceSN);
            bool IsDeviceConnectedAndBound(uint deviceIndex);
            string GetBoundDeviceByRoleValue(int roleValue);
            /// <summary>
            /// Should use IsDeviceBound to validate deviceSN before calling this function
            /// </summary>
            int GetBoundRoleValueByDevice(string deviceSN);
            /// <summary>
            /// Should use IsDeviceConnectedAndBound to validate deviceIndex before calling this function
            /// </summary>
            int GetBoundRoleValueByConnectedDevice(uint deviceIndex);

            event UnityAction<IMap, MappingChangedEventArg> onRoleValueMappingChanged;
        }

        private sealed class Map : IMap
        {
            private readonly ViveRoleEnum.IInfo m_info;
            private IMapHandler m_handler;
            private bool m_lockInternalMapping;

            // mapping table
            private readonly uint[] m_role2index;
            private readonly int[] m_index2role;

            // binding table
            private readonly IndexedSet<uint>[] m_roleBoundDevices; // connected devices only
            private readonly IndexedTable<string, int> m_sn2role;

            public Map(Type roleType)
            {
                m_info = ViveRoleEnum.GetInfo(roleType);

                m_role2index = new uint[m_info.ValidRoleLength];
                m_index2role = new int[VRModule.MAX_DEVICE_COUNT];

                m_roleBoundDevices = new IndexedSet<uint>[m_info.ValidRoleLength];
                m_sn2role = new IndexedTable<string, int>(Mathf.Min(m_info.ValidRoleLength, (int)VRModule.MAX_DEVICE_COUNT));

                for (int i = 0; i < m_role2index.Length; ++i)
                {
                    m_role2index[i] = VRModule.INVALID_DEVICE_INDEX;
                }

                for (int i = 0; i < m_index2role.Length; ++i)
                {
                    m_index2role[i] = m_info.InvalidRoleValue;
                }
            }

            public ViveRoleEnum.IInfo RoleValueInfo { get { return m_info; } }
            public int BindingCount { get { return m_sn2role.Count; } }
            public IIndexedTableReadOnly<string, int> BindingTable { get { return m_sn2role.ReadOnly; } }

            public IMapHandler Handler
            {
                get { return m_handler; }
                set
                {
                    if (m_handler == value) { return; }

                    if (m_handler != null)
                    {
                        m_handler.OnDivestedOfCurrentMapHandler();
                        m_handler = null;
                    }

                    if (value != null)
                    {
                        if (value.BlockBindings)
                        {
                            UnbindAll();
                        }

                        m_handler = value;
                        m_handler.OnAssignedAsCurrentMapHandler();
                    }
                }
            }

            public event UnityAction<IMap, MappingChangedEventArg> onRoleValueMappingChanged;

            private string DeviceSN(uint deviceIndex) { return VRModule.GetCurrentDeviceState(deviceIndex).serialNumber; }

            public void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected)
            {
                if (connected)
                {
                    if (IsDeviceBound(deviceSN))
                    {
                        InternalInsertRoleBoundDevice(deviceSN, deviceIndex, GetBoundRoleValueByDevice(deviceSN));
                    }
                }
                else
                {
                    if (IsDeviceMapped(deviceIndex))
                    {
                        if (IsDeviceBound(deviceSN))
                        {
                            InternalRemoveRoleBoundDevice(deviceSN, deviceIndex, GetBoundRoleValueByDevice(deviceSN));
                        }

                        if (IsDeviceMapped(deviceIndex))
                        {
                            InternalUnmapping(GetMappedRoleValueByDevice(deviceIndex), deviceIndex);
                        }
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

            #region retrieve state
            public bool IsRoleValueMapped(int roleValue)
            {
                if (!m_info.IsValidRoleValue(roleValue)) { return false; }
                return IsRoleOffsetMapped(m_info.RoleValueToRoleOffset(roleValue));
            }

            public bool IsRoleOffsetMapped(int roleOffset)
            {
                return VRModule.IsValidDeviceIndex(m_role2index[roleOffset]);
            }

            public bool IsDeviceMapped(uint deviceIndex)
            {
                return VRModule.IsValidDeviceIndex(deviceIndex) && m_info.IsValidRoleValue(m_index2role[deviceIndex]);
            }

            public bool IsRoleValueBound(int roleValue)
            {
                if (!m_info.IsValidRoleValue(roleValue)) { return false; }

                var roleOffset = m_info.RoleValueToRoleOffset(roleValue);
                return m_roleBoundDevices[roleOffset] != null && m_roleBoundDevices[roleOffset].Count > 0;
            }

            public bool IsDeviceBound(string deviceSN)
            {
                return string.IsNullOrEmpty(deviceSN) ? false : m_sn2role.ContainsKey(deviceSN);
            }

            public bool IsDeviceConnectedAndBound(uint deviceIndex)
            {
                return IsDeviceBound(DeviceSN(deviceIndex));
            }

            public uint GetMappedDeviceByRoleValue(int roleValue)
            {
                if (m_info.IsValidRoleValue(roleValue))
                {
                    return m_role2index[m_info.RoleValueToRoleOffset(roleValue)];
                }
                else
                {
                    return VRModule.INVALID_DEVICE_INDEX;
                }
            }

            public int GetMappedRoleValueByDevice(uint deviceIndex)
            {
                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    return m_index2role[deviceIndex];
                }
                else
                {
                    return m_info.InvalidRoleValue;
                }
            }

            public string GetBoundDeviceByRoleValue(int roleValue)
            {
                if (!IsRoleValueBound(roleValue)) { return string.Empty; }
                return DeviceSN(GetMappedDeviceByRoleValue(roleValue));
            }

            public int GetBoundRoleValueByDevice(string deviceSN)
            {
                return m_sn2role[deviceSN];
            }

            public int GetBoundRoleValueByConnectedDevice(uint deviceIndex)
            {
                return GetBoundRoleValueByDevice(DeviceSN(deviceIndex));
            }
            #endregion retrieve state

            #region internal operation
            // both roleValue and deviceIndex must be valid
            // ignore binding state
            private void InternalMapping(int roleValue, uint deviceIndex)
            {
                if (m_lockInternalMapping) { throw new Exception("Recursive calling InternalMapping"); }
                m_lockInternalMapping = true;

                var previousRoleValue = m_index2role[deviceIndex];
                if (roleValue == previousRoleValue)
                {
                    m_lockInternalMapping = false;
                    return;
                }

                if (m_info.IsValidRoleValue(previousRoleValue))
                {
                    m_lockInternalMapping = false;
                    InternalUnmapping(previousRoleValue, deviceIndex);
                    m_lockInternalMapping = true;
                }

                var roleOffset = m_info.RoleValueToRoleOffset(roleValue);
                var previousDeviceIndex = m_role2index[roleOffset];
                var eventArg = new MappingChangedEventArg()
                {
                    roleValue = roleValue,
                    previousDeviceIndex = previousDeviceIndex,
                    currentDeviceIndex = deviceIndex,
                };

                m_role2index[roleOffset] = deviceIndex;
                m_index2role[deviceIndex] = roleValue;

                if (VRModule.IsValidDeviceIndex(previousDeviceIndex))
                {
                    m_index2role[previousDeviceIndex] = m_info.InvalidRoleValue;
                }

                if (onRoleValueMappingChanged != null)
                {
                    onRoleValueMappingChanged(this, eventArg);
                }

                m_lockInternalMapping = false;
            }

            // both roleValue and deviceIndex must be valid
            // ignore binding state
            private void InternalUnmapping(int roleValue, uint deviceIndex)
            {
                if (m_lockInternalMapping) { throw new Exception("Recursive calling InternalMapping"); }
                m_lockInternalMapping = true;

                var roleOffset = m_info.RoleValueToRoleOffset(roleValue);
                var eventArg = new MappingChangedEventArg()
                {
                    roleValue = roleValue,
                    previousDeviceIndex = deviceIndex,
                    currentDeviceIndex = VRModule.INVALID_DEVICE_INDEX,
                };

                m_role2index[roleOffset] = VRModule.INVALID_DEVICE_INDEX;
                m_index2role[deviceIndex] = m_info.InvalidRoleValue;

                if (onRoleValueMappingChanged != null)
                {
                    onRoleValueMappingChanged(this, eventArg);
                }

                m_lockInternalMapping = false;
            }

            // device must be valid and connected and have bound role value
            // device must not exist in role bound devices
            // boundRoleValue can be whether valid or not
            private void InternalInsertRoleBoundDevice(string deviceSN, uint deviceIndex, int boundRoleValue)
            {
                if (m_info.IsValidRoleValue(boundRoleValue))
                {
                    var roleBoundDevices = InternalGetRoleBoundDevices(boundRoleValue);

                    roleBoundDevices.Add(deviceIndex); // if key already added here, means that this device already in role bound devices

                    InternalMapping(boundRoleValue, deviceIndex);
                }
            }

            // device must be valid and connected and have bound role value
            // device must already exist in role bound devices
            // boundRoleValue can be whether valid or not
            private void InternalRemoveRoleBoundDevice(string deviceSN, uint deviceIndex, int boundRoleValue)
            {
                if (m_info.IsValidRoleValue(boundRoleValue))
                {
                    var roleBoundDevices = InternalGetRoleBoundDevices(boundRoleValue);

                    if (!roleBoundDevices.Remove(deviceIndex))
                    {
                        throw new Exception("device([" + deviceIndex + "]" + deviceSN + ") has not been InternalMappingRoleBoundDevice");
                    }

                    if (roleBoundDevices.Count > 0)
                    {
                        InternalMapping(boundRoleValue, roleBoundDevices[0]);
                    }
                }
            }

            // deviceSN must be valid
            // device can be whether bound or not
            // device can be whether connected or not
            private void InternalBind(string deviceSN, int roleValue)
            {
                var deviceIndex = VRModule.GetConnectedDeviceIndex(deviceSN);

                bool previousIsBound = false;
                int previousBoundRoleValue = m_info.InvalidRoleValue;
                if (m_sn2role.TryGetValue(deviceSN, out previousBoundRoleValue))
                {
                    if (previousBoundRoleValue == roleValue) { return; }

                    previousIsBound = true;

                    m_sn2role.Remove(deviceSN);

                    if (VRModule.IsValidDeviceIndex(deviceIndex))
                    {
                        InternalRemoveRoleBoundDevice(deviceSN, deviceIndex, previousBoundRoleValue);
                    }
                }

                m_sn2role[deviceSN] = roleValue;

                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    InternalInsertRoleBoundDevice(deviceSN, deviceIndex, roleValue);
                }

                if (m_handler != null)
                {
                    m_handler.OnBindingRoleValueChanged(deviceSN, previousIsBound, previousBoundRoleValue, true, roleValue);
                }
            }

            // deviceSN must be valid
            // device must be bound
            // device can be whether connected or not
            private void InternalUnbind(string deviceSN, int boundRoleValue)
            {
                var deviceIndex = VRModule.GetConnectedDeviceIndex(deviceSN);

                if (!m_sn2role.Remove(deviceSN))
                {
                    throw new Exception("device([" + deviceIndex + "]" + deviceSN + ") already unbound");
                }

                if (VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    InternalRemoveRoleBoundDevice(deviceSN, deviceIndex, boundRoleValue);
                }

                if (m_handler != null)
                {
                    m_handler.OnBindingRoleValueChanged(deviceSN, true, boundRoleValue, false, m_info.InvalidRoleValue);
                }
            }
            #endregion internal operation

            #region mapping
            public void MappingRoleValue(int roleValue, uint deviceIndex)
            {
                if (!m_info.IsValidRoleValue(roleValue))
                {
                    throw new ArgumentException("Cannot mapping invalid roleValue(" + m_info.RoleEnumType.Name + "[" + roleValue + "])");
                }

                if (!VRModule.IsValidDeviceIndex(deviceIndex))
                {
                    throw new ArgumentException("Cannot mapping invalid deviceIndex(" + deviceIndex + ")");
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

            // return true if role is ready for mapping
            public bool UnmappingRoleValue(int roleValue)
            {
                // is mapped?
                if (!IsRoleValueMapped(roleValue)) { return false; }

                // is bound?
                if (IsRoleValueBound(roleValue)) { return false; }

                InternalUnmapping(roleValue, GetMappedDeviceByRoleValue(roleValue));

                return true;
            }

            // return true if device is ready for mapping
            public bool UnmappingDevice(uint deviceIndex)
            {
                // is mapped?
                if (!IsDeviceMapped(deviceIndex)) { return false; }

                // is bound?
                if (IsDeviceConnectedAndBound(deviceIndex)) { return false; }

                InternalUnmapping(GetMappedRoleValueByDevice(deviceIndex), deviceIndex);

                return true;
            }

            public void UnmappingAll()
            {
                for (int roleValue = m_info.MinValidRoleValue; roleValue <= m_info.MaxValidRoleValue; ++roleValue)
                {
                    if (!m_info.IsValidRoleValue(roleValue)) { continue; }

                    UnmappingRoleValue(roleValue);
                }
            }
            #endregion mapping

            #region bind
            [Obsolete("Use BindDeviceToRoleValue instead")]
            public void BindRoleValue(int roleValue, string deviceSN)
            {
                BindDeviceToRoleValue(deviceSN, roleValue);
            }

            public void BindDeviceToRoleValue(string deviceSN, int roleValue)
            {
                if (string.IsNullOrEmpty(deviceSN))
                {
                    throw new ArgumentException("deviceSN cannot be null or empty.");
                }

                if (m_handler != null && m_handler.BlockBindings) { return; }

                InternalBind(deviceSN, roleValue);
            }

            // bind all mapped roles & devices
            public void BindAll()
            {
                if (m_handler != null && m_handler.BlockBindings) { return; }

                for (int roleValue = m_info.MinValidRoleValue; roleValue <= m_info.MaxValidRoleValue; ++roleValue)
                {
                    if (!m_info.IsValidRoleValue(roleValue)) { continue; }

                    if (IsRoleValueMapped(roleValue) && !IsRoleValueBound(roleValue))
                    {
                        InternalBind(DeviceSN(GetMappedDeviceByRoleValue(roleValue)), roleValue);
                    }
                }
            }

            public bool UnbindRoleValue(int roleValue)
            {
                if (!IsRoleValueBound(roleValue)) { return false; }

                var roleBoundDevices = InternalGetRoleBoundDevices(roleValue);
                var boundDeviceIndex = GetMappedDeviceByRoleValue(roleValue);

                // unbind other bound device first, to avoid redundent mapping changes event
                while (roleBoundDevices.Count > 1)
                {
                    for (int i = roleBoundDevices.Count - 1; i >= 0; --i)
                    {
                        if (roleBoundDevices[i] != boundDeviceIndex)
                        {
                            InternalUnbind(DeviceSN(roleBoundDevices[i]), roleValue);
                            break;
                        }
                    }
                };

                if (roleBoundDevices.Count == 1)
                {
                    InternalUnbind(DeviceSN(boundDeviceIndex), roleValue);
                }

                return true;
            }

            public bool UnbindDevice(string deviceSN)
            {
                if (!IsDeviceBound(deviceSN)) { return false; }

                InternalUnbind(deviceSN, GetBoundRoleValueByDevice(deviceSN));

                return true;
            }

            public bool UnbindConnectedDevice(uint deviceIndex)
            {
                return UnbindDevice(DeviceSN(deviceIndex));
            }

            public void UnbindAll()
            {
                for (int i = m_sn2role.Count - 1; i >= 0; --i)
                {
                    UnbindDevice(m_sn2role.GetKeyByIndex(i));
                }
            }

            // roleValue must be valid
            private IndexedSet<uint> InternalGetRoleBoundDevices(int roleValue)
            {
                var roleOffset = m_info.RoleValueToRoleOffset(roleValue);

                if (m_roleBoundDevices[roleOffset] == null)
                {
                    m_roleBoundDevices[roleOffset] = new IndexedSet<uint>();
                }

                return m_roleBoundDevices[roleOffset];
            }
            #endregion bind
        }

        public interface IMap<TRole> : IMap
        {
            ViveRoleEnum.IInfo<TRole> RoleInfo { get; }

            bool IsRoleMapped(TRole role);
            uint GetMappedDeviceByRole(TRole role);
            TRole GetMappedRoleByDevice(uint deviceIndex);

            [Obsolete("Use BindDeviceToRole instead")]
            void BindRole(TRole role, string deviceSN);
            void BindDeviceToRole(string deviceSN, TRole role);
            bool UnbindRole(TRole role); // return true if role is ready for bind

            bool IsRoleBound(TRole role);
            string GetBoundDeviceByRole(TRole role);
            TRole GetBoundRoleByDevice(string deviceSN);
            TRole GetBoundRoleByConnectedDevice(uint deviceIndex);

            event UnityAction<IMap<TRole>, MappingChangedEventArg<TRole>> onRoleMappingChanged;
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
                    Debug.LogWarning("duplicated instance for RoleInfo<" + typeof(TRole).Name + ">");
                }

                m_map.onRoleValueMappingChanged += OnMappingChanged;
            }

            public ViveRoleEnum.IInfo RoleValueInfo { get { return m_map.RoleValueInfo; } }
            public ViveRoleEnum.IInfo<TRole> RoleInfo { get { return m_info; } }
            public IMapHandler Handler { get { return m_map.Handler; } }
            public int BindingCount { get { return m_map.BindingCount; } }
            public IIndexedTableReadOnly<string, int> BindingTable { get { return m_map.BindingTable; } }

            public event UnityAction<IMap, MappingChangedEventArg> onRoleValueMappingChanged { add { m_map.onRoleValueMappingChanged += value; } remove { m_map.onRoleValueMappingChanged -= value; } }

            public event UnityAction<IMap<TRole>, MappingChangedEventArg<TRole>> onRoleMappingChanged;

            private void OnMappingChanged(IMap map, MappingChangedEventArg arg)
            {
                if (onRoleMappingChanged != null)
                {
                    onRoleMappingChanged(this, new MappingChangedEventArg<TRole>()
                    {
                        role = m_info.ToRole(arg.roleValue),
                        previousDeviceIndex = arg.previousDeviceIndex,
                        currentDeviceIndex = arg.currentDeviceIndex,
                    });
                }
            }

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

            [Obsolete("Use BindDeviceToRole instead")]
            public void BindRole(TRole role, string deviceSN) { m_map.BindDeviceToRoleValue(deviceSN, m_info.ToRoleValue(role)); }
            [Obsolete("Use BindDeviceToRoleValue instead")]
            public void BindRoleValue(int roleValue, string deviceSN) { m_map.BindDeviceToRoleValue(deviceSN, roleValue); }
            public void BindDeviceToRole(string deviceSN, TRole role) { m_map.BindDeviceToRoleValue(deviceSN, m_info.ToRoleValue(role)); }
            public void BindDeviceToRoleValue(string deviceSN, int roleValue) { m_map.BindDeviceToRoleValue(deviceSN, roleValue); }
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
                    Debug.LogWarning(roleType.Name + " is not valid ViveRole type. " + validateResult);
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
                    Debug.LogWarning(roleEnumType.Name + " is not valid ViveRole type. " + validateResult);
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
            Initialize();
            GetInternalMap(typeof(TRole)).Handler = mapHandler;
        }
    }
}