//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

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

#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
        private IntPtr sdkAvatar = IntPtr.Zero;
        private HashSet<UInt64> assetLoadingIds = new HashSet<UInt64>();
        private Dictionary<string, OvrAvatarComponent> trackedComponents =
            new Dictionary<string, OvrAvatarComponent>();
        private OvrAvatarTouchController ovrController;
        private int renderPartCount = 0;
        private Shader SurfaceShader;
        private Shader SurfaceShaderSelfOccluding;
        private List<OvrAvatarRenderComponent> RenderParts = new List<OvrAvatarRenderComponent>();

        private ovrAvatarHandInputState inputStateLeft;
        private ovrAvatarHandInputState inputStateRight;
        private bool firstSkinnedUpdate = true;
        private bool assetsFinishedLoading = false;
        private bool isMaterialInitilized = false;
        private bool isMeshInitilized = false;
        private ovrAvatarControllerType m_controllerType = ovrAvatarControllerType.Quest;
#endif

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
#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
                if (sdkAvatar == IntPtr.Zero)
                {
                    return;
                }

                if (m_deviceIndex == LEFT_INDEX)
                {
                    inputStateLeft.transform.position = transform.position;
                    inputStateLeft.transform.orientation = transform.rotation;
                    inputStateLeft.transform.scale = transform.localScale;

                    inputStateLeft.buttonMask = 0;
                    inputStateLeft.touchMask = ovrAvatarTouch.Pointing | ovrAvatarTouch.ThumbUp;

                    if (ViveInput.GetPress(HandRole.LeftHand, ControllerButton.AKey))
                    {
                        inputStateLeft.buttonMask |= ovrAvatarButton.One;
                    }

                    if (ViveInput.GetPress(HandRole.LeftHand, ControllerButton.BKey))
                    {
                        inputStateLeft.buttonMask |= ovrAvatarButton.Two;
                    }

                    if (ViveInput.GetPress(HandRole.LeftHand, ControllerButton.System))
                    {
                        inputStateLeft.buttonMask |= ovrAvatarButton.Three;
                    }

                    if (ViveInput.GetPress(HandRole.LeftHand, ControllerButton.Pad))
                    {
                        inputStateLeft.buttonMask |= ovrAvatarButton.Joystick;
                    }

                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.AKeyTouch))
                    {
                        inputStateLeft.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateLeft.touchMask |= ovrAvatarTouch.One;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.BkeyTouch))
                    {
                        inputStateLeft.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateLeft.touchMask |= ovrAvatarTouch.Two;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.PadTouch))
                    {
                        inputStateLeft.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateLeft.touchMask |= ovrAvatarTouch.Joystick;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.TriggerTouch))
                    {
                        inputStateLeft.touchMask &= ~ovrAvatarTouch.Pointing;
                        inputStateLeft.touchMask |= ovrAvatarTouch.Index;
                    }

                    inputStateLeft.joystickX = ViveInput.GetAxis(HandRole.LeftHand, ControllerAxis.JoystickX);
                    inputStateLeft.joystickY = ViveInput.GetAxis(HandRole.LeftHand, ControllerAxis.JoystickY);
                    inputStateLeft.indexTrigger = ViveInput.GetAxis(HandRole.LeftHand, ControllerAxis.Trigger);
                    inputStateLeft.handTrigger = ViveInput.GetAxis(HandRole.LeftHand, ControllerAxis.CapSenseGrip);
                    inputStateLeft.isActive = true;
                }
                else if (m_deviceIndex == RIGHT_INDEX)
                {
                    inputStateRight.transform.position = transform.position;
                    inputStateRight.transform.orientation = transform.rotation;
                    inputStateRight.transform.scale = transform.localScale;

                    inputStateRight.buttonMask = 0;
                    inputStateRight.touchMask = ovrAvatarTouch.Pointing | ovrAvatarTouch.ThumbUp;

                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.AKey))
                    {
                        inputStateRight.buttonMask |= ovrAvatarButton.One;
                    }

                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.BKey))
                    {
                        inputStateRight.buttonMask |= ovrAvatarButton.Two;
                    }

                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.Pad))
                    {
                        inputStateRight.buttonMask |= ovrAvatarButton.Joystick;
                    }

                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.AKeyTouch))
                    {
                        inputStateRight.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateRight.touchMask |= ovrAvatarTouch.One;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.BkeyTouch))
                    {
                        inputStateRight.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateRight.touchMask |= ovrAvatarTouch.Two;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.PadTouch))
                    {
                        inputStateRight.touchMask &= ~ovrAvatarTouch.ThumbUp;
                        inputStateRight.touchMask |= ovrAvatarTouch.Joystick;
                    }
                    if (ViveInput.GetPress(HandRole.RightHand, ControllerButton.TriggerTouch))
                    {
                        inputStateRight.touchMask &= ~ovrAvatarTouch.Pointing;
                        inputStateRight.touchMask |= ovrAvatarTouch.Index;
                    }

                    inputStateRight.joystickX = ViveInput.GetAxis(HandRole.RightHand, ControllerAxis.JoystickX);
                    inputStateRight.joystickY = ViveInput.GetAxis(HandRole.RightHand, ControllerAxis.JoystickY);
                    inputStateRight.indexTrigger = ViveInput.GetAxis(HandRole.RightHand, ControllerAxis.Trigger);
                    inputStateRight.handTrigger = ViveInput.GetAxis(HandRole.RightHand, ControllerAxis.CapSenseGrip);
                    inputStateRight.isActive = true;
                }

                CAPI.ovrAvatarPose_UpdateHandsWithType(sdkAvatar, inputStateLeft, inputStateRight, m_controllerType);
                CAPI.ovrAvatarPose_Finalize(sdkAvatar, Time.deltaTime);
#endif

                UpdateComponents();

#if VIU_OCULUSVR_1_37_0_OR_NEWER && VIU_OCULUSVR_AVATAR
                if (m_deviceIndex == LEFT_INDEX)
                {
                    ovrAvatarControllerComponent component = new ovrAvatarControllerComponent();
                    if (CAPI.ovrAvatarPose_GetLeftControllerComponent(sdkAvatar, ref component))
                    {
                        UpdateAvatarComponent(component.renderComponent);
                    }
                }
                else if (m_deviceIndex == RIGHT_INDEX)
                {
                    ovrAvatarControllerComponent component = new ovrAvatarControllerComponent();
                    if (CAPI.ovrAvatarPose_GetRightControllerComponent(sdkAvatar, ref component))
                    {
                        UpdateAvatarComponent(component.renderComponent);
                    }
                }
#endif
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
#if VIU_OCULUSVR_1_37_0_OR_NEWER && VIU_OCULUSVR_AVATAR
                OvrAvatarSDKManager.AvatarSpecRequestParams avatarSpecRequest = new OvrAvatarSDKManager.AvatarSpecRequestParams(
                    0,
                    this.AvatarSpecificationCallback,
                    false,
                    ovrAvatarAssetLevelOfDetail.Highest,
                    false,
                    ovrAvatarLookAndFeelVersion.Two,
                    ovrAvatarLookAndFeelVersion.One,
                    false);

                OvrAvatarSDKManager.Instance.RequestAvatarSpecification(avatarSpecRequest);
#elif VIU_OCULUSVR_1_36_0_OR_NEWER && VIU_OCULUSVR_AVATAR
                OvrAvatarSDKManager.Instance.RequestAvatarSpecification(
                    0,
                    this.AvatarSpecificationCallback,
                    false,
                    ovrAvatarAssetLevelOfDetail.Highest,
                    false,
                    ovrAvatarLookAndFeelVersion.Two,
                    ovrAvatarLookAndFeelVersion.One,
                    false);
#elif VIU_OCULUSVR_1_32_0_OR_NEWER && VIU_OCULUSVR_AVATAR
                    OvrAvatarSDKManager.Instance.RequestAvatarSpecification(
                    0,
                    this.AvatarSpecificationCallback,
                    false,
                    ovrAvatarAssetLevelOfDetail.Highest,
                    false);
#endif
            }
        }


        private void AvatarSpecificationCallback(IntPtr avatarSpecification)
        {
#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
            sdkAvatar = CAPI.ovrAvatar_Create(avatarSpecification, ovrAvatarCapabilities.All);
            CAPI.ovrAvatar_SetLeftControllerVisibility(sdkAvatar, true);
            CAPI.ovrAvatar_SetRightControllerVisibility(sdkAvatar, true);

            //Fetch all the assets that this avatar uses.
            UInt32 assetCount = CAPI.ovrAvatar_GetReferencedAssetCount(sdkAvatar);
            for (UInt32 i = 0; i < assetCount; ++i)
            {
                UInt64 id = CAPI.ovrAvatar_GetReferencedAsset(sdkAvatar, i);
                if (OvrAvatarSDKManager.Instance.GetAsset(id) == null)
                {
                    OvrAvatarSDKManager.Instance.BeginLoadingAsset(
                        id,
                        ovrAvatarAssetLevelOfDetail.Highest,
                        AssetLoadedCallback);

                    assetLoadingIds.Add(id);
                }
            }
#endif
        }

#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
        private void AssetLoadedCallback(OvrAvatarAsset asset)
        {
            assetLoadingIds.Remove(asset.assetID);
        }
#endif

        private void OnLoadModelComplete(string renderModelName)
        {
            m_loadingRenderModels.Remove(renderModelName);

            if (loadedModelName == renderModelName) { return; }
            if (preferedModelName != renderModelName) { return; }
            if (!isActiveAndEnabled) { return; }
            //Debug.Log(transform.parent.parent.name + " OnLoadModelComplete " + renderModelName);
            ClearModel();

            loadedModelName = renderModelName;
        }

        private void UpdateComponents()
        {
#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
            if (assetLoadingIds.Count == 0)
            {
                if (!assetsFinishedLoading)
                {
                    UpdateSDKAvatarUnityState();
                    assetsFinishedLoading = true;
                }
            }
#endif
        }

#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER || VIU_OCULUSVR_1_37_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
        private void UpdateSDKAvatarUnityState()
        {
#if VIU_OCULUSVR_1_37_0_OR_NEWER && VIU_OCULUSVR_AVATAR
            ovrAvatarControllerComponent controllerComponent = new ovrAvatarControllerComponent();
            ovrAvatarComponent dummyComponent = new ovrAvatarComponent();
            OvrAvatarTouchController controller = null;

            if (m_deviceIndex == LEFT_INDEX)
            {
                if (CAPI.ovrAvatarPose_GetLeftControllerComponent(sdkAvatar, ref controllerComponent))
                {
                    CAPI.ovrAvatarComponent_Get(controllerComponent.renderComponent, true, ref dummyComponent);
                    AddAvatarComponent(ref controller, dummyComponent);
                    controller.isLeftHand = true;
                }
            }
            else if (m_deviceIndex == RIGHT_INDEX)
            {
                if (CAPI.ovrAvatarPose_GetRightControllerComponent(sdkAvatar, ref controllerComponent))
                {
                    CAPI.ovrAvatarComponent_Get(controllerComponent.renderComponent, true, ref dummyComponent);
                    AddAvatarComponent(ref controller, dummyComponent);
                    controller.isLeftHand = false;
                }
            }
#elif (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
            //Iterate through all the render components
            UInt32 componentCount = CAPI.ovrAvatarComponent_Count(sdkAvatar);

            for (UInt32 i = 0; i < componentCount; i++)
            {
                IntPtr ptr = CAPI.ovrAvatarComponent_Get_Native(sdkAvatar, i);

                ovrAvatarComponent component = (ovrAvatarComponent)Marshal.PtrToStructure(ptr, typeof(ovrAvatarComponent));

                if (!trackedComponents.ContainsKey(component.name))
                {
                    GameObject componentObject = null;
                    Type specificType = null;


                    if (specificType == null && (ovrAvatarCapabilities.All & ovrAvatarCapabilities.Hands) != 0)
                    {
                        if (m_deviceIndex == LEFT_INDEX)
                        {
                            ovrAvatarControllerComponent? controllerComponent = CAPI.ovrAvatarPose_GetLeftControllerComponent(sdkAvatar);
                            if (specificType == null && controllerComponent.HasValue && ptr == controllerComponent.Value.renderComponent)
                            {
                                specificType = typeof(OvrAvatarTouchController);
                                if (ovrController != null)
                                {
                                    componentObject = ovrController.gameObject;
                                }
                            }
                        }
                        else if (m_deviceIndex == RIGHT_INDEX)
                        {
                            ovrAvatarControllerComponent? controllerComponent = CAPI.ovrAvatarPose_GetRightControllerComponent(sdkAvatar);
                            if (specificType == null && controllerComponent.HasValue && ptr == controllerComponent.Value.renderComponent)
                            {
                                specificType = typeof(OvrAvatarTouchController);
                                if (ovrController != null)
                                {
                                    componentObject = ovrController.gameObject;
                                }
                            }
                        }
                    }

                    if (componentObject != null)
                    {
                        AddAvatarComponent(componentObject, component);
                    }
                }

                UpdateAvatarComponent(component);
            }
#endif
        }

#if VIU_OCULUSVR_1_37_0_OR_NEWER && VIU_OCULUSVR_AVATAR
        private void AddAvatarComponent<T>(ref T root, ovrAvatarComponent nativeComponent) where T : OvrAvatarComponent
        {
            GameObject componentObject = new GameObject();
            componentObject.name = nativeComponent.name;
            componentObject.transform.SetParent(transform);
            root = componentObject.AddComponent<T>();
            AddRenderParts(root, nativeComponent, componentObject.transform);
        }
#elif VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER
        private void AddAvatarComponent(GameObject componentObject, ovrAvatarComponent component)
        {
            OvrAvatarComponent ovrComponent = componentObject.AddComponent<OvrAvatarComponent>();
            trackedComponents.Add(component.name, ovrComponent);

            AddRenderParts(ovrComponent, component, componentObject.transform);
        }
#endif

        private void AddRenderParts(
            OvrAvatarComponent ovrComponent,
            ovrAvatarComponent component,
            Transform parent)
        {
            for (UInt32 renderPartIndex = 0; renderPartIndex < component.renderPartCount; renderPartIndex++)
            {
                GameObject renderPartObject = new GameObject();
                renderPartObject.name = GetRenderPartName(component, renderPartIndex);

                renderPartObject.transform.SetParent(parent);
                renderPartObject.transform.localPosition = Vector2.zero;
                renderPartObject.transform.localRotation = Quaternion.identity;

                IntPtr renderPart = GetRenderPart(component, renderPartIndex);
                ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                OvrAvatarRenderComponent ovrRenderPart = null;
                switch (type)
                {
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                        ovrRenderPart = AddSkinnedMeshRenderPBSComponent(renderPartObject, CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart));
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                        ovrRenderPart = AddSkinnedMeshRenderPBSV2Component(renderPartObject, CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart));
                        break;
                    default:
                        break;// throw new NotImplementedException(string.Format("Unsupported render part type: {0}", type.ToString()));
                }

                if (ovrRenderPart != null)
                {
                    ovrComponent.RenderParts.Add(ovrRenderPart);
                    RenderParts.Add(ovrRenderPart);
                }
            }
        }

        private OvrAvatarSkinnedMeshRenderPBSComponent AddSkinnedMeshRenderPBSComponent(GameObject gameObject, ovrAvatarRenderPart_SkinnedMeshRenderPBS skinnedMeshRenderPBS)
        {
            OvrAvatarSkinnedMeshRenderPBSComponent skinnedMeshRenderer = gameObject.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
            OvrAvatarAssetMesh meshAsset = (OvrAvatarAssetMesh)OvrAvatarSDKManager.Instance.GetAsset(skinnedMeshRenderPBS.meshAssetID);
            SkinnedMeshRenderer renderer = meshAsset.CreateSkinnedMeshRendererOnObject(gameObject);

#if UNITY_ANDROID
            renderer.quality = SkinQuality.Bone2;
#else
            renderer.quality = SkinQuality.Bone4;
#endif
            renderer.updateWhenOffscreen = true;
            skinnedMeshRenderer.mesh = renderer;
            transform.GetChild(0).localPosition = Vector2.zero;
            transform.GetChild(0).localRotation = Quaternion.identity;
            transform.GetChild(0).GetComponentInChildren<OvrAvatarSkinnedMeshRenderPBSComponent>().gameObject.SetActive(false);
            var shader = Shader.Find("OvrAvatar/AvatarSurfaceShaderPBS");
            renderer.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
            SetMaterialOpaque(renderer.sharedMaterial);
            skinnedMeshRenderer.bones = renderer.bones;

            return skinnedMeshRenderer;
        }

        private OvrAvatarSkinnedMeshPBSV2RenderComponent AddSkinnedMeshRenderPBSV2Component(GameObject gameObject, ovrAvatarRenderPart_SkinnedMeshRenderPBS_V2 skinnedMeshRenderPBSV2)
        {
            OvrAvatarSkinnedMeshPBSV2RenderComponent skinnedMeshRenderer = gameObject.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
            OvrAvatarAssetMesh meshAsset = (OvrAvatarAssetMesh)OvrAvatarSDKManager.Instance.GetAsset(skinnedMeshRenderPBSV2.meshAssetID);
            SkinnedMeshRenderer renderer = meshAsset.CreateSkinnedMeshRendererOnObject(gameObject);

#if UNITY_ANDROID
            renderer.quality = SkinQuality.Bone2;
#else
            renderer.quality = SkinQuality.Bone4;
#endif
            renderer.updateWhenOffscreen = true;
            skinnedMeshRenderer.mesh = renderer;
            transform.GetChild(0).localPosition = Vector2.zero;
            transform.GetChild(0).localRotation = Quaternion.identity;
            transform.GetChild(0).GetComponentInChildren<OvrAvatarSkinnedMeshPBSV2RenderComponent>().gameObject.SetActive(false);
            var shader = Shader.Find("OvrAvatar/AvatarPBRV2Simple");
            renderer.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
            SetMaterialOpaque(renderer.sharedMaterial);
            skinnedMeshRenderer.bones = renderer.bones;

            return skinnedMeshRenderer;
        }

        private Material CreateAvatarMaterial(string name, Shader shader)
        {
            if (shader == null)
            {
                throw new Exception("No shader provided for avatar material.");
            }
            Material mat = new Material(shader);
            mat.name = name;
            return mat;
        }

        private void SetMaterialOpaque(Material mat)
        {
            // Initialize shader to use geometry render queue with no blending
            mat.SetOverrideTag("Queue", "Geometry");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        private static string GetRenderPartName(ovrAvatarComponent component, uint renderPartIndex)
        {
            return component.name + "_renderPart_" + (int)renderPartIndex;
        }

        static public IntPtr GetRenderPart(ovrAvatarComponent component, UInt32 renderPartIndex)
        {
            long offset = Marshal.SizeOf(typeof(IntPtr)) * renderPartIndex;
            IntPtr marshalPtr = new IntPtr(component.renderParts.ToInt64() + offset);
            return (IntPtr)Marshal.PtrToStructure(marshalPtr, typeof(IntPtr));
        }

#if VIU_OCULUSVR_1_37_0_OR_NEWER
        private void UpdateAvatarComponent(IntPtr nativeComponent)
        {
            ovrAvatarComponent nativeAvatarComponent = new ovrAvatarComponent();
            CAPI.ovrAvatarComponent_Get(nativeComponent, false, ref nativeAvatarComponent);
            ConvertTransform(nativeAvatarComponent.transform, transform);
            for (UInt32 renderPartIndex = 0; renderPartIndex < nativeAvatarComponent.renderPartCount; renderPartIndex++)
            {
                if (RenderParts.Count <= renderPartIndex)
                {
                    break;
                }

                OvrAvatarRenderComponent renderComponent = RenderParts[(int)renderPartIndex];
                IntPtr renderPart = OvrAvatar.GetRenderPart(nativeAvatarComponent, renderPartIndex);
                ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                ovrAvatarTransform localTransform;
                ovrAvatarVisibilityFlags visibilityMask;
                var mesh = renderComponent.mesh;
                var bones = renderComponent.bones;
                switch (type)
                {
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                        visibilityMask = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetVisibilityMask(renderPart);
                        localTransform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetTransform(renderPart);
                        if ((visibilityMask & ovrAvatarVisibilityFlags.FirstPerson) != 0)
                        {
                            renderComponent.gameObject.SetActive(true);
                            mesh.enabled = true;
                        }

                        UpdateSkinnedMesh(localTransform, renderPart, bones);

                        UInt64 albedoTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetAlbedoTextureAssetID(renderPart);
                        UInt64 surfaceTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetSurfaceTextureAssetID(renderPart);
                        mesh.sharedMaterial.SetTexture("_Albedo", OvrAvatarComponent.GetLoadedTexture(albedoTextureID));
                        mesh.sharedMaterial.SetTexture("_Surface", OvrAvatarComponent.GetLoadedTexture(surfaceTextureID));
                        break;
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                        visibilityMask = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetVisibilityMask(renderPart);
                        localTransform = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetTransform(renderPart);
                        if ((visibilityMask & ovrAvatarVisibilityFlags.FirstPerson) != 0)
                        {
                            renderComponent.gameObject.SetActive(true);
                            mesh.enabled = true;
                        }

                        UpdateSkinnedMesh(localTransform, renderPart, bones);

                        ovrAvatarPBSMaterialState materialState =
                            CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetPBSMaterialState(renderPart);
                        Texture2D diffuseTexture = OvrAvatarComponent.GetLoadedTexture(materialState.albedoTextureID);
                        Texture2D normalTexture = OvrAvatarComponent.GetLoadedTexture(materialState.normalTextureID);
                        Texture2D metallicTexture = OvrAvatarComponent.GetLoadedTexture(materialState.metallicnessTextureID);
                        mesh.materials[0].SetTexture("_MainTex", diffuseTexture);
                        mesh.materials[0].SetTexture("_NormalMap", normalTexture);
                        mesh.materials[0].SetTexture("_RoughnessMap", metallicTexture);
                        break;
                    default:
                        break;
                }
            }
        }
#elif VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER
        private void UpdateAvatarComponent(ovrAvatarComponent component)
        {
            for (UInt32 renderPartIndex = 0; renderPartIndex < component.renderPartCount; renderPartIndex++)
            {
                if (RenderParts.Count <= renderPartIndex)
                {
                    break;
                }

                OvrAvatarRenderComponent renderComponent = RenderParts[(int)renderPartIndex];
                IntPtr renderPart = OvrAvatar.GetRenderPart(component, renderPartIndex);
                ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                switch (type)
                {
                    case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                        var mat = renderComponent.mesh.sharedMaterial;
                        var bones = renderComponent.bones;
                        ovrAvatarVisibilityFlags visibilityMask = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetVisibilityMask(renderPart);
                        ovrAvatarTransform localTransform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetTransform(renderPart);
                        UpdateSkinnedMesh(localTransform, renderPart, bones);

                        UInt64 albedoTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetAlbedoTextureAssetID(renderPart);
                        UInt64 surfaceTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetSurfaceTextureAssetID(renderPart);
                        mat.SetTexture("_Albedo", OvrAvatarComponent.GetLoadedTexture(albedoTextureID));
                        mat.SetTexture("_Surface", OvrAvatarComponent.GetLoadedTexture(surfaceTextureID));
                        break;
                    default:
                        break;
                }
            }
        }
#endif

        private void UpdateSkinnedMesh(ovrAvatarTransform localTransform, IntPtr renderPart, Transform[] bones)
        {
            ConvertTransform(localTransform, transform);
            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
            UInt64 dirtyJoints;
            switch (type)
            {
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    dirtyJoints = CAPI.ovrAvatarSkinnedMeshRender_GetDirtyJoints(renderPart);
                    break;
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetDirtyJoints(renderPart);
                    break;
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    dirtyJoints = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetDirtyJoints(renderPart);
                    break;
                default:
                    throw new Exception("Unhandled render part type: " + type);
            }

            for (UInt32 i = 0; i < 64; i++)
            {
                UInt64 dirtyMask = (ulong)1 << (int)i;
                // We need to make sure that we fully update the initial position of
                // Skinned mesh renderers, then, thereafter, we can only update dirty handJoints
                if ((firstSkinnedUpdate && i < bones.Length) ||
                    (dirtyMask & dirtyJoints) != 0)
                {
                    //This joint is dirty and needs to be updated
                    Transform targetBone = bones[i];
                    ovrAvatarTransform transform;
                    switch (type)
                    {
                        case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                            transform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetJointTransform(renderPart, i);
                            break;
                        case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                            transform = CAPI.ovrAvatarSkinnedMeshRenderPBSV2_GetJointTransform(renderPart, i);
                            break;
                        default:
                            throw new Exception("Unhandled render part type: " + type);
                    }

                    ConvertTransform(transform, targetBone);
                }
            }
            firstSkinnedUpdate = false;
        }

        private void ConvertTransform(ovrAvatarTransform transform, Transform target)
        {
            Vector3 position = transform.position;
            position.z = -position.z;
            Quaternion orientation = transform.orientation;
            orientation.x = -orientation.x;
            orientation.y = -orientation.y;
            target.localPosition = position;
            target.localRotation = orientation;
            target.localScale = transform.scale;
        }
#endif

        public void SetDeviceIndex(uint index)
        {
            //Debug.Log(transform.parent.parent.name + " SetDeviceIndex " + index);
            m_deviceIndex = index;
#if (VIU_OCULUSVR_1_32_0_OR_NEWER || VIU_OCULUSVR_1_36_0_OR_NEWER) && VIU_OCULUSVR_AVATAR
            ovrController = this.GetComponent<OvrAvatarTouchController>();
#endif
#if VIU_OCULUSVR && VIU_OCULUSVR_AVATAR
            var headsetType = OVRPlugin.GetSystemHeadsetType();
            switch (headsetType)
            {
#if !VIU_OCULUSVR_19_0_OR_NEWER
                case OVRPlugin.SystemHeadset.GearVR_R320:
                case OVRPlugin.SystemHeadset.GearVR_R321:
                case OVRPlugin.SystemHeadset.GearVR_R322:
                case OVRPlugin.SystemHeadset.GearVR_R323:
                case OVRPlugin.SystemHeadset.GearVR_R324:
                case OVRPlugin.SystemHeadset.GearVR_R325:
                    m_controllerType = ovrAvatarControllerType.Malibu;
                    break;
                case OVRPlugin.SystemHeadset.Oculus_Go:
                    m_controllerType = ovrAvatarControllerType.Go;
                    break;
#endif
#if VIU_OCULUSVR_16_0_OR_NEWER
                case OVRPlugin.SystemHeadset.Oculus_Link_Quest:
#endif
                case OVRPlugin.SystemHeadset.Oculus_Quest:
#if VIU_OCULUSVR_1_37_0_OR_NEWER
                case OVRPlugin.SystemHeadset.Rift_S:
                    m_controllerType = ovrAvatarControllerType.Quest;
                    break;
#endif
                case OVRPlugin.SystemHeadset.Rift_DK1:
                case OVRPlugin.SystemHeadset.Rift_DK2:
                case OVRPlugin.SystemHeadset.Rift_CV1:
                default:
                    m_controllerType = ovrAvatarControllerType.Touch;
                    break;
            }
#endif
            LoadPreferedModel();
        }
    }
}