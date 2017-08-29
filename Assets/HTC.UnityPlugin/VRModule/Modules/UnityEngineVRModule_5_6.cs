//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

#if UNITY_5_6_OR_NEWER && !UNITY_2017_2_OR_NEWER
using UnityEngine.VR;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public sealed partial class UnityEngineVRModule : VRModule.ModuleBase
    {
#if UNITY_5_6_OR_NEWER && !UNITY_2017_2_OR_NEWER
        private TrackingSpaceType m_prevTrackingSpace;

        private void SaveTrackingSpaceType()
        {
            m_prevTrackingSpace = VRDevice.GetTrackingSpaceType();
        }

        private void LoadTrackingSpaceType()
        {
            VRDevice.SetTrackingSpaceType(m_prevTrackingSpace);
        }

        public override void UpdateTrackingSpaceType()
        {
            switch (VRModule.trackingSpaceType)
            {
                case VRModuleTrackingSpaceType.Stationary:
                    VRDevice.SetTrackingSpaceType(TrackingSpaceType.Stationary);
                    break;
                case VRModuleTrackingSpaceType.RoomScale:
                    VRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
                    break;
            }
        }
#endif
    }
}