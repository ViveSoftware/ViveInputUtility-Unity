//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public abstract partial class LiteCoroutine : IEnumerator
    {
        private static bool isManagerCreated;
        private static Manager defaultManager;

        public static LiteCoroutineManager DefaultManager { get { return defaultManager; } }

        public static event Action DelayUpdateCall { add { if (Initialize()) { defaultManager.DelayUpdateCall += value; } } remove { if (isManagerCreated) { defaultManager.DelayUpdateCall -= value; } } }

        public static event Action DelayLateUpdateCall { add { if (Initialize()) { defaultManager.DelayLateUpdateCall += value; } } remove { if (isManagerCreated) { defaultManager.DelayLateUpdateCall -= value; } } }

        public static event Action DelayFixedUpdateCall { add { if (Initialize()) { defaultManager.DelayFixedUpdateCall += value; } } remove { if (isManagerCreated) { defaultManager.DelayFixedUpdateCall -= value; } } }

        public static event Action DelayEndOfFrameCall { add { if (Initialize()) { defaultManager.DelayEndOfFrameCall += value; } } remove { if (isManagerCreated) { defaultManager.DelayEndOfFrameCall -= value; } } }

        public static bool Initialize(bool wake = false)
        {
            if (isManagerCreated)
            {
                if (wake) { ManagerUpdater.WakeUp(); }
                return true;
            }
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogError("LiteCoroutine only supported in playing mode");
                return false;
            }
#endif
            defaultManager = new Manager();
            if (wake) { ManagerUpdater.WakeUp(); }
            return isManagerCreated = true;
        }

        public static LiteCoroutineManager CreateManager()
        {
            return new Manager();
        }

        public static bool IsAwake
        {
            get { return ManagerUpdater.IsAwake; }
        }

        public static bool StayAwake
        {
            get { return ManagerUpdater.StayAwake; }
            set { ManagerUpdater.StayAwake = value; }
        }

        public static void WakeUp() { ManagerUpdater.WakeUp(); }

        public static LiteCoroutine StartCoroutine(IEnumerator routine) { return StartCoroutine(routine, true, true); }

        public static LiteCoroutine StartCoroutine(IEnumerator routine, bool runImmediate) { return StartCoroutine(routine, runImmediate, true); }

        /// <summary>
        /// Routine will start running until CoroutineSystem is enabled
        /// wakeSystem should only set to true in main thread call
        /// If StayAwake is false and IsSleeping is true, coroutine won't start if wakeSystem is set to false
        /// </summary>
        public static LiteCoroutine StartCoroutine(IEnumerator routine, bool runImmediate, bool wakeSystem)
        {
            if (!Initialize()) { return null; }

            var handle = defaultManager.StartCoroutine(routine, runImmediate);

            if (!handle.IsDone && wakeSystem) { WakeUp(); }

            return handle;
        }

        public static LiteCoroutine StartCoroutine(ref LiteCoroutine handle, IEnumerator routine) { return StartCoroutine(ref handle, routine, true, true); }

        public static LiteCoroutine StartCoroutine(ref LiteCoroutine handle, IEnumerator routine, bool runImmediate) { return StartCoroutine(ref handle, routine, runImmediate, true); }

        public static LiteCoroutine StartCoroutine(ref LiteCoroutine handle, IEnumerator routine, bool runImmediate, bool wakeSystem)
        {
            if (!Initialize()) { return null; }

            defaultManager.StartCoroutine(ref handle, routine, runImmediate);

            if (!handle.IsDone && wakeSystem) { WakeUp(); }

            return handle;
        }

        public static void StopCoroutine(LiteCoroutine handle)
        {
            if (handle == null) { return; }
            handle.Stop();
        }
    }
}