//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using UnityEngine;

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
    public partial class VivePose : SingletonBehaviour<VivePose>
    {
        private static IndexedSet<INewPoseListener> s_listeners = new IndexedSet<INewPoseListener>();

        [SerializeField]
        private bool m_dontDestroyOnLoad = false;

        static VivePose()
        {
            SetDefaultInitGameObjectGetter(VRModule.GetInstanceGameObject);
        }

        protected override void OnSingletonBehaviourInitialized()
        {
            if (m_dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            VRModule.onNewPoses += OnDeviceStateUpdated;
        }

        protected override void OnDestroy()
        {
            if (IsInstance)
            {
                VRModule.onNewPoses -= OnDeviceStateUpdated;
            }

            base.OnDestroy();
        }

        public static bool AddNewPosesListener(INewPoseListener listener)
        {
            Initialize();
            return s_listeners.AddUnique(listener);
        }

        public static bool RemoveNewPosesListener(INewPoseListener listener)
        {
            return s_listeners.Remove(listener);
        }

        private void OnDeviceStateUpdated()
        {
            var tempListeners = ListPool<INewPoseListener>.Get();
            tempListeners.AddRange(s_listeners);

            for (int i = tempListeners.Count - 1; i >= 0; --i)
            {
                tempListeners[i].BeforeNewPoses();
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