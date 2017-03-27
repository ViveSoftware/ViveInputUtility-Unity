//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    public static partial class ViveRole
    {
        public interface IMapHandler
        {
            void OnInitialize();
            void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected);
            void OnTrackedDeviceRoleChanged();
            void OnBindingRoleValueChanged(int roleValue, string deviceSN, bool bound);
        }

        public abstract class MapHandler<TRole> : IMapHandler
        {
            private readonly GenericMap<TRole> m_map;

            public MapHandler()
            {
                m_map = GetInternalMap<TRole>();
            }

            public IMap<TRole> RoleMap { get { return m_map; } }
            public bool IsCurrentMapHandler { get { return m_map.Handler == this; } }

            public abstract void OnInitialize();
            public abstract void OnConnectedDeviceChanged(uint deviceIndex, ETrackedDeviceClass deviceClass, string deviceSN, bool connected);
            public abstract void OnTrackedDeviceRoleChanged();
            public virtual void OnBindingRoleValueChanged(int roleValue, string deviceSN, bool bound) { OnBindingChanged(m_map.RoleInfo.ToRole(roleValue), deviceSN, bound); }
            public abstract void OnBindingChanged(TRole role, string deviceSN, bool bound);

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