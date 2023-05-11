//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

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

        public class DefaultRenderModelCreator : RenderModelCreator
        {
            private struct DefaultModelData
            {
                public bool isLoaded;
                public GameObject model;
            }

            private static EnumArray<VRModuleDeviceModel, DefaultModelData> s_defaultModels = new EnumArray<VRModuleDeviceModel, DefaultModelData>();

            private bool m_isModelActivated;
            private EnumArray<VRModuleDeviceModel, GameObject> m_modelObjs = new EnumArray<VRModuleDeviceModel, GameObject>();
            private VRModuleDeviceModel m_activeModel;

            [Obsolete]
            protected GameObject m_model;

            public override bool shouldActive { get { return true; } }

            protected VRModuleDeviceModel activeDefaultModel { get { return m_activeModel; } }

            protected bool isDefaultModelActive { get { return m_isModelActivated; } }

            public static GameObject GetDefaultDeviceModelPrefab(VRModuleDeviceModel modelNum)
            {
                if (modelNum < s_defaultModels.Min || modelNum > s_defaultModels.Max) { return null; }

                var modelData = s_defaultModels[modelNum];
                if (!modelData.isLoaded)
                {
                    GameObject modelPrefab = null;
                    int modelNameIndex;
                    var info = EnumUtils.GetDisplayInfo(typeof(VRModuleDeviceModel));
                    if (info.value2displayedIndex.TryGetValue((int)modelNum, out modelNameIndex))
                    {
                        modelPrefab = Resources.Load<GameObject>("Models/VIUModel" + info.displayedNames[modelNameIndex]);
                    }
                    s_defaultModels[modelNum] = modelData = new DefaultModelData()
                    {
                        isLoaded = true,
                        model = modelPrefab,
                    };
                }

                return modelData.model;
            }

            public override void UpdateRenderModel()
            {
                UpdateDefaultRenderModel(true);
            }

            protected void UpdateDefaultRenderModel(bool shouldActive)
            {
                var deviceState = VRModule.GetDeviceState(hook.GetModelDeviceIndex());

                var lastModelActivated = m_isModelActivated;
                var lastActivatedModel = m_activeModel;
                var shouldActiveModelNum = hook.overrideModel == OverrideModelEnum.DontOverride ? deviceState.deviceModel : (VRModuleDeviceModel)hook.overrideModel;
                var shouldActiveModelPrefab = shouldActive ? GetDefaultDeviceModelPrefab(shouldActiveModelNum) : null;
                var shouldActiveModel = shouldActive && deviceState.isConnected && shouldActiveModelPrefab != null;

                if (lastModelActivated)
                {
                    if (!shouldActiveModel || lastActivatedModel != shouldActiveModelNum)
                    {
                        // deactivate custom override model
                        var lastActiveModelObj = m_modelObjs[m_activeModel];
                        if (lastActiveModelObj != null && SendBeforeModelDeactivatedMessage(lastActiveModelObj, hook))
                        {
                            lastActiveModelObj.gameObject.SetActive(false);
                        }
                        m_isModelActivated = false;
                    }
                }

                if (shouldActiveModel)
                {
                    if (!lastModelActivated || lastActivatedModel != shouldActiveModelNum)
                    {
                        var shouldActiveModelObj = m_modelObjs[shouldActiveModelNum];
                        if (shouldActiveModelObj == null)
                        {
                            // instantiate custom override model
                            shouldActiveModelObj = Instantiate(shouldActiveModelPrefab);
                            shouldActiveModelObj.transform.position = Vector3.zero;
                            shouldActiveModelObj.transform.rotation = Quaternion.identity;
                            if (hook.m_overrideMaterial != null)
                            {
                                var renderer = shouldActiveModelObj.GetComponentInChildren<Renderer>();
                                if (renderer != null)
                                {
                                    renderer.material = hook.m_overrideMaterial;
                                }
                            }
                            if (hook.m_overrideShader != null)
                            {
                                var renderer = shouldActiveModelObj.GetComponentInChildren<Renderer>();
                                if (renderer != null)
                                {
                                    renderer.material.shader = hook.m_overrideShader;
                                }
                            }
                            shouldActiveModelObj.transform.SetParent(hook.transform, false);
                            m_modelObjs[shouldActiveModelNum] = shouldActiveModelObj;
                            SendAfterModelCreatedMessage(shouldActiveModelObj, hook);
                        }

                        // active custom override model
                        if (SendBeforeModelActivatedMessage(shouldActiveModelObj, hook))
                        {
                            shouldActiveModelObj.gameObject.SetActive(true);
                        }

                        m_activeModel = shouldActiveModelNum;
                        m_isModelActivated = true;
                    }
                }
            }

            public override void CleanUpRenderModel()
            {
                if (m_isModelActivated)
                {
                    // deactivate custom override model
                    var lastActiveModelObj = m_modelObjs[m_activeModel];
                    if (lastActiveModelObj != null && SendBeforeModelDeactivatedMessage(lastActiveModelObj, hook))
                    {
                        lastActiveModelObj.gameObject.SetActive(false);
                    }
                    m_isModelActivated = false;
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
            IndexHMD = VRModuleDeviceModel.IndexHMD, // no model
            IndexControllerLeft = VRModuleDeviceModel.IndexControllerLeft,
            IndexControllerRight = VRModuleDeviceModel.IndexControllerRight,
            MagicLeapHMD = VRModuleDeviceModel.MagicLeapHMD, // no model
            MagicLeapController = VRModuleDeviceModel.MagicLeapController, // no model
            ViveHandTrackingTrackedHandLeft = VRModuleDeviceModel.ViveHandTrackingTrackedHandLeft,
            ViveHandTrackingTrackedHandRight = VRModuleDeviceModel.ViveHandTrackingTrackedHandRight,
            WaveLegacyTrackedHandLeft = VRModuleDeviceModel.WaveLegacyTrackedHandLeft,
            WaveLegacyTrackedHandRight = VRModuleDeviceModel.WaveLegacyTrackedHandRight,
            WaveTrackedHandLeft = VRModuleDeviceModel.WaveTrackedHandLeft,
            WaveTrackedHandRight = VRModuleDeviceModel.WaveTrackedHandRight,
            OculusTrackedHandLeft = VRModuleDeviceModel.OculusTrackedHandLeft,
            OculusTrackedHandRight = VRModuleDeviceModel.OculusTrackedHandRight,
            ViveFocus3ControllerLeft = VRModuleDeviceModel.ViveFocus3ControllerLeft,
            ViveFocus3ControllerRight = VRModuleDeviceModel.ViveFocus3ControllerRight,
            ViveFocusChirp = VRModuleDeviceModel.ViveFocusChirp,
            ViveTracker3 = VRModuleDeviceModel.ViveTracker3,
            ViveFlowPhoneController = VRModuleDeviceModel.ViveFlowPhoneController,
            OculusQuest2ControllerLeft = VRModuleDeviceModel.OculusQuest2ControllerLeft,
            OculusQuest2ControllerRight = VRModuleDeviceModel.OculusQuest2ControllerRight,
            ViveWristTracker = VRModuleDeviceModel.ViveWristTracker,
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
        [SerializeField]
        private VIUSettings.DeviceModelArray m_customModels = new VIUSettings.DeviceModelArray();

        private static Type[] s_creatorTypes;
        private RenderModelCreator[] m_creators;
        private int m_activeCreatorIndex = -1;
        private int m_defaultCreatorIndex = -1;
        private bool m_isQuiting;
        private bool m_updateModelLock;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
        [Obsolete]
        public Transform origin { get; set; }
        [Obsolete]
        public bool applyTracking { get; set; }

        public OverrideModelEnum overrideModel { get { return m_overrideModel; } set { m_overrideModel = value; } }

        public Shader overrideShader { get { return m_overrideShader; } set { m_overrideShader = value; } }

        public Material overrideMaterial { get { return m_overrideMaterial; } set { m_overrideMaterial = value; } }

        public VIUSettings.DeviceModelArray customModels { get { return m_customModels; } }

        private static void FindAllRenderModelCreatorTypes()
        {
            if (s_creatorTypes != null) { return; }

            var defaultCreatorType = typeof(DefaultRenderModelCreator);
            try
            {
                var creatorBaseType = typeof(RenderModelCreator);
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
                        foreach (var type in asm.GetTypes().Where(t => t.IsSubclassOf(creatorBaseType) && !t.IsAbstract && t != defaultCreatorType))
                        {
                            creatorTypes.Add(type);
                        }
                    }
                }

                creatorTypes.Sort((x, y) => GetCreatorPriority(x) - GetCreatorPriority(y));
                creatorTypes.Add(defaultCreatorType);
                s_creatorTypes = creatorTypes.ToArray();
            }
            catch (Exception e)
            {
                s_creatorTypes = new Type[] { defaultCreatorType };
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
            FindAllRenderModelCreatorTypes();

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
            UpdateModel();

            VRModule.onActiveModuleChanged += OnActiveModuleChanged;
            VRModule.onControllerRoleChanged += UpdateModel;
            m_viveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
            m_viveRole.onRoleChanged += UpdateModel;
        }

        protected virtual void OnDisable()
        {
            VRModule.onActiveModuleChanged -= OnActiveModuleChanged;
            VRModule.onControllerRoleChanged -= UpdateModel;
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

        [ContextMenu("Update Model")]
        public void UpdateModel()
        {
            if (m_isQuiting) { return; }

            if (m_updateModelLock)
            {
                Debug.LogWarning("Recursive UpdateModel call is not supported");
                return;
            }

            m_updateModelLock = true;

            var deviceState = VRModule.GetDeviceState(GetModelDeviceIndex());

            var lastActiveCustomModelNum = m_activeCustomModel;
            var lastActiveCustomModelObj = m_customModelObjs[m_activeCustomModel];
            var lastCustomModelActive = m_isCustomModelActivated;
            var shouldActiveCustomModelNum = deviceState.deviceModel;
            var shouldActiveCustomModelPrefab = m_customModels[shouldActiveCustomModelNum];
            if (shouldActiveCustomModelPrefab == null) { shouldActiveCustomModelPrefab = VIUSettings.GetOverrideDeviceModel(shouldActiveCustomModelNum); }
            var shouldActiveCustomModel = enabled && deviceState.isConnected && shouldActiveCustomModelPrefab != null;

            var lastCreatorActive = m_activeCreatorIndex >= 0;
            var shouldActiveCreator = enabled && !shouldActiveCustomModel;
            var shouldActiveCreatorIndex = -1;
            if (shouldActiveCreator)
            {
                // determin which creator should be activated
                shouldActiveCreatorIndex = m_defaultCreatorIndex;
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
                if (!lastCustomModelActive || lastActiveCustomModelNum != shouldActiveCustomModelNum)
                {
                    var shouldActiveCustomModelObj = m_customModelObjs[shouldActiveCustomModelNum];
                    if (shouldActiveCustomModelObj == null)
                    {
                        // instantiate custom override model
                        shouldActiveCustomModelObj = Instantiate(shouldActiveCustomModelPrefab);
                        shouldActiveCustomModelObj.transform.position = Vector3.zero;
                        shouldActiveCustomModelObj.transform.rotation = Quaternion.identity;
                        shouldActiveCustomModelObj.transform.SetParent(transform, false);
                        m_customModelObjs[shouldActiveCustomModelNum] = shouldActiveCustomModelObj;
                        SendAfterModelCreatedMessage(shouldActiveCustomModelObj, this);
                    }

                    // active custom override model
                    if (SendBeforeModelActivatedMessage(shouldActiveCustomModelObj, this))
                    {
                        shouldActiveCustomModelObj.gameObject.SetActive(true);
                    }

                    m_activeCustomModel = shouldActiveCustomModelNum;
                    m_isCustomModelActivated = true;
                }
            }

            if (shouldActiveCreator)
            {
                m_activeCreatorIndex = shouldActiveCreatorIndex;
                // update active creator
                m_creators[m_activeCreatorIndex].UpdateRenderModel();
            }

            m_updateModelLock = false;
        }
    }
}