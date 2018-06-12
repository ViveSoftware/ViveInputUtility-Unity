//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.VRModuleManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/Teleportable", 3)]
    public class Teleportable : MonoBehaviour, ReticlePoser.IMaterialChanger
        , IPointer3DPressExitHandler
    {
        public enum TeleportButton
        {
            Trigger,
            Pad,
            Grip,
        }

        public Transform target;  // The actual transfrom that will be moved Ex. CameraRig
        public Transform pivot;  // The actual pivot point that want to be teleported to the pointed location Ex. CameraHead
        public float fadeDuration = 0.3f;
        [SerializeField]
        private Material m_reticleMaterial;

        public TeleportButton teleportButton = TeleportButton.Pad;

        private Coroutine teleportCoroutine;

        public Material reticleMaterial { get { return m_reticleMaterial; } set { m_reticleMaterial = value; } }

#if UNITY_EDITOR
        private void Reset()
        {
            FindTeleportPivotAndTarget();

            var scriptDir = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this)));
            if (!string.IsNullOrEmpty(scriptDir))
            {
                m_reticleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(scriptDir.Replace("Scripts/Misc", "Materials/Reticle.mat"));
            }
        }
#endif
        private void FindTeleportPivotAndTarget()
        {
            foreach (var cam in Camera.allCameras)
            {
                if (!cam.enabled) { continue; }
#if UNITY_5_4_OR_NEWER
                // try find vr camera eye
                if (cam.stereoTargetEye != StereoTargetEyeMask.Both) { continue; }
#endif
                pivot = cam.transform;
                target = cam.transform.root == null ? cam.transform : cam.transform.root;
            }
        }

        public void OnPointer3DPressExit(Pointer3DEventData eventData)
        {
            // skip if it was teleporting
            if (teleportCoroutine != null) { return; }

            // skip if it was not releasing the button
            if (eventData.GetPress()) { return; }

            // check if is teleport button
            VivePointerEventData viveEventData;
            if (eventData.TryGetViveButtonEventData(out viveEventData))
            {
                switch (teleportButton)
                {
                    case TeleportButton.Trigger: if (viveEventData.viveButton != ControllerButton.Trigger) { return; } break;
                    case TeleportButton.Pad: if (viveEventData.viveButton != ControllerButton.Pad) { return; } break;
                    case TeleportButton.Grip: if (viveEventData.viveButton != ControllerButton.Grip) { return; } break;
                }
            }
            else if (eventData.button != (PointerEventData.InputButton)teleportButton)
            {
                switch (teleportButton)
                {
                    case TeleportButton.Trigger: if (eventData.button != PointerEventData.InputButton.Left) { return; } break;
                    case TeleportButton.Pad: if (eventData.button != PointerEventData.InputButton.Right) { return; } break;
                    case TeleportButton.Grip: if (eventData.button != PointerEventData.InputButton.Middle) { return; } break;
                }
            }

            var hitResult = eventData.pointerCurrentRaycast;

            // check if hit something
            if (!hitResult.isValid) { return; }

            if (target == null || pivot == null)
            {
                FindTeleportPivotAndTarget();
            }

            var headVector = Vector3.ProjectOnPlane(pivot.position - target.position, target.up);
            var targetPos = hitResult.worldPosition - headVector;

            if (VRModule.activeModule != VRModuleActiveEnum.SteamVR && fadeDuration != 0f)
            {
                Debug.LogWarning("Install SteamVR plugin and enable SteamVRModule support to enable fading");
                fadeDuration = 0f;
            }

            teleportCoroutine = StartCoroutine(StartTeleport(targetPos, fadeDuration));
        }

        private bool m_steamVRFadeInitialized;

        public IEnumerator StartTeleport(Vector3 position, float duration)
        {
#if VIU_STEAMVR
            var halfDuration = Mathf.Max(0f, duration * 0.5f);

            if (VRModule.activeModule == VRModuleActiveEnum.SteamVR && !Mathf.Approximately(halfDuration, 0f))
            {
                if (!m_steamVRFadeInitialized)
                {
                    // add SteamVR_Fade to the last rendered stereo camera
                    var fadeScripts = FindObjectsOfType<SteamVR_Fade>();
                    if (fadeScripts == null || fadeScripts.Length <= 0)
                    {
                        var topCam = SteamVR_Render.Top();
                        if (topCam != null)
                        {
                            topCam.gameObject.AddComponent<SteamVR_Fade>();
                        }
                    }

                    m_steamVRFadeInitialized = true;
                }

                SteamVR_Fade.Start(new Color(0f, 0f, 0f, 1f), halfDuration);
                yield return new WaitForSeconds(halfDuration);
                yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position
                target.position = position;
                SteamVR_Fade.Start(new Color(0f, 0f, 0f, 0f), halfDuration);
                yield return new WaitForSeconds(halfDuration);
            }
            else
#endif
            {
                yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position
                target.position = position;
            }

            teleportCoroutine = null;
        }
    }
}