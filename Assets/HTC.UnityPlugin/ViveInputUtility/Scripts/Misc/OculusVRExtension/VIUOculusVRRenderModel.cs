//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using UnityEngine;

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

        [Tooltip(MODEL_OVERRIDE_WARNNING)]
        [SerializeField]
        private string m_modelOverride;

        [Tooltip("Shader to apply to model.")]
        [SerializeField]
        private Shader m_shaderOverride;

        [Tooltip("Update transforms of components at runtime to reflect user action.")]
        [SerializeField]
        private bool m_updateDynamically = true;

        public bool updateDynamically { get { return m_updateDynamically; } set { m_updateDynamically = value; } }
        public bool isLoadingModel { get { return false; } }
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