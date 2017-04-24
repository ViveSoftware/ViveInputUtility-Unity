//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using Valve.VR;

namespace HTC.UnityPlugin.Vive
{
    public interface INewPoseListener
    {
        void BeforeNewPoses();
        void OnNewPoses();
        void AfterNewPoses();
    }

    /// <summary>
    /// To manage all NewPoseListeners
    /// </summary>
    public static partial class VivePose
    {
        private static bool s_initialized;
        private static bool s_hasFocus;

        private static readonly Pose[] s_poses = new Pose[OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] s_rawPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] s_rawGamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        private static IndexedSet<INewPoseListener> s_listeners = new IndexedSet<INewPoseListener>();

        static VivePose()
        {
            Initialize();
        }

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (s_initialized || !Application.isPlaying) { return; }
            s_initialized = true;

            var system = OpenVR.System;
            if (system != null)
            {
                OnInputFocus(!system.IsInputFocusCapturedByAnotherProcess());
            }
            else
            {
                OnInputFocus(true);
            }

            SteamVR_Events.InputFocusAction(OnInputFocus).Enable(true);
            Camera.onPreCull += OnCameraPreCull;
        }

        public static bool AddNewPosesListener(INewPoseListener listener)
        {
            return s_listeners.AddUnique(listener);
        }

        public static bool RemoveNewPosesListener(INewPoseListener listener)
        {
            return s_listeners.Remove(listener);
        }

        private static void OnInputFocus(bool arg)
        {
            s_hasFocus = arg;
        }

        private static void OnCameraPreCull(Camera cam)
        {
            var compositor = OpenVR.Compositor;
            if (compositor != null)
            {
                compositor.GetLastPoses(s_rawPoses, s_rawGamePoses);
            }
            else
            {
                for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; ++i) { s_rawPoses[i] = default(TrackedDevicePose_t); }
            }

            var tempListeners = ListPool<INewPoseListener>.Get();
            tempListeners.AddRange(s_listeners);

            for (int i = tempListeners.Count - 1; i >= 0; --i)
            {
                tempListeners[i].BeforeNewPoses();
            }

            for (int i = s_rawPoses.Length - 1; i >= 0; --i)
            {
                if (!s_rawPoses[i].bDeviceIsConnected || !s_rawPoses[i].bPoseIsValid) { continue; }
                s_poses[i] = new Pose(s_rawPoses[i].mDeviceToAbsoluteTracking);
            }

            for (int i = tempListeners.Count - 1; i >= 0; --i)
            {
                tempListeners[i].OnNewPoses();
            }

            for (int i = tempListeners.Count - 1; i >= 0; --i)
            {
                tempListeners[i].AfterNewPoses();
            }

            ListPool<INewPoseListener>.Release(tempListeners);
        }
    }
}