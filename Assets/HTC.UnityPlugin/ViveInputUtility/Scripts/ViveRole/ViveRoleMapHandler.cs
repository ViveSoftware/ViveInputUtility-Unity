//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;

namespace HTC.UnityPlugin.Vive
{
    public static partial class ViveRole
    {
        public interface IMapHandler
        {
            bool BlockBindings { get; }
            void OnAssignedAsCurrentMapHandler();
            void OnDivestedOfCurrentMapHandler();
            void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected);
            void OnTrackedDeviceRoleChanged();
            void OnBindingRoleValueChanged(string deviceSN, bool previousIsBound, int previousRoleValue, bool currentIsBound, int currentRoleValue);
        }

        public abstract class MapHandler<TRole> : IMapHandler
        {
            private readonly GenericMap<TRole> m_map;

            public MapHandler()
            {
                m_map = GetInternalMap<TRole>();
            }

            public IMap<TRole> RoleMap { get { return m_map; } }
            public ViveRoleEnum.IInfo<TRole> RoleInfo { get { return m_map.RoleInfo; } }
            public bool IsCurrentMapHandler { get { return m_map.Handler == this; } }
            public virtual bool BlockBindings { get { return false; } }

            public virtual void OnAssignedAsCurrentMapHandler() { }
            public virtual void OnDivestedOfCurrentMapHandler() { }
            public virtual void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected) { }
            public virtual void OnTrackedDeviceRoleChanged() { }
            public virtual void OnBindingChanged(string deviceSN, bool previousIsBound, TRole previousRole, bool currentIsBound, TRole currentRole) { }

            public void OnBindingRoleValueChanged(string deviceSN, bool previousIsBound, int previousRoleValue, bool currentIsBound, int currentRoleValue)
            {
                OnBindingChanged(deviceSN, previousIsBound, m_map.RoleInfo.ToRole(previousRoleValue), currentIsBound, m_map.RoleInfo.ToRole(currentRoleValue));
            }

            protected void MappingRole(TRole role, uint deviceIndex)
            {
                if (!IsCurrentMapHandler) { return; }
                m_map.MappingRole(role, deviceIndex);
            }

            protected void MappingRoleIfUnbound(TRole role, uint deviceIndex)
            {
                if (!RoleMap.IsRoleBound(role) && !RoleMap.IsDeviceConnectedAndBound(deviceIndex))
                {
                    MappingRole(role, deviceIndex);
                }
            }

            // return true if role is ready for mapping
            protected bool UnmappingRole(TRole role)
            {
                if (!IsCurrentMapHandler) { return false; }
                return m_map.UnmappingRole(role);
            }

            // return true if device is ready for mapping
            protected bool UnmappingDevice(uint deviceIndex)
            {
                if (!IsCurrentMapHandler) { return false; }
                return m_map.UnmappingDevice(deviceIndex);
            }

            protected void UnmappingAll()
            {
                if (!IsCurrentMapHandler) { return; }
                m_map.UnmappingAll();
            }
        }
    }
}