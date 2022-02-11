using Oculus.Avatar;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    public class VIUOvrAvatar : MonoBehaviour
    {
#if VIU_OCULUSVR_1_32_0_OR_NEWER
        public const bool SUPPORTED = true;
#if UNITY_ANDROID && !UNITY_EDITOR
        public const bool USE_MOBILE_TEXTURE_FORMAT = true;
#else
        public const bool USE_MOBILE_TEXTURE_FORMAT = false;
#endif

        private HashSet<ulong> loadingIds = new HashSet<ulong>();

        [SerializeField]
#if UNITY_ANDROID
        private ovrAvatarAssetLevelOfDetail levelOfDetail = ovrAvatarAssetLevelOfDetail.Medium;
#else
        private ovrAvatarAssetLevelOfDetail levelOfDetail = ovrAvatarAssetLevelOfDetail.Highest;
#endif

        [SerializeField]
#if UNITY_EDITOR && UNITY_ANDROID
        private bool forceMobileTextureFormat = true;
#else
        private bool forceMobileTextureFormat = false;
#endif
        [SerializeField]
        private bool showHand = true;
        private bool isShowingHand = true;

        [SerializeField]
        private bool showController = true;
        private bool isShowingCtrl = true;

        // if true, manual update OVRInput if OVRManager not found
        [SerializeField]
        private bool manualUpdateOVRInput = true;
        private static VIUOvrAvatar updateOvrInputUpdateInstance = null;

        public bool isAvatarReady { get; private set; }
        public IntPtr sdkAvatar { get; private set; }
        public OvrAvatar ovrAvatar { get; private set; }
        public OvrAvatarLocalDriver ovrAvatarDriver { get; private set; }
        public OvrAvatarMaterialManager ovrMaterialManager { get; private set; }

        public bool ShowHand
        {
            get { return showHand; }
            set { showHand = value; }
        }

        public bool ShowController
        {
            get { return showController; }
            set { showController = value; }
        }

        public bool CombineMeshes
        {
            // not supported, VIU only render controller & hand
            get { return false; }
        }

        public ovrAvatarAssetLevelOfDetail LevelOfDetail
        {
            get { return levelOfDetail; }
            set { levelOfDetail = value; }
        }

        public bool ForceMobileTextureFormat
        {
            get { return forceMobileTextureFormat; }
            set { forceMobileTextureFormat = value; }
        }

        public bool ShouldManuallyUpdateOVRInput
        {
            get
            {
                return manualUpdateOVRInput && (OVRManager.instance == null || OVRManager.instance.isActiveAndEnabled);
            }
        }

        private void Start()
        {
            GetReady();
        }

        private void Update()
        {
            if (sdkAvatar == IntPtr.Zero) { return; }

            if (isShowingCtrl != showController)
            {
                CAPI.ovrAvatar_SetLeftControllerVisibility(sdkAvatar, showController);
                CAPI.ovrAvatar_SetRightControllerVisibility(sdkAvatar, showController);
                isShowingCtrl = showController;
            }

            if (isShowingHand != showHand)
            {
                CAPI.ovrAvatar_SetLeftHandVisibility(sdkAvatar, showHand);
                CAPI.ovrAvatar_SetRightHandVisibility(sdkAvatar, showHand);
                isShowingHand = showHand;
            }

            if (ShouldManuallyUpdateOVRInput)
            {
                if (updateOvrInputUpdateInstance == this)
                {
                    OVRInput.Update();
                }
                else if (updateOvrInputUpdateInstance == null)
                {
                    updateOvrInputUpdateInstance = this;
                    OVRInput.Update();
                }
            }
            else
            {
                if (updateOvrInputUpdateInstance == this)
                {
                    updateOvrInputUpdateInstance = null;
                }
            }

            if (ovrAvatarDriver != null)
            {
                ovrAvatarDriver.UpdateTransforms(sdkAvatar);
                CAPI.ovrAvatarPose_Finalize(sdkAvatar, Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (updateOvrInputUpdateInstance == this)
            {
                updateOvrInputUpdateInstance = null;
            }
        }

        private void FixedUpdate()
        {
            if (ShouldManuallyUpdateOVRInput && updateOvrInputUpdateInstance == this)
            {
                OVRInput.FixedUpdate();
            }
        }

        private void OnDestroy()
        {
            if (sdkAvatar != IntPtr.Zero)
            {
                CAPI.ovrAvatar_Destroy(sdkAvatar);
                sdkAvatar = IntPtr.Zero;
            }
        }

        public void GetReady()
        {
            if (isAvatarReady) { return; }

            gameObject.SetActive(false);
            ovrAvatarDriver = gameObject.AddComponent<OvrAvatarLocalDriver>();
            ovrMaterialManager = gameObject.AddComponent<OvrAvatarMaterialManager>();
            ovrAvatar = gameObject.AddComponent<OvrAvatar>();
            ovrAvatar.enabled = false;

            ovrAvatar.ShowFirstPerson = true;
            ovrAvatar.ShowThirdPerson = false;

            gameObject.SetActive(true);

#if VIU_OCULUSVR_20_0_OR_NEWER
            ovrAvatar.Monochrome_SurfaceShader = Shader.Find("OvrAvatar/AvatarSurfaceShader");
            ovrAvatar.Monochrome_SurfaceShader_SelfOccluding = Shader.Find("OvrAvatar/AvatarSurfaceShaderSelfOccluding");
            ovrAvatar.Monochrome_SurfaceShader_PBS = Shader.Find("OvrAvatar/AvatarSurfaceShaderPBS");
            ovrAvatar.Skinshaded_SurfaceShader_SingleComponent = Shader.Find("OvrAvatar/Avatar_PC_SingleComponent");
            ovrAvatar.Skinshaded_VertFrag_SingleComponent = Shader.Find("OvrAvatar/Avatar_Mobile_SingleComponent");
            ovrAvatar.Skinshaded_VertFrag_CombinedMesh = Shader.Find("OvrAvatar/Avatar_Mobile_CombinedMesh");
            ovrAvatar.Skinshaded_Expressive_SurfaceShader_SingleComponent = Shader.Find("OvrAvatar/Avatar_PC_SingleComponentExpressive");
            ovrAvatar.Skinshaded_Expressive_VertFrag_SingleComponent = Shader.Find("OvrAvatar/Avatar_Mobile_SingleComponentExpressive");
            ovrAvatar.Skinshaded_Expressive_VertFrag_CombinedMesh = Shader.Find("OvrAvatar/Avatar_Mobile_CombinedMeshExpressive");
            ovrAvatar.Loader_VertFrag_CombinedMesh = Shader.Find("OvrAvatar/Avatar_Mobile_Loader");
            ovrAvatar.EyeLens = Shader.Find("OvrAvatar/Avatar_EyeLens");
            ovrAvatar.ControllerShader = Shader.Find("OvrAvatar/AvatarPBRV2Simple");

            ovrAvatar.EnableExpressive = false;

            var avatarSpecRequest = new OvrAvatarSDKManager.AvatarSpecRequestParams(
                0ul,
                this.AvatarSpecificationCallback,
                CombineMeshes,
                LevelOfDetail,
                USE_MOBILE_TEXTURE_FORMAT,
                ovrAvatarLookAndFeelVersion.Two,
                ovrAvatarLookAndFeelVersion.One,
                false);

            OvrAvatarSDKManager.Instance.RequestAvatarSpecification(avatarSpecRequest);
            OvrAvatarSDKManager.Instance.AddLoadingAvatar(ovrAvatar.GetInstanceID());
#else
            ovrAvatar.SurfaceShader = Shader.Find("OvrAvatar/AvatarSurfaceShader");
            ovrAvatar.SurfaceShaderSelfOccluding = Shader.Find("OvrAvatar/AvatarSurfaceShaderSelfOccluding");
            ovrAvatar.SurfaceShaderPBS = Shader.Find("OvrAvatar/AvatarSurfaceShaderPBS");
            ovrAvatar.SurfaceShaderPBSV2Single = Shader.Find("OvrAvatar/Avatar_Mobile_SingleComponent");
            ovrAvatar.SurfaceShaderPBSV2Combined = Shader.Find("OvrAvatar/Avatar_Mobile_CombinedMesh");
            ovrAvatar.SurfaceShaderPBSV2Simple = Shader.Find("OvrAvatar/Avatar_PC_SingleComponent");
            ovrAvatar.SurfaceShaderPBSV2Loading = Shader.Find("OvrAvatar/Avatar_Mobile_Loader");

            OvrAvatarSDKManager.Instance.RequestAvatarSpecification(
                0ul,
                AvatarSpecificationCallback,
                CombineMeshes,
                LevelOfDetail,
                ForceMobileTextureFormat);
#endif
        }

        private void AvatarSpecificationCallback(IntPtr spec)
        {
            sdkAvatar = CAPI.ovrAvatar_Create(spec, ovrAvatarCapabilities.Hands);
            CAPI.ovrAvatar_SetLeftControllerVisibility(sdkAvatar, showController);
            CAPI.ovrAvatar_SetRightControllerVisibility(sdkAvatar, showController);
            CAPI.ovrAvatar_SetLeftHandVisibility(sdkAvatar, showHand);
            CAPI.ovrAvatar_SetRightHandVisibility(sdkAvatar, showHand);
            isShowingCtrl = showController;
            isShowingHand = showHand;
            ovrAvatar.sdkAvatar = sdkAvatar;
            ovrAvatarDriver.UpdateTransforms(sdkAvatar);

            //Fetch all the assets that this avatar uses.
            var assetCount = CAPI.ovrAvatar_GetReferencedAssetCount(sdkAvatar);
            for (uint i = 0u; i < assetCount; ++i)
            {
                var id = CAPI.ovrAvatar_GetReferencedAsset(sdkAvatar, i);
                if (OvrAvatarSDKManager.Instance.GetAsset(id) == null)
                {
                    OvrAvatarSDKManager.Instance.BeginLoadingAsset(
                        id,
                        LevelOfDetail,
                        AssetLoadedCallback);

                    TrackLoadingId(id);
                }
            }

            if (CombineMeshes)
            {
                OvrAvatarSDKManager.Instance.RegisterCombinedMeshCallback(
                    sdkAvatar,
                    CombinedMeshLoadedCallback);
            }
        }

        private void AssetLoadedCallback(OvrAvatarAsset asset)
        {
            UntrackLoadingId(asset.assetID);
        }

        private void CombinedMeshLoadedCallback(IntPtr assetPtr)
        {
            var meshIDs = CAPI.ovrAvatarAsset_GetCombinedMeshIDs(assetPtr);
            foreach (var id in meshIDs)
            {
                UntrackLoadingId(id);
            }
        }

        private bool isLoadingAsset { get { return loadingIds.Count > 0; } }

        private void TrackLoadingId(ulong id)
        {
            if (loadingIds.Add(id))
            {
                isAvatarReady = false;
            }
        }

        private void UntrackLoadingId(ulong id)
        {
            if (loadingIds.Remove(id) && loadingIds.Count == 0)
            {
                isAvatarReady = true;
#if VIU_OCULUSVR_20_0_OR_NEWER
                OvrAvatarSDKManager.Instance.AddLoadingAvatar(ovrAvatar.GetInstanceID());
#endif
            }
        }
#else
        public const bool SUPPORTED = false;
#endif
    }
}