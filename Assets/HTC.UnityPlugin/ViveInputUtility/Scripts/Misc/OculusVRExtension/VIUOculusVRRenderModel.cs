//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if VIU_OCULUSVR_AVATAR
using Oculus.Avatar;
#endif

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    // Only works in playing mode
    [Obsolete("Use VIUOvrAvatar and VIUOvrAvatarComponent instead")]
    public class VIUOculusVRRenderModel : MonoBehaviour
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

        private const uint LEFT_INDEX = 1;
        private const uint RIGHT_INDEX = 2;

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
            }
        }

        public void ClearModel() { }

        public void SetDeviceIndex(uint index) { }
    }
}