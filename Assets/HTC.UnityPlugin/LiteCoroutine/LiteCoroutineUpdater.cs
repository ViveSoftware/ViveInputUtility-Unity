//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections;
using UnityEngine;

namespace HTC.UnityPlugin.LiteCoroutineSystem
{
    public abstract partial class LiteCoroutine : IEnumerator
    {
        private sealed class ManagerUpdater : MonoBehaviour
        {
            private static object stateLock;
            private static bool isQuitting;
            private static bool isAwake;
            private static bool stayAwake;
            private static bool isInstanceCreated;
            private static ManagerUpdater instance;
            private static Coroutine endOfFrameCoroutine;

            public static bool IsAwake
            {
                get { return isAwake; }
            }

            public static bool StayAwake
            {
                get { return stayAwake; }
                set
                {
                    stayAwake = value;
                    if (value) { WakeUp(); }
                }
            }

            static ManagerUpdater()
            {
                stateLock = new object();
            }

            private static void OnQuitting()
            {
                lock (stateLock)
                {
                    isQuitting = true;
                    instance = null;
                }
            }

            public static void WakeUp()
            {
                lock (stateLock)
                {
                    if (isAwake) { return; }
                    if (!isInstanceCreated)
                    {
                        if (isQuitting) { return; }
                        try { instance = CreateHiddenInstance(); }
                        catch (Exception e) { Debug.LogWarning("Caught exception while creating hidden UpdaterBehaviour.\n" + e.Message + "\n" + e.StackTrace); return; }
                        if (instance == null) { return; }
                        isInstanceCreated = true;
                    }
                    instance.gameObject.SetActive(true);
                    isAwake = true;

                    if (endOfFrameCoroutine == null)
                    {
                        endOfFrameCoroutine = instance.StartCoroutine(instance.EndOfFrameUpdate());
                    }
                }
            }

            // won't work if StayAwake set to true
            public static void GotoSleep()
            {
                lock (stateLock)
                {
                    if (stayAwake) { return; }
                    if (!isAwake) { return; }
                    if (!isInstanceCreated) { return; }
                    instance.gameObject.SetActive(false);
                    isAwake = false;
                }
            }

            private static ManagerUpdater CreateHiddenInstance()
            {
                var obj = new GameObject("[LiteCoroutineBehaviour]") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(obj);
                obj.SetActive(false);
                return obj.AddComponent<ManagerUpdater>();
            }

            private void Update()
            {
                if (isManagerCreated)
                {
                    defaultManager.MainUpdate();

                    if (defaultManager.CoroutineCount == 0 && defaultManager.NoDelayCalls && !stayAwake)
                    {
                        GotoSleep();
                    }
                }
            }

            private void LateUpdate()
            {
                if (isManagerCreated)
                {
                    defaultManager.LateUpdate();
                }
            }

            private void FixedUpdate()
            {
                if (isManagerCreated)
                {
                    defaultManager.FixedeUpdate();
                }
            }

            private IEnumerator EndOfFrameUpdate()
            {
                while (true)
                {
                    yield return new WaitForEndOfFrame();
                    defaultManager.EndOfFrameUpdate();
                }
            }

            private void OnApplicationQuit()
            {
                OnQuitting();
            }
        }
    }
}