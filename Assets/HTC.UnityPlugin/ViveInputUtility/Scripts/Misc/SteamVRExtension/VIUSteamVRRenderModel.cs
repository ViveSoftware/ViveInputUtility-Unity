//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections.Generic;
using UnityEngine;
#if VIU_STEAMVR
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    // Only works in playing mode
    public class VIUSteamVRRenderModel : MonoBehaviour
    {
        private struct ChildTransforms
        {
            public Transform root;
            public Transform attach;
        }

        // Name of the sub-object which represents the "local" coordinate space for each component.
        public const string LOCAL_TRANSFORM_NAME = "attach";

        public const string MODEL_OVERRIDE_WARNNING = "Model override is really only meant to be used in " +
            "the scene view for lining things up.  Use tracked device " +
            "index instead to ensure the correct model is displayed for all users.";

        [Tooltip(MODEL_OVERRIDE_WARNNING)]
        [SerializeField]
        private string m_modelOverride;

        [Tooltip("Shader to apply to model.")]
        [SerializeField]
        private Shader m_shaderOverride;

        [Tooltip("Update transforms of components at runtime to reflect user action.")]
        [SerializeField]
        private bool m_updateDynamically = true;

        private uint m_deviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private MeshFilter m_meshFilter;
        private MeshRenderer m_meshRenderer;
        private IndexedTable<string, ChildTransforms> m_chilTransforms = new IndexedTable<string, ChildTransforms>();
        private IndexedTable<int, Material> m_materials = new IndexedTable<int, Material>();
        private HashSet<string> m_loadingRenderModels = new HashSet<string>();
        private bool m_isAppQuit;

        private string preferedModelName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_modelOverride)) { return m_modelOverride; }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return string.Empty;
                }
                else
#endif
                {
                    return VRModule.GetCurrentDeviceState(m_deviceIndex).renderModelName;
                }
            }
        }

        private Shader preferedShader { get { return m_shaderOverride == null ? Shader.Find("Standard") : m_shaderOverride; } }

        public bool updateDynamically { get { return m_updateDynamically; } set { m_updateDynamically = value; } }
        public bool isLoadingModel { get { return m_loadingRenderModels.Count > 0; } }
        public string loadedModelName { get; private set; }
        public bool isModelLoaded { get { return !string.IsNullOrEmpty(loadedModelName); } }
        public Shader loadedShader { get; private set; }

        public string modelOverride
        {
            get
            {
                return m_modelOverride;
            }
            set
            {
                m_modelOverride = value;
                LoadPreferedModel();
            }
        }

        public Shader shaderOverride
        {
            get
            {
                return m_shaderOverride;
            }
            set
            {
                m_shaderOverride = value;
                SetPreferedShader();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (!m_isAppQuit && this != null && isActiveAndEnabled)
                    {
                        LoadPreferedModel();
                        SetPreferedShader();
                    }
                };
            }
        }
#endif

        private void Update()
        {
            if (m_updateDynamically)
            {
                UpdateComponents();
            }
        }

        private void OnEnable()
        {
            LoadPreferedModel();
        }

        private void OnDestroy()
        {
            ClearModel();
        }

        private void OnApplicationQuit()
        {
            m_isAppQuit = true;
        }

        public void ClearModel()
        {
            if (!isModelLoaded) { return; }

            if (m_meshRenderer != null) { Destroy(m_meshRenderer); }
            if (m_meshFilter != null) { Destroy(m_meshFilter); }

            for (int i = 0, imax = m_chilTransforms.Count; i < imax; ++i)
            {
                var c = m_chilTransforms.GetValueByIndex(i);
                if (c.root == null) { continue; }
                Destroy(c.root.gameObject);
            }

            m_chilTransforms.Clear();
            m_materials.Clear();
            loadedModelName = string.Empty;
            loadedShader = null;
        }

        private void SetPreferedShader()
        {
            SetShader(preferedShader);
        }

        private void SetShader(Shader newShader)
        {
            if (loadedShader == newShader) { return; }

            loadedShader = newShader;

            if (m_materials == null) { return; }

            for (int i = 0, imax = m_materials.Count; i < imax; ++i)
            {
                var mat = m_materials.GetValueByIndex(i);
                if (mat != null)
                {
                    mat.shader = newShader;
                }
            }
        }

        private void LoadPreferedModel()
        {
            LoadModel(preferedModelName);
        }

        private void LoadModel(string renderModelName)
        {
            //Debug.Log(transform.parent.parent.name + " Try LoadModel " + renderModelName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("LoadModel failed! This function only works in playing mode");
                return;
            }
#endif
            if (string.IsNullOrEmpty(loadedModelName) && string.IsNullOrEmpty(renderModelName)) { return; }

            if (loadedModelName == renderModelName) { return; }

            if (m_loadingRenderModels.Contains(renderModelName)) { return; }

            ClearModel();

            if (!m_isAppQuit && !string.IsNullOrEmpty(renderModelName))
            {
                //Debug.Log(transform.parent.parent.name + " LoadModel " + renderModelName);
                m_loadingRenderModels.Add(renderModelName);
                VIUSteamVRRenderModelLoader.Load(renderModelName, OnLoadModelComplete);
            }
        }

        private void OnLoadModelComplete(string renderModelName)
        {
            m_loadingRenderModels.Remove(renderModelName);

            if (loadedModelName == renderModelName) { return; }
            if (preferedModelName != renderModelName) { return; }
            if (!isActiveAndEnabled) { return; }
            //Debug.Log(transform.parent.parent.name + " OnLoadModelComplete " + renderModelName);
            ClearModel();

            VIUSteamVRRenderModelLoader.RenderModel renderModel;
            if (!VIUSteamVRRenderModelLoader.renderModelsCache.TryGetValue(renderModelName, out renderModel)) { return; }

            if (loadedShader == null) { loadedShader = preferedShader; }

            if (renderModel.childCount == 0)
            {
                VIUSteamVRRenderModelLoader.Model model;
                if (VIUSteamVRRenderModelLoader.modelsCache.TryGetValue(renderModelName, out model))
                {
                    Material material;
                    if (!m_materials.TryGetValue(model.textureID, out material))
                    {
                        material = new Material(loadedShader)
                        {
                            mainTexture = renderModel.textures[model.textureID]
                        };

                        m_materials.Add(model.textureID, material);
                    }

                    m_meshFilter = gameObject.AddComponent<MeshFilter>();
                    m_meshFilter.mesh = model.mesh;
                    m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    m_meshRenderer.sharedMaterial = material;
                }
            }
            else
            {
                for (int i = 0, imax = renderModel.childCount; i < imax; ++i)
                {
                    var childName = renderModel.childCompNames[i];
                    var modelName = renderModel.childModelNames[i];
                    if (string.IsNullOrEmpty(childName) || string.IsNullOrEmpty(modelName)) { continue; }

                    if (!m_chilTransforms.ContainsKey(childName))
                    {
                        var root = new GameObject(childName).transform;

                        root.SetParent(transform, false);
                        root.gameObject.layer = gameObject.layer;

                        VIUSteamVRRenderModelLoader.Model model;
                        if (VIUSteamVRRenderModelLoader.modelsCache.TryGetValue(modelName, out model))
                        {
                            Material material;
                            if (!m_materials.TryGetValue(model.textureID, out material))
                            {
                                material = new Material(loadedShader)
                                {
                                    mainTexture = renderModel.textures[model.textureID]
                                };

                                m_materials.Add(model.textureID, material);
                            }

                            root.gameObject.AddComponent<MeshFilter>().mesh = model.mesh;
                            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                        }

                        // Also create a child 'attach' object for attaching things.
                        var attach = new GameObject(LOCAL_TRANSFORM_NAME).transform;
                        attach.SetParent(root, false);
                        attach.gameObject.layer = gameObject.layer;

                        m_chilTransforms.Add(childName, new ChildTransforms()
                        {
                            root = root,
                            attach = attach,
                        });
                    }
                }
            }

            loadedModelName = renderModelName;
        }

        private void UpdateComponents()
        {
#if VIU_STEAMVR
            if (!isModelLoaded) { return; }

            if (m_chilTransforms.Count == 0) { return; }

            var vrSystem = OpenVR.System;
            if (vrSystem == null) { return; }

            var vrRenderModels = OpenVR.RenderModels;
            if (vrRenderModels == null) { return; }

            for (int i = 0, imax = m_chilTransforms.Count; i < imax; ++i)
            {
                var name = m_chilTransforms.GetKeyByIndex(i);

                RenderModel_ComponentState_t state;
                if (!TryGetComponentState(vrSystem, vrRenderModels, name, out state)) { continue; }

                var comp = m_chilTransforms.GetValueByIndex(i);

                var compPose = new SteamVR_Utils.RigidTransform(state.mTrackingToComponentRenderModel);
                comp.root.localPosition = compPose.pos;
                comp.root.localRotation = compPose.rot;

                var attachPose = new SteamVR_Utils.RigidTransform(state.mTrackingToComponentLocal);
                comp.attach.position = transform.TransformPoint(attachPose.pos);
                comp.attach.rotation = transform.rotation * attachPose.rot;

                var visible = (state.uProperties & (uint)EVRComponentProperty.IsVisible) != 0;
                if (visible != comp.root.gameObject.activeSelf)
                {
                    comp.root.gameObject.SetActive(visible);
                }
            }
#endif
        }

#if VIU_STEAMVR_2_0_0_OR_NEWER
        private bool TryGetComponentState(CVRSystem vrSystem, CVRRenderModels vrRenderModels, string componentName, out RenderModel_ComponentState_t componentState)
        {
            componentState = default(RenderModel_ComponentState_t);
            var modeState = default(RenderModel_ControllerMode_State_t);
            return vrRenderModels.GetComponentStateForDevicePath(loadedModelName, componentName, SteamVRModule.GetInputSourceHandleForDevice(m_deviceIndex), ref modeState, ref componentState);
        }
#elif VIU_STEAMVR
        private static readonly uint s_sizeOfControllerStats = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t));
        private bool TryGetComponentState(CVRSystem vrSystem, CVRRenderModels vrRenderModels, string componentName, out RenderModel_ComponentState_t componentState)
        {
            componentState = default(RenderModel_ComponentState_t);
            var modeState = default(RenderModel_ControllerMode_State_t);
            var controllerState = default(VRControllerState_t);
            if (!vrSystem.GetControllerState(m_deviceIndex, ref controllerState, s_sizeOfControllerStats)) { return false; }
            if (!vrRenderModels.GetComponentState(loadedModelName, componentName, ref controllerState, ref modeState, ref componentState)) { return false; }
            return true;
        }
#endif

        public void SetDeviceIndex(uint index)
        {
            //Debug.Log(transform.parent.parent.name + " SetDeviceIndex " + index);
            m_deviceIndex = index;
            LoadPreferedModel();
        }
    }
}