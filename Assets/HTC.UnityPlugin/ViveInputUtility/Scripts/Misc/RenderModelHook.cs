//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

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
    [AddComponentMenu("VIU/Hooks/Render Model Hook", 10)]
    public class RenderModelHook : MonoBehaviour, IViveRoleComponent
    {
        public interface ICustomModel
        {
            void OnAfterModelCreated(RenderModelHook hook);
            /// <summary>
            /// If from all components return true, hook will perform SetActive(true) on this model after this message
            /// </summary>
            bool OnBeforeModelActivated(RenderModelHook hook);
            /// <summary>
            /// If from all components return true, hook will perform SetActive(false) on this model after this message
            /// </summary>
            bool OnBeforeModelDeactivated(RenderModelHook hook);
        }

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

        [CreatorPriorityAttirbute(10)]
        public class DefaultRenderModelCreator : RenderModelCreator
        {
            private bool m_isModelActivated;
            private EnumArray<VRModuleDeviceModel, GameObject> m_modelObjs = new EnumArray<VRModuleDeviceModel, GameObject>();
            private VRModuleDeviceModel m_activeModel;

            [Obsolete]
            protected GameObject m_model;

            public override bool shouldActive { get { return true; } }

            public override void UpdateRenderModel()
            {
                var deviceState = VRModule.GetDeviceState(hook.GetModelDeviceIndex());

                var lastActiveCustomModelNum = m_activeModel;
                var lastActiveCustomModelObj = m_modelObjs[m_activeModel];
                var lastCustomModelActive = m_isModelActivated;
                var shouldActiveCustomModelNum = deviceState.deviceModel;
                var shouldActiveCustomModelPrefab = VIUSettings.GetDefaultDeviceModel(shouldActiveCustomModelNum);
                var shouldActiveCustomModel = deviceState.isConnected && shouldActiveCustomModelPrefab != null;

                if (lastCustomModelActive)
                {
                    if (!shouldActiveCustomModel || lastActiveCustomModelNum != shouldActiveCustomModelNum)
                    {
                        // deactivate custom override model
                        if (lastActiveCustomModelObj != null && SendBeforeModelDeactivatedMessage(lastActiveCustomModelObj, hook))
                        {
                            lastActiveCustomModelObj.gameObject.SetActive(false);
                        }
                        m_isModelActivated = false;
                    }
                }

                if (shouldActiveCustomModel)
                {
                    var shouldActiveCustomModelObj = m_modelObjs[shouldActiveCustomModelNum];
                    if (shouldActiveCustomModelObj == null)
                    {
                        // instantiate custom override model
                        shouldActiveCustomModelObj = Instantiate(shouldActiveCustomModelPrefab);
                        shouldActiveCustomModelObj.transform.position = Vector3.zero;
                        shouldActiveCustomModelObj.transform.rotation = Quaternion.identity;
                        shouldActiveCustomModelObj.transform.SetParent(hook.transform, false);
                        m_activeModel = shouldActiveCustomModelNum;
                        m_modelObjs[shouldActiveCustomModelNum] = shouldActiveCustomModelObj;
                        m_isModelActivated = false;
                        SendAfterModelCreatedMessage(shouldActiveCustomModelObj, hook);
                    }

                    if (!m_isModelActivated)
                    {
                        // active custom override model
                        if (SendBeforeModelActivatedMessage(shouldActiveCustomModelObj, hook))
                        {
                            shouldActiveCustomModelObj.gameObject.SetActive(true);
                        }
                        m_isModelActivated = true;
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (!m_isModelActivated)
                {
                    var activatedModelObj = m_modelObjs[m_activeModel];
                    // active custom override model
                    if (activatedModelObj != null && SendBeforeModelActivatedMessage(activatedModelObj, hook))
                    {
                        activatedModelObj.gameObject.SetActive(true);
                    }
                    m_isModelActivated = true;
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
            DaydreamController = VRModuleDeviceModel.DaydreamController,
            ViveFocusFinch = VRModuleDeviceModel.ViveFocusFinch,
            OculusGoController = VRModuleDeviceModel.OculusGoController,
            OculusGearVrController = VRModuleDeviceModel.OculusGearVrController,
            WMRControllerLeft = VRModuleDeviceModel.WMRControllerLeft,
            WMRControllerRight = VRModuleDeviceModel.WMRControllerRight,
            ViveCosmosControllerLeft = VRModuleDeviceModel.ViveCosmosControllerLeft,
            ViveCosmosControllerRight = VRModuleDeviceModel.ViveCosmosControllerRight,
            OculusQuestControllerLeft = VRModuleDeviceModel.OculusQuestControllerLeft,
            OculusQuestControllerRight = VRModuleDeviceModel.OculusQuestControllerRight,
            IndexHMD = VRModuleDeviceModel.IndexHMD,
            IndexControllerLeft = VRModuleDeviceModel.IndexControllerLeft,
            IndexControllerRight = VRModuleDeviceModel.IndexControllerRight,
        }

        [SerializeField]
        private Mode m_mode = Mode.ViveRole;
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        private Index m_deviceIndex = Index.Hmd;
        [SerializeField]
        private OverrideModelEnum m_overrideModel = OverrideModelEnum.DontOverride;
        [SerializeField]
        private Shader m_overrideShader = null;
        [SerializeField]
        private Material m_overrideMaterial = null;

        private static readonly Type[] s_creatorTypes;
        private RenderModelCreator[] m_creators;
        private int m_activeCreatorIndex = -1;
        private int m_defaultCreatorIndex = -1;
        private bool m_isQuiting;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
        [Obsolete]
        public Transform origin { get; set; }
        [Obsolete]
        public bool applyTracking { get; set; }

        public OverrideModelEnum overrideModel { get { return m_overrideModel; } set { m_overrideModel = value; } }

        public Shader overrideShader { get { return m_overrideShader; } set { m_overrideShader = value; } }

        public Material overrideMaterial { get { return m_overrideMaterial; } set { m_overrideMaterial = value; } }

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
                s_creatorTypes = new Type[] { typeof(DefaultRenderModelCreator) };
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
            m_viveRole.onRoleChanged += UpdateModel;

            UpdateModel();
        }

        protected virtual void OnDisable()
        {
            VRModule.onActiveModuleChanged -= OnActiveModuleChanged;
            m_viveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
            m_viveRole.onRoleChanged -= UpdateModel;

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

        private bool m_isCustomModelActivated;
        private EnumArray<VRModuleDeviceModel, GameObject> m_customModelObjs = new EnumArray<VRModuleDeviceModel, GameObject>();
        private VRModuleDeviceModel m_activeCustomModel;

        public EnumArray<VRModuleDeviceModel, GameObject>.IReadOnly loadedCuustomModels { get { return m_customModelObjs.ReadOnly; } }

        private static void SendAfterModelCreatedMessage(GameObject rootObj, RenderModelHook hook)
        {
            var iList = ListPool<ICustomModel>.Get(); try
            {
                rootObj.GetComponentsInChildren(true, iList);
                for (int i = 0, imax = iList.Count; i < imax; ++i)
                {
                    iList[i].OnAfterModelCreated(hook);
                }
            }
            finally { ListPool<ICustomModel>.Release(iList); }
        }

        private static bool SendBeforeModelActivatedMessage(GameObject rootObj, RenderModelHook hook)
        {
            var result = true;
            var iList = ListPool<ICustomModel>.Get(); try
            {
                rootObj.GetComponentsInChildren(true, iList);
                for (int i = 0, imax = iList.Count; i < imax; ++i)
                {
                    result &= iList[i].OnBeforeModelActivated(hook);
                }
            }
            finally { ListPool<ICustomModel>.Release(iList); }
            return result;
        }

        private static bool SendBeforeModelDeactivatedMessage(GameObject rootObj, RenderModelHook hook)
        {
            var result = true;
            var iList = ListPool<ICustomModel>.Get(); try
            {
                rootObj.GetComponentsInChildren(true, iList);
                for (int i = 0, imax = iList.Count; i < imax; ++i)
                {
                    result &= iList[i].OnBeforeModelDeactivated(hook);
                }
            }
            finally { ListPool<ICustomModel>.Release(iList); }
            return result;
        }

        private void UpdateModel()
        {
            if (m_isQuiting) { return; }

            var deviceState = VRModule.GetDeviceState(GetModelDeviceIndex());

            var lastActiveCustomModelNum = m_activeCustomModel;
            var lastActiveCustomModelObj = m_customModelObjs[m_activeCustomModel];
            var lastCustomModelActive = m_isCustomModelActivated;
            var shouldActiveCustomModelNum = deviceState.deviceModel;
            var shouldActiveCustomModelPrefab = VIUSettings.GetOverrideDeviceModel(shouldActiveCustomModelNum);
            var shouldActiveCustomModel = deviceState.isConnected && shouldActiveCustomModelPrefab != null;

            var lastCreatorActive = m_activeCreatorIndex >= 0;
            var shouldActiveCreator = !shouldActiveCustomModel;
            var shouldActiveCreatorIndex = -1;
            if (shouldActiveCreator)
            {
                // determin which creator should be activated
                if (m_overrideModel == OverrideModelEnum.DontOverride)
                {
                    for (int i = 0, imax = m_creators.Length; i < imax; ++i)
                    {
                        if (m_creators[i].shouldActive)
                        {
                            shouldActiveCreatorIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    shouldActiveCreatorIndex = m_defaultCreatorIndex;
                }
            }

            if (lastCustomModelActive)
            {
                if (!shouldActiveCustomModel || lastActiveCustomModelNum != shouldActiveCustomModelNum)
                {
                    // deactivate custom override model
                    if (lastActiveCustomModelObj != null && SendBeforeModelDeactivatedMessage(lastActiveCustomModelObj, this))
                    {
                        lastActiveCustomModelObj.gameObject.SetActive(false);
                    }
                    m_isCustomModelActivated = false;
                }
            }

            if (lastCreatorActive)
            {
                if (!shouldActiveCreator || m_activeCreatorIndex != shouldActiveCreatorIndex)
                {
                    // clean up old creator
                    m_creators[m_activeCreatorIndex].CleanUpRenderModel();
                    m_activeCreatorIndex = -1;
                }
            }

            if (shouldActiveCustomModel)
            {
                var shouldActiveCustomModelObj = m_customModelObjs[shouldActiveCustomModelNum];
                if (shouldActiveCustomModelObj == null)
                {
                    // instantiate custom override model
                    shouldActiveCustomModelObj = Instantiate(shouldActiveCustomModelPrefab);
                    shouldActiveCustomModelObj.transform.position = Vector3.zero;
                    shouldActiveCustomModelObj.transform.rotation = Quaternion.identity;
                    shouldActiveCustomModelObj.transform.SetParent(transform, false);
                    m_activeCustomModel = shouldActiveCustomModelNum;
                    m_customModelObjs[shouldActiveCustomModelNum] = shouldActiveCustomModelObj;
                    m_isCustomModelActivated = false;
                    SendAfterModelCreatedMessage(shouldActiveCustomModelObj, this);
                }

                if (!m_isCustomModelActivated)
                {
                    // active custom override model
                    if (SendBeforeModelActivatedMessage(shouldActiveCustomModelObj, this))
                    {
                        shouldActiveCustomModelObj.gameObject.SetActive(true);
                    }
                    m_isCustomModelActivated = true;
                }
            }

            if (shouldActiveCreator)
            {
                m_activeCreatorIndex = shouldActiveCreatorIndex;
                // update active creator
                m_creators[m_activeCreatorIndex].UpdateRenderModel();
            }
        }
    }
}