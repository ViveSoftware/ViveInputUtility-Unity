//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    // This script creates and handles SteamVR_RenderModel using viveRole property or device index
    [DisallowMultipleComponent]
    [AddComponentMenu("HTC/VIU/Hooks/Render Model Hook", 10)]
    public class RenderModelHook : MonoBehaviour, IViveRoleComponent
    {
        [AttributeUsage(AttributeTargets.Class)]
        public class CreatorPriorityAttirbute : Attribute
        {
            public int priority { get; set; }
            public CreatorPriorityAttirbute(int priority = 0) { this.priority = priority; }
        }

        public abstract class RenderModelCreator
        {
            public abstract bool shouldActive { get; }
            protected RenderModelHook hook { get; private set; }

            public void Initialize(RenderModelHook hook) { this.hook = hook; }
            public abstract void UpdateRenderModel();
            public abstract void CleanUpRenderModel();
        }

        [CreatorPriorityAttirbute(1)]
        private class DefaultRenderModelCreator : RenderModelCreator
        {
            private VRModuleDeviceModel m_loadedModelEnum = (VRModuleDeviceModel)(-1);
            private GameObject m_model;

            public override bool shouldActive { get { return true; } }

            public override void UpdateRenderModel()
            {
                var index = hook.GetModelDeviceIndex();

                if (!VRModule.IsValidDeviceIndex(index))
                {
                    if (m_model != null)
                    {
                        m_model.SetActive(false);
                    }
                }
                else
                {
                    var loadModelEnum = VRModuleDeviceModel.Unknown;
                    if (hook.m_overrideModel != OverrideModelEnum.DontOverride)
                    {
                        loadModelEnum = (VRModuleDeviceModel)hook.m_overrideModel;
                    }
                    else
                    {
                        loadModelEnum = VRModule.GetCurrentDeviceState(index).deviceModel;
                    }

                    if (ChangeProp.Set(ref m_loadedModelEnum, loadModelEnum))
                    {
                        CleanUpRenderModel();

                        var prefab = Resources.Load<GameObject>("Models/VIUModel" + m_loadedModelEnum.ToString());
                        if (prefab != null)
                        {
                            m_model = Instantiate(prefab);
                            m_model.transform.SetParent(hook.transform, false);
                            m_model.gameObject.name = "VIUModel" + m_loadedModelEnum.ToString();

                            if (hook.m_overrideShader != null)
                            {
                                var renderer = m_model.GetComponentInChildren<Renderer>();
                                if (renderer != null)
                                {
                                    renderer.material.shader = hook.m_overrideShader;
                                }
                            }
                        }
                    }

                    if (m_model != null)
                    {
                        m_model.SetActive(true);
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_model != null)
                {
                    Destroy(m_model);
                    m_model = null;
                }
            }
        }

        public enum Mode
        {
            Disable,
            ViveRole,
            DeivceIndex,
        }

        public enum Index
        {
            None = -1,
            Hmd,
            Device1,
            Device2,
            Device3,
            Device4,
            Device5,
            Device6,
            Device7,
            Device8,
            Device9,
            Device10,
            Device11,
            Device12,
            Device13,
            Device14,
            Device15,
        }

        public enum OverrideModelEnum
        {
            DontOverride = VRModuleDeviceModel.Unknown,
            ViveController = VRModuleDeviceModel.ViveController,
            ViveTracker = VRModuleDeviceModel.ViveTracker,
            ViveBaseStation = VRModuleDeviceModel.ViveBaseStation,
            OculusTouchLeft = VRModuleDeviceModel.OculusTouchLeft,
            OculusTouchRight = VRModuleDeviceModel.OculusTouchRight,
            OculusSensor = VRModuleDeviceModel.OculusSensor,
            KnucklesLeft = VRModuleDeviceModel.KnucklesLeft,
            KnucklesRight = VRModuleDeviceModel.KnucklesRight,
            OculusGoController = VRModuleDeviceModel.OculusGoController,
            OculusGearVrController = VRModuleDeviceModel.OculusGearVrController,
            WMRControllerLeft = VRModuleDeviceModel.WMRControllerLeft,
            WMRControllerRight = VRModuleDeviceModel.WMRControllerRight,
        }

        [SerializeField]
        private Mode m_mode = Mode.ViveRole;
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private Transform m_origin;
        [SerializeField]
        private Index m_deviceIndex = Index.Hmd;
        [SerializeField]
        private OverrideModelEnum m_overrideModel = OverrideModelEnum.DontOverride;
        [SerializeField]
        private Shader m_overrideShader = null;

        private static readonly Type[] s_creatorTypes;
        private RenderModelCreator[] m_creators;
        private int m_activeCreatorIndex = -1;
        private int m_defaultCreatorIndex = -1;
        private bool m_isQuiting;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }

        public Transform origin { get { return m_origin; } set { m_origin = value; } }

        public bool applyTracking { get; set; }

        public OverrideModelEnum overrideModel { get { return m_overrideModel; } set { m_overrideModel = value; } }

        public Shader overrideShader { get { return m_overrideShader; } set { m_overrideShader = value; } }

        static RenderModelHook()
        {
            try
            {
                var creatorTypes = new List<Type>();
                foreach (var type in Assembly.GetAssembly(typeof(RenderModelCreator)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RenderModelCreator))))
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled && Application.isPlaying && VRModule.Active)
            {
                UpdateModel();
            }
        }
#endif
        private void Awake()
        {
            m_creators = new RenderModelCreator[s_creatorTypes.Length];
            for (int i = s_creatorTypes.Length - 1; i >= 0; --i)
            {
                m_creators[i] = (RenderModelCreator)Activator.CreateInstance(s_creatorTypes[i]);
                m_creators[i].Initialize(this);

                if (s_creatorTypes[i] == typeof(DefaultRenderModelCreator))
                {
                    m_defaultCreatorIndex = i;
                }
            }
        }

        protected virtual void OnEnable()
        {
            VRModule.onActiveModuleChanged += OnActiveModuleChanged;
            m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;

            UpdateModel();
        }

        protected virtual void OnDisable()
        {
            VRModule.onActiveModuleChanged -= OnActiveModuleChanged;
            m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;

            UpdateModel();
        }

        private void OnDeviceIndexChanged(uint deviceIndex) { UpdateModel(); }

        private void OnActiveModuleChanged(VRModuleActiveEnum module) { UpdateModel(); }

        private void OnApplicationQuit() { m_isQuiting = true; }

        public uint GetModelDeviceIndex()
        {
            if (!enabled) { return VRModule.INVALID_DEVICE_INDEX; }

            uint result;
            switch (m_mode)
            {
                case Mode.ViveRole:
                    result = m_viveRole.GetDeviceIndex();
                    break;
                case Mode.DeivceIndex:
                    result = (uint)m_deviceIndex;
                    break;
                case Mode.Disable:
                default:
                    return VRModule.INVALID_DEVICE_INDEX;
            }

            return result;
        }

        private void UpdateModel()
        {
            if (m_isQuiting) { return; }

            var activeCreatorIndex = -1;
            if (enabled)
            {
                if (m_overrideModel == OverrideModelEnum.DontOverride)
                {
                    for (int i = 0, imax = m_creators.Length; i < imax; ++i)
                    {
                        if (m_creators[i].shouldActive)
                        {
                            activeCreatorIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    activeCreatorIndex = m_defaultCreatorIndex;
                }
            }

            if (m_activeCreatorIndex != activeCreatorIndex)
            {
                // clean up model created from previous active creator
                if (m_activeCreatorIndex >= 0)
                {
                    m_creators[activeCreatorIndex].CleanUpRenderModel();
                }
                m_activeCreatorIndex = activeCreatorIndex;
            }

            if (m_activeCreatorIndex >= 0)
            {
                m_creators[activeCreatorIndex].UpdateRenderModel();
            }
        }
    }
}