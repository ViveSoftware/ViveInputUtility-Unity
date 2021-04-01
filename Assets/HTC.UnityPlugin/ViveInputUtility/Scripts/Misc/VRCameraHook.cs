//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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
    [AddComponentMenu("VIU/Hooks/VR Camera Hook", 10)]
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

        private static Type[] s_creatorTypes;
        private CameraCreator[] m_creators;

        private static void FindAllCameraCreatorTypes()
        {
            if (s_creatorTypes != null) { return; }

            try
            {
                var creatorBaseType = typeof(CameraCreator);
                var creatorTypes = new List<Type>();
                var currentAsm = creatorBaseType.Assembly;
                var currentAsmName = currentAsm.GetName().Name;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var referencingCurrentAsm = false;

                    if (asm == currentAsm)
                    {
                        referencingCurrentAsm = true;
                    }
                    else
                    {
                        foreach (var asmref in asm.GetReferencedAssemblies())
                        {
                            if (asmref.Name == currentAsmName)
                            {
                                referencingCurrentAsm = true;
                                break;
                            }
                        }
                    }

                    if (referencingCurrentAsm)
                    {
                        foreach (var type in asm.GetTypes().Where(t => t.IsSubclassOf(creatorBaseType) && !t.IsAbstract))
                        {
                            creatorTypes.Add(type);
                        }
                    }
                }

                creatorTypes.Sort((x, y) => GetCreatorPriority(x) - GetCreatorPriority(y));
                s_creatorTypes = creatorTypes.ToArray();
            }
            catch (Exception e)
            {
                s_creatorTypes = new Type[0];
                Debug.LogError(e);
            }
        }

        private static int GetCreatorPriority(Type t, int defaultValue = 0)
        {
            foreach (var at in t.GetCustomAttributes(typeof(CreatorPriorityAttirbute), true))
            {
                return ((CreatorPriorityAttirbute)at).priority;
            }
            return defaultValue;
        }

        private void Awake()
        {
            FindAllCameraCreatorTypes();

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