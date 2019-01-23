//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [DisallowMultipleComponent]
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T s_instance = null;
        private static bool s_isApplicationQuitting = false;
        private static object s_lock = new object();
        private static Func<GameObject> s_defaultInitGOGetter;

        private bool m_initialized;

        public static bool Active { get { return !s_isApplicationQuitting && s_instance != null; } }

        public bool IsInstance { get { return this == Instance; } }

        public static T Instance
        {
            get
            {
                Initialize();
                return s_instance;
            }
        }

        public static void Initialize()
        {
            if (!Application.isPlaying || s_isApplicationQuitting) { return; }

            lock (s_lock)
            {
                if (s_instance != null) { return; }

                var instances = FindObjectsOfType<T>();
                if (instances.Length > 0)
                {
                    s_instance = instances[0];
                    if (instances.Length > 1) { Debug.LogWarning("Multiple " + typeof(T).Name + " not supported!"); }
                }

                if (s_instance == null)
                {
                    GameObject defaultInitGO = null;

                    if (s_defaultInitGOGetter != null)
                    {
                        defaultInitGO = s_defaultInitGOGetter();
                    }

                    if (defaultInitGO == null)
                    {
                        defaultInitGO = new GameObject("[" + typeof(T).Name + "]");
                    }

                    s_instance = defaultInitGO.AddComponent<T>();
                }

                if (!s_instance.m_initialized)
                {
                    s_instance.m_initialized = true;
                    s_instance.OnSingletonBehaviourInitialized();
                }
            }
        }

        /// <summary>
        /// Must set before the instance being initialized
        /// </summary>
        public static void SetDefaultInitGameObjectGetter(Func<GameObject> getter) { s_defaultInitGOGetter = getter; }

        protected virtual void OnSingletonBehaviourInitialized() { }

        protected virtual void OnApplicationQuit()
        {
            s_isApplicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (!s_isApplicationQuitting && s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}