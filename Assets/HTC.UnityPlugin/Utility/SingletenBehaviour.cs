using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [DisallowMultipleComponent]
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T s_instance = null;
        private static bool s_isApplicationQuitting = false;
        private static object s_lock = new object();

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
            if (s_instance != null || s_isApplicationQuitting) { return; }

            lock (s_lock)
            {
                var instances = FindObjectsOfType<T>();
                if (instances.Length > 0)
                {
                    s_instance = instances[0];
                    if (instances.Length > 1) { Debug.LogWarning("Multiple " + typeof(T).Name + " not supported!"); }
                }

                if (s_instance == null)
                {
                    s_instance = new GameObject("[" + typeof(T).Name + "]").AddComponent<T>();
                }

                s_instance.OnSingletonBehaviourInitialized();
            }
        }

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