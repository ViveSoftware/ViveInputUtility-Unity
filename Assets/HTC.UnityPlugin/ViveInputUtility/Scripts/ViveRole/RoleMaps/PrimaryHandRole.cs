//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.LiteCoroutineSystem;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections;
using System.Collections.Generic;

namespace HTC.UnityPlugin.Vive
{
    [ViveRoleEnum((int)PrimaryHandRole.Invalid)]
    public enum PrimaryHandRole
    {
        Invalid = -1,
        Primary,
        Secondary,
        Tertiary,
        Quaternary,
        Quinary,
        Senary,
        Septenary,
        Octonary,
        Nonary,
        Denary,
    }

    internal class PrimaryHandRoleIntReslver : EnumToIntResolver<PrimaryHandRole> { public override int Resolve(PrimaryHandRole e) { return (int)e; } }

    public class PrimaryHandRoleHandler : ViveRole.MapHandler<PrimaryHandRole>
    {
        public enum Handed
        {
            Right,
            Left,
        }

        private readonly List<VRModuleDeviceClass> appliedDeviceClasses = new List<VRModuleDeviceClass>()
        {
            VRModuleDeviceClass.Controller,
            VRModuleDeviceClass.TrackedHand,
            VRModuleDeviceClass.GenericTracker,
            //VRModuleDeviceClass.HMD,
        };
        private UnmappedRoles unmappedRoles = new UnmappedRoles();
        private PrioritizedDevices prioritizedDevices = new PrioritizedDevices();

        public Handed DominantHand { get; private set; }

        public List<VRModuleDeviceClass> AppliedDeviceClasses { get { return appliedDeviceClasses; } }

        public void SetRightDominantAndRefresh() { SetRightDominantAndRefresh(true); }

        public void SetRightDominantAndRefresh(bool delayRefresh)
        {
            if (DominantHand != Handed.Right)
            {
                SetDominantAndRefresh(Handed.Right, delayRefresh);
            }
        }

        public void SetLeftDominantAndRefresh() { SetLeftDominantAndRefresh(true); }

        public void SetLeftDominantAndRefresh(bool delayRefresh)
        {
            if (DominantHand != Handed.Left)
            {
                SetDominantAndRefresh(Handed.Left, delayRefresh);
            }
        }

        public void SwapDominantHandAndRefresh() { SwapDominantHandAndRefresh(true); }

        public void SwapDominantHandAndRefresh(bool delayRefresh)
        {
            SetDominantAndRefresh(DominantHand == Handed.Right ? Handed.Left : Handed.Right, delayRefresh);
        }

        private void SetDominantAndRefresh(Handed hand, bool delayRefresh)
        {
            DominantHand = hand;
            if (delayRefresh)
            {
                DelayRefresh();
            }
            else
            {
                Refresh();
            }
        }

        public void DelayRefresh()
        {
            // avoid multiple refreshes in one frame
            LiteCoroutine.DelayUpdateCall -= Refresh;
            LiteCoroutine.DelayUpdateCall += Refresh;
        }

        public override void OnAssignedAsCurrentMapHandler() { Refresh(); }

        public override void OnTrackedDeviceRoleChanged() { Refresh(); }

        public override void OnConnectedDeviceChanged(uint deviceIndex, VRModuleDeviceClass deviceClass, string deviceSN, bool connected) { Refresh(); }

        public override void OnBindingChanged(string deviceSN, bool previousIsBound, PrimaryHandRole previousRole, bool currentIsBound, PrimaryHandRole currentRole) { Refresh(); }

        public void Refresh()
        {
            UnmappingAll();

            unmappedRoles.Reset(this);
            prioritizedDevices.Reset(this);

            while (unmappedRoles.MoveNext() && prioritizedDevices.MoveNext())
            {
                MappingRole(unmappedRoles.Current, prioritizedDevices.Current);
            }
        }

        private class UnmappedRoles : IEnumerator<PrimaryHandRole>
        {
            private PrimaryHandRoleHandler handler;
            private PrimaryHandRole current = PrimaryHandRole.Invalid;
            public PrimaryHandRole Current { get { return current; } }
            object IEnumerator.Current { get { return Current; } }

            public void Reset(PrimaryHandRoleHandler handler)
            {
                this.handler = handler;
                Reset();
            }

            public bool MoveNext()
            {
                while (++current <= PrimaryHandRole.Denary)
                {
                    if (handler.RoleMap.IsRoleMapped(Current)) { continue; }
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                current = PrimaryHandRole.Invalid;
            }

            public void Dispose()
            {
                handler = null;
                current = PrimaryHandRole.Invalid;
            }
        }

        private class PrioritizedDevices : IEnumerator<uint>
        {
            private PrimaryHandRoleHandler handler;
            private List<uint> devices = new List<uint>();
            private int currentIndex = -1;
            public uint Current { get { return devices[currentIndex]; } }
            object IEnumerator.Current { get { return Current; } }

            public void Reset(PrimaryHandRoleHandler handler)
            {
                Reset();

                this.handler = handler;
                var moduleRight = VRModule.GetRightControllerDeviceIndex();
                var moduleLeft = VRModule.GetLeftControllerDeviceIndex();
                if (handler.DominantHand == Handed.Right)
                {
                    TryAddDevice(moduleRight);
                    TryAddDevice(moduleLeft);
                }
                else
                {
                    TryAddDevice(moduleLeft);
                    TryAddDevice(moduleRight);
                }

                foreach (var deviceClass in handler.appliedDeviceClasses)
                {
                    for (uint i = 0u, imax = VRModule.GetDeviceStateCount(); i < imax; ++i)
                    {
                        TryAddDevice(i, deviceClass);
                    }
                }
            }

            private void TryAddDevice(uint device, VRModuleDeviceClass deviceClass = VRModuleDeviceClass.Invalid)
            {
                if (!VRModule.IsValidDeviceIndex(device)) { return; }
                if (handler.RoleMap.IsDeviceMapped(device)) { return; }
                var state = VRModule.GetDeviceState(device);
                if (!state.isConnected) { return; }
                if (deviceClass != VRModuleDeviceClass.Invalid && state.deviceClass != deviceClass) { return; }
                if (devices.IndexOf(device) >= 0) { return; }
                devices.Add(device);
            }

            public bool MoveNext()
            {
                while (++currentIndex < devices.Count)
                {
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                devices.Clear();
                currentIndex = -1;
            }

            public void Dispose()
            {
                handler = null;
                devices = null;
            }
        }
    }
}