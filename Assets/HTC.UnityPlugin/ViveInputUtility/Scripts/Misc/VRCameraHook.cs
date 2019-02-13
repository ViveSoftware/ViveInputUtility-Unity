//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    /// <summary>
    /// This componenet hooks up custom VR camera required component
    /// </summary>
    [AddComponentMenu("HTC/VIU/Hooks/VR Camera Hook", 10)]
    public class VRCameraHook : MonoBehaviour
    {
        [AttributeUsage(AttributeTargets.Class)]
        public class CreatorPriorityAttirbute : Attribute
        {
            public int priority { get; set; }
            public CreatorPriorityAttirbute(int priority = 0) { this.priority = priority; }
        }

        public abstract class CameraCreator
        {
            public abstract bool shouldActive { get; }
            public abstract void CreateCamera(VRCameraHook hook);
        }

        private static readonly Type[] s_creatorTypes;
        private CameraCreator[] m_creators;

        static VRCameraHook()
        {
            try
            {
                var creatorTypes = new List<Type>();
                foreach (var type in Assembly.GetAssembly(typeof(CameraCreator)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(CameraCreator))))
                {
                    creatorTypes.Add(type);
                }
                s_creatorTypes = creatorTypes.OrderBy(t =>
                {
                    foreach (var at in t.GetCustomAttributes(typeof(CreatorPriorityAttirbute), true))
                    {
                        return ((CreatorPriorityAttirbute)at).priority;
                    }
                    return 0;
                }).ToArray();
            }
            catch (Exception e)
            {
                s_creatorTypes = new Type[0];
                Debug.LogError(e);
            }
        }

        private void Awake()
        {
            m_creators = new CameraCreator[s_creatorTypes.Length];
            for (int i = s_creatorTypes.Length - 1; i >= 0; --i)
            {
                m_creators[i] = (CameraCreator)Activator.CreateInstance(s_creatorTypes[i]);
            }

            if (VRModule.activeModule == VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged += OnModuleActivated;
            }
            else
            {
                OnModuleActivated(VRModule.activeModule);
            }
        }

        private void OnModuleActivated(VRModuleActiveEnum activatedModule)
        {
            foreach (var creator in m_creators)
            {
                if (creator.shouldActive)
                {
                    creator.CreateCamera(this);
                    break;
                }
            }

            if (activatedModule != VRModuleActiveEnum.Uninitialized)
            {
                VRModule.onActiveModuleChanged -= OnModuleActivated;
            }
        }
    }
}