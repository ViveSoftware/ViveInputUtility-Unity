//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Reflection;

#if VIU_OCULUSVR_AVATAR
using Oculus.Avatar;
#endif

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    public class VIUOvrAvatarComponent : MonoBehaviour
    {
#if VIU_OCULUSVR_AVATAR
        [SerializeField]
        private VIUOvrAvatar owner;
        [SerializeField]
        private bool isLeft = true;

        private OvrAvatarComponent ctrlComp;
        private OvrAvatarComponent handComp;

        public VIUOvrAvatar Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        public bool IsLeft
        {
            get { return isLeft; }
            set { isLeft = value; }
        }

        private void Update()
        {
            if (owner == null || !owner.isAvatarReady) { return; }

            try
            {
                var compData = default(ovrAvatarComponent);
                var ctrlCompData = default(ovrAvatarControllerComponent);
                var handCompData = default(ovrAvatarHandComponent);

                if (TryGetCtrlComponent(owner.sdkAvatar, ref ctrlCompData, ref compData))
                {
                    if (ctrlComp == null)
                    {
                        var goParent = new GameObject(compData.name);
                        goParent.transform.SetParent(transform, false);
                        ctrlComp = AddAvatarCtrlComp(goParent);

                        for (uint i = 0, imax = compData.renderPartCount; i < imax; ++i)
                        {
                            var goPart = new GameObject(compData.name + "_renderPart_" + (int)i);
                            goPart.transform.SetParent(goParent.transform, false);
                            IntPtr renderPart = OvrAvatar.GetRenderPart(compData, i);
                            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                            var ovrRenderPart = AddAvatarRenderComp(goPart, type, renderPart, compData, true);
                            if (ovrRenderPart != null)
                            {
                                ctrlComp.RenderParts.Add(ovrRenderPart);
                            }
                        }
                    }

                    UpdateAvatarCtrlComp(ctrlComp, ref ctrlCompData, ref compData);
                }

                if (TryGetHandComponent(owner.sdkAvatar, ref handCompData, ref compData))
                {
                    if (handComp == null)
                    {
                        var goParent = new GameObject(compData.name);
                        goParent.transform.SetParent(transform, false);
                        handComp = AddAvatarHandComp(goParent);

                        for (uint i = 0, imax = compData.renderPartCount; i < imax; ++i)
                        {
                            var goPart = new GameObject(compData.name + "_renderPart_" + (int)i);
                            goPart.transform.SetParent(goParent.transform, false);
                            IntPtr renderPart = OvrAvatar.GetRenderPart(compData, i);
                            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                            var ovrRenderPart = AddAvatarRenderComp(goPart, type, renderPart, compData, false);
                            if (ovrRenderPart != null)
                            {
                                handComp.RenderParts.Add(ovrRenderPart);
                            }
                        }
                    }

                    UpdateAvatarHandComp(handComp, ref handCompData, ref compData);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private bool TryGetCtrlComponent(IntPtr avatar, ref ovrAvatarControllerComponent ctrlCompData, ref ovrAvatarComponent compData)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            if (isLeft)
            {
                if (CAPI.ovrAvatarPose_GetLeftControllerComponent(avatar, ref ctrlCompData))
                {
                    CAPI.ovrAvatarComponent_Get(ctrlCompData.renderComponent, true, ref compData);
                    return true;
                }
            }
            else
            {
                if (CAPI.ovrAvatarPose_GetRightControllerComponent(avatar, ref ctrlCompData))
                {
                    CAPI.ovrAvatarComponent_Get(ctrlCompData.renderComponent, true, ref compData);
                    return true;
                }
            }
            return false;
#else
            var compDataRef = IsLeft ?
                CAPI.ovrAvatarPose_GetLeftControllerComponent(owner.sdkAvatar) :
                CAPI.ovrAvatarPose_GetRightControllerComponent(owner.sdkAvatar);
            if (compDataRef.HasValue)
            {
                ctrlCompData = compDataRef.Value;
                compData = (ovrAvatarComponent)Marshal.PtrToStructure(ctrlCompData.renderComponent, typeof(ovrAvatarComponent));
                return true;
            }
            return false;
#endif
        }

        private bool TryGetHandComponent(IntPtr avatar, ref ovrAvatarHandComponent handCompData, ref ovrAvatarComponent compData)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            if (isLeft)
            {
                if (CAPI.ovrAvatarPose_GetLeftHandComponent(avatar, ref handCompData))
                {
                    CAPI.ovrAvatarComponent_Get(handCompData.renderComponent, true, ref compData);
                    return true;
                }
            }
            else
            {
                if (CAPI.ovrAvatarPose_GetRightHandComponent(avatar, ref handCompData))
                {
                    CAPI.ovrAvatarComponent_Get(handCompData.renderComponent, true, ref compData);
                    return true;
                }
            }
            return false;
#else
            var compDataRef = IsLeft ?
                CAPI.ovrAvatarPose_GetLeftHandComponent(owner.sdkAvatar) :
                CAPI.ovrAvatarPose_GetRightHandComponent(owner.sdkAvatar);
            if (compDataRef.HasValue)
            {
                handCompData = compDataRef.Value;
                compData = (ovrAvatarComponent)Marshal.PtrToStructure(handCompData.renderComponent, typeof(ovrAvatarComponent));
                return true;
            }
            return false;
#endif
        }

        private OvrAvatarComponent AddAvatarCtrlComp(GameObject go)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            var comp = go.AddComponent<OvrAvatarComponent>();
            comp.SetOvrAvatarOwner(owner.ovrAvatar);
            return comp;
#else
            go.AddComponent<OvrAvatarTouchController>();
            return go.AddComponent<OvrAvatarComponent>();
#endif
        }

        private OvrAvatarComponent AddAvatarHandComp(GameObject go)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            var comp = go.AddComponent<OvrAvatarComponent>();
            comp.SetOvrAvatarOwner(owner.ovrAvatar);
            return comp;
#else
            go.AddComponent<OvrAvatarHand>();
            return go.AddComponent<OvrAvatarComponent>();
#endif
        }

        private void UpdateAvatarCtrlComp(OvrAvatarComponent comp, ref ovrAvatarControllerComponent ctrlCompData, ref ovrAvatarComponent compData)
        {
            var recoverPos = comp.transform.localPosition;
            var recoverRot = comp.transform.localRotation;
#if VIU_OCULUSVR_20_0_OR_NEWER
            comp.UpdateAvatar(ctrlCompData.renderComponent);
#else
            comp.UpdateAvatar(compData, owner.ovrAvatar);
#endif
            comp.transform.localPosition = recoverPos;
            comp.transform.localRotation = recoverRot;
        }

        private void UpdateAvatarHandComp(OvrAvatarComponent comp, ref ovrAvatarHandComponent handCompData, ref ovrAvatarComponent compData)
        {
            var recoverPos = comp.transform.localPosition;
            var recoverRot = comp.transform.localRotation;
#if VIU_OCULUSVR_20_0_OR_NEWER
            comp.UpdateAvatar(handCompData.renderComponent);
#else
            comp.UpdateAvatar(compData, owner.ovrAvatar);
#endif
            comp.transform.localPosition = recoverPos;
            comp.transform.localRotation = recoverRot;
        }

        private OvrAvatarRenderComponent AddAvatarRenderComp(GameObject go, ovrAvatarRenderPartType type, IntPtr renderPart, ovrAvatarComponent compData, bool isController)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
#endif
            switch (type)
            {
#if VIU_OCULUSVR_20_0_OR_NEWER
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRender(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderComponent>();
                        typeof(OvrAvatarSkinnedMeshRenderComponent).GetMethod("Initialize", flags).Invoke(renderer, new object[]
                        { rendererData, null, null, 0, 0 });
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
                        var wasActive = go.activeSelf;
                        typeof(OvrAvatarSkinnedMeshRenderPBSComponent).GetMethod("Initialize", flags).Invoke(renderer, new object[]
                        { rendererData, null, 0, 0 });
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
                        typeof(OvrAvatarSkinnedMeshPBSV2RenderComponent).GetMethod("Initialize", flags).Invoke(renderer, new object[]
                        {
                            renderPart,
                            rendererData,
                            owner.ovrMaterialManager,
                            0,
                            0,
                            owner.CombineMeshes,
                            owner.LevelOfDetail,
                            false,
                            owner.ovrAvatar,
                            isController,
                        });
                        return renderer;
                    }
#elif VIU_OCULUSVR_1_37_0_OR_NEWER
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRender(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderComponent>();
                        renderer.Initialize(rendererData, null, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
                        renderer.Initialize(rendererData, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
                        renderer.Initialize(renderPart, rendererData, owner.ovrMaterialManager, 0, 0, ctrlComp.RenderParts.Count, false, owner.LevelOfDetail, false, owner.ovrAvatar, isController);
                        return renderer;
                    }
#elif VIU_OCULUSVR_1_36_0_OR_NEWER
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRender(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderComponent>();
                        renderer.Initialize(rendererData, null, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
                        renderer.Initialize(rendererData, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
                        renderer.Initialize(renderPart, rendererData, owner.ovrMaterialManager, 0, 0, ctrlComp.RenderParts.Count, false, owner.LevelOfDetail, false, owner.ovrAvatar);
                        return renderer;
                    }
#else
                case ovrAvatarRenderPartType.SkinnedMeshRender:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRender(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderComponent>();
                        renderer.Initialize(rendererData, null, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBS(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshRenderPBSComponent>();
                        renderer.Initialize(rendererData, null, 0, 0, ctrlComp.RenderParts.Count);
                        return renderer;
                    }
                // TODO
                //case ovrAvatarRenderPartType.ProjectorRender:
                case ovrAvatarRenderPartType.SkinnedMeshRenderPBS_V2:
                    {
                        var rendererData = CAPI.ovrAvatarRenderPart_GetSkinnedMeshRenderPBSV2(renderPart);
                        var renderer = go.AddComponent<OvrAvatarSkinnedMeshPBSV2RenderComponent>();
                        renderer.Initialize(renderPart, rendererData, owner.ovrMaterialManager, 0, 0, ctrlComp.RenderParts.Count, false, owner.LevelOfDetail);
                        return renderer;
                    }
#endif
                default:
                    Debug.LogWarning("Unsupported render part type " + type.ToString());
                    return null;
            }
        }
#else
        public VIUOvrAvatar Owner { get; set; }
        public bool IsLeft { get; set; }
#endif
    }
}