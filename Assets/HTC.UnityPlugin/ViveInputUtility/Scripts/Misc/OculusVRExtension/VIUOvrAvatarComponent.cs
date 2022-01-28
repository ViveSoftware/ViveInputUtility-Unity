using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Reflection;

#if VIU_OCULUSVR_1_32_0_OR_NEWER
using Oculus.Avatar;
#endif

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    public class VIUOvrAvatarComponent : MonoBehaviour
    {
#if VIU_OCULUSVR_1_32_0_OR_NEWER
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
                if (ctrlComp == null)
                {
                    var ctrlCompData = default(ovrAvatarControllerComponent);
                    var ctrlCompDataBase = default(ovrAvatarComponent);
                    if (TryGetCtrlComponent(owner.sdkAvatar, ref ctrlCompDataBase, ref ctrlCompData))
                    {
                        var goParent = new GameObject(ctrlCompDataBase.name);
                        goParent.transform.SetParent(transform, false);
                        ctrlComp = AddAvatarCtrlComp(goParent);

                        for (uint i = 0, imax = ctrlCompDataBase.renderPartCount; i < imax; ++i)
                        {
                            var goPart = new GameObject(ctrlCompDataBase.name + "_renderPart_" + (int)i);
                            goPart.transform.SetParent(goParent.transform, false);
                            IntPtr renderPart = OvrAvatar.GetRenderPart(ctrlCompDataBase, i);
                            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                            var ovrRenderPart = AddAvatarRenderComp(goPart, type, renderPart, ctrlCompDataBase, true);
                            if (ovrRenderPart != null)
                            {
                                ctrlComp.RenderParts.Add(ovrRenderPart);
                            }
                        }
                    }

                    PrintHierarchy(ctrlComp.transform, 0);
                }

                UpdateAvatarCtrlComp(ctrlComp);

                if (handComp == null)
                {
                    var handCompDataBase = default(ovrAvatarComponent);
                    var handCompData = default(ovrAvatarHandComponent);
                    if (TryGetHandComponent(owner.sdkAvatar, ref handCompDataBase, ref handCompData))
                    {
                        var goParent = new GameObject(handCompDataBase.name);
                        goParent.transform.SetParent(transform, false);
                        handComp = AddAvatarHandComp(goParent);

                        for (uint i = 0, imax = handCompDataBase.renderPartCount; i < imax; ++i)
                        {
                            var goPart = new GameObject(handCompDataBase.name + "_renderPart_" + (int)i);
                            goPart.transform.SetParent(goParent.transform, false);
                            IntPtr renderPart = OvrAvatar.GetRenderPart(handCompDataBase, i);
                            ovrAvatarRenderPartType type = CAPI.ovrAvatarRenderPart_GetType(renderPart);
                            var ovrRenderPart = AddAvatarRenderComp(goPart, type, renderPart, handCompDataBase, false);
                            if (ovrRenderPart != null)
                            {
                                handComp.RenderParts.Add(ovrRenderPart);
                            }
                        }
                    }

                    PrintHierarchy(handComp.transform, 0);
                }

                UpdateAvatarHandComp(handComp);
            }
            catch (Exception e)
            {
                Debug.Log("lawwong CompUpdate " + (isLeft ? "L" : "R") + " " + e.Message + " -- " + e.StackTrace);
            }
        }

        private void PrintHierarchy(Transform t, int indent)
        {
            var indentStr = string.Empty;
            for (int i = indent; i > 0; --i) { indentStr += "  "; }
            Debug.Log("lawwongPH " + indentStr + "Trans: " + t.name + " " + (t.gameObject.activeInHierarchy ? "O" : "X"));
            indentStr += "  ";
            var comps = Utility.ListPool<Component>.Get();
            try
            {
                t.GetComponents(comps);
                for (int i = 0, imax = comps.Count; i < imax; ++i)
                {
                    var compType = comps[i].GetType();
                    if (comps[i] is MonoBehaviour)
                    {
                        Debug.Log("lawwongPH " + indentStr + "Comps: " + compType.Name + " " + (((MonoBehaviour)comps[i]).enabled ? "O" : "X"));
                    }
                    else if (compType != typeof(Transform))
                    {
                        Debug.Log("lawwongPH " + indentStr + "Comps: " + compType.Name);
                    }
                }
            }
            finally
            {
                Utility.ListPool<Component>.Release(comps);
                comps = null;
            }
            for (int i = 0, imax = t.childCount; i < imax; ++i)
            {
                PrintHierarchy(t.GetChild(i), indent + 1);
            }
        }

        private bool TryGetCtrlComponent(IntPtr avatar, ref ovrAvatarComponent compDataBase, ref ovrAvatarControllerComponent compData)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            if (isLeft)
            {
                if (CAPI.ovrAvatarPose_GetLeftControllerComponent(avatar, ref compData))
                {
                    CAPI.ovrAvatarComponent_Get(compData.renderComponent, true, ref compDataBase);
                    return true;
                }
            }
            else
            {
                if (CAPI.ovrAvatarPose_GetRightControllerComponent(avatar, ref compData))
                {
                    CAPI.ovrAvatarComponent_Get(compData.renderComponent, true, ref compDataBase);
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
                compData = compDataRef.Value;
                compDataBase = (ovrAvatarComponent)Marshal.PtrToStructure(compData.renderComponent, typeof(ovrAvatarComponent));
                return true;
            }
            return false;
#endif
        }

        private bool TryGetHandComponent(IntPtr avatar, ref ovrAvatarComponent compDataBase, ref ovrAvatarHandComponent compData)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            if (isLeft)
            {
                if (CAPI.ovrAvatarPose_GetLeftHandComponent(avatar, ref compData))
                {
                    CAPI.ovrAvatarComponent_Get(compData.renderComponent, true, ref compDataBase);
                    return true;
                }
            }
            else
            {
                if (CAPI.ovrAvatarPose_GetRightHandComponent(avatar, ref compData))
                {
                    CAPI.ovrAvatarComponent_Get(compData.renderComponent, true, ref compDataBase);
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
                compData = compDataRef.Value;
                compDataBase = (ovrAvatarComponent)Marshal.PtrToStructure(compData.renderComponent, typeof(ovrAvatarComponent));
                return true;
            }
            return false;
#endif
        }

        private OvrAvatarComponent AddAvatarCtrlComp(GameObject go)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            var comp = go.AddComponent<OvrAvatarTouchController>();
            comp.SetOvrAvatarOwner(owner.ovrAvatar);
            comp.isLeftHand = isLeft;
            return comp;
#else
            go.AddComponent<OvrAvatarTouchController>();
            return go.AddComponent<OvrAvatarComponent>();
#endif
        }

        private OvrAvatarComponent AddAvatarHandComp(GameObject go)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
            var comp = go.AddComponent<OvrAvatarHand>();
            comp.SetOvrAvatarOwner(owner.ovrAvatar);
            comp.isLeftHand = isLeft;
            return comp;
#else
            go.AddComponent<OvrAvatarHand>();
            return go.AddComponent<OvrAvatarComponent>();
#endif
        }

        private void UpdateAvatarCtrlComp(OvrAvatarComponent comp)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
#else
            var compDataBase = default(ovrAvatarComponent);
            var compData = default(ovrAvatarControllerComponent);
            if (TryGetCtrlComponent(owner.sdkAvatar, ref compDataBase, ref compData))
            {
                comp.UpdateAvatar(compDataBase, owner.ovrAvatar);
            }
#endif
        }

        private void UpdateAvatarHandComp(OvrAvatarComponent comp)
        {
#if VIU_OCULUSVR_20_0_OR_NEWER
#else
            var compDataBase = default(ovrAvatarComponent);
            var compData = default(ovrAvatarHandComponent);
            if (TryGetHandComponent(owner.sdkAvatar, ref compDataBase, ref compData))
            {
                comp.UpdateAvatar(compDataBase, owner.ovrAvatar);
            }
#endif
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
                        Debug.Log("lawwong AddAvatarRenderComp " + go.name + " " + (wasActive ? "O" : "X") + " >> " + (go.activeSelf ? "O" : "X"));
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
#endif
    }
}