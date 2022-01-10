//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("VIU/Teleportable", 3)]
    public class Teleportable : MonoBehaviour
        , ReticlePoser.IMaterialChanger
        , IPointerDownHandler
        , IPointerClickHandler
        , IPointer3DPressExitHandler
    {
        public enum TeleportButton
        {
            None = -1,
            MouseButtonLeft = PointerEventData.InputButton.Left,
            MouseButtonRight = PointerEventData.InputButton.Right,
            MouseButtonMiddle = PointerEventData.InputButton.Middle,
            Trigger = PointerEventData.InputButton.Left,
            Pad = PointerEventData.InputButton.Right,
            Grip = PointerEventData.InputButton.Middle,
        }

        public enum TriggeredTypeEnum
        {
            ButtonUp,
            ButtonDown,
            ButtonClick,
        }

        [Serializable]
        public class UnityEventTeleport : UnityEvent<Teleportable, RaycastResult, float> { }
        public delegate void OnTeleportAction(Teleportable src, RaycastResult hitResult, float delay);

        /// <summary>
        /// The actual transfrom that will be moved Ex. CameraRig
        /// </summary>
        public Transform target;
        /// <summary>
        /// The actual pivot point that want to be teleported to the pointed location Ex. CameraHead
        /// </summary>
        public Transform pivot;
        public float fadeDuration = 0.3f;
        [SerializeField]
        [FlagsFromEnum(typeof(ControllerButton))]
        private ulong primaryTeleportButton = 0ul;
        [SerializeField]
        [FlagsFromEnum(typeof(TeleportButton))]
        private uint secondaryTeleportButton = 1u << (int)TeleportButton.MouseButtonRight;
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("teleportButton")]
        private TeleportButton _teleportButton = TeleportButton.Pad;
        [SerializeField]
        private TriggeredTypeEnum triggeredType;
        [SerializeField]
        private Material m_reticleMaterial;
        [SerializeField]
        private bool rotateToHitObjectFront;
        [SerializeField]
        [Tooltip("Otherwise, teleport to the world position of the hit result")]
        [FormerlySerializedAs("_teleportToCenter")]
        private bool teleportToHitObjectPivot;
        [SerializeField]
        [Tooltip("Use SteamVR_Fade solution (Only works when SteamVR Plugin v1 is installed)")]
        private bool useSteamVRFade = true;
        [SerializeField]
        private UnityEventTeleport onBeforeTeleport = new UnityEventTeleport();
        [SerializeField]
        private UnityEventTeleport onAfterTeleport = new UnityEventTeleport();


        private Quaternion additionalTeleportRotation = Quaternion.identity;

        public ulong PrimeryTeleportButton { get { return primaryTeleportButton; } set { primaryTeleportButton = value; } }

        public uint SecondaryTeleportButton { get { return secondaryTeleportButton; } set { secondaryTeleportButton = value; } }

        [Obsolete("Use IsSecondaryTeleportButtonOn and SetSecondaryTeleportButton instead")]
        public TeleportButton teleportButton
        {
            get
            {
                for (uint btn = 0u, btns = secondaryTeleportButton; btns > 0u; btns >>= 1, ++btn)
                {
                    if ((btns & 1u) > 0u) { return (TeleportButton)btn; }
                }
                return TeleportButton.None;
            }
            set { secondaryTeleportButton = 1u << (int)value; }
        }

        public TriggeredTypeEnum TriggeredType { get { return triggeredType; } set { triggeredType = value; } }

        public Material reticleMaterial { get { return m_reticleMaterial; } set { m_reticleMaterial = value; } }

        public bool RotateToHitObjectFront { get { return rotateToHitObjectFront; } set { rotateToHitObjectFront = value; } }

        public bool TeleportToHitObjectPivot { get { return teleportToHitObjectPivot; } set { teleportToHitObjectPivot = value; } }

        public bool UseSteamVRFade { get { return useSteamVRFade; } set { useSteamVRFade = value; } }

        public virtual Quaternion AdditionalTeleportRotation { get { return additionalTeleportRotation; } set { additionalTeleportRotation = value; } }

        public event OnTeleportAction OnBeforeTeleport
        {
            add { onBeforeTeleport.AddListener(new UnityAction<Teleportable, RaycastResult, float>(value)); }
            remove { onBeforeTeleport.RemoveListener(new UnityAction<Teleportable, RaycastResult, float>(value)); }
        }

        public event OnTeleportAction OnAfterTeleport
        {
            add { onAfterTeleport.AddListener(new UnityAction<Teleportable, RaycastResult, float>(value)); }
            remove { onAfterTeleport.RemoveListener(new UnityAction<Teleportable, RaycastResult, float>(value)); }
        }

        public static event OnTeleportAction OnBeforeAnyTeleport;
        public static event OnTeleportAction OnAfterAnyTeleport;

        public bool IsAborted { get { return abort; } }

        private bool abort;
        private Coroutine teleportCoroutine;

        public bool IsPrimeryTeleportButtonOn(ControllerButton btn) { return EnumUtils.GetFlag(primaryTeleportButton, (int)btn); }

        public void SetPrimeryTeleportButton(ControllerButton btn, bool isOn = true) { EnumUtils.SetFlag(ref primaryTeleportButton, (int)btn, isOn); }

        public void ClearPrimeryTeleportButton() { primaryTeleportButton = 0ul; }

        public bool IsSecondaryTeleportButtonOn(TeleportButton btn) { return EnumUtils.GetFlag(secondaryTeleportButton, (int)btn); }

        public void SetSecondaryTeleportButton(TeleportButton btn, bool isOn = true) { EnumUtils.SetFlag(ref secondaryTeleportButton, (int)btn, isOn); }

        public void ClearSecondaryTeleportButton() { secondaryTeleportButton = 0u; }
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            FindTeleportPivotAndTarget();

            var scriptDir = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this)));
            if (!string.IsNullOrEmpty(scriptDir))
            {
                m_reticleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(scriptDir.Replace("Scripts/Misc", "Materials/Reticle.mat"));
            }
        }

        protected virtual void OnValidate() { RestoreObsoleteTeleportButton(); }
#endif
        private void RestoreObsoleteTeleportButton()
        {
            if (_teleportButton == TeleportButton.Pad) { return; }
            ClearSecondaryTeleportButton();
            SetSecondaryTeleportButton(_teleportButton, true);
            _teleportButton = TeleportButton.Pad;
        }

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

        protected virtual void Awake() { RestoreObsoleteTeleportButton(); }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (triggeredType != TriggeredTypeEnum.ButtonDown) { return; }

            // skip if it was teleporting
            if (teleportCoroutine != null) { return; }

            OnPointerTeleport(eventData);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (triggeredType != TriggeredTypeEnum.ButtonClick) { return; }

            // skip if it was teleporting
            if (teleportCoroutine != null) { return; }

            OnPointerTeleport(eventData);
        }

        // This event happens when pointer leaves this object OR the button is released
        public virtual void OnPointer3DPressExit(Pointer3DEventData eventData)
        {
            if (triggeredType != TriggeredTypeEnum.ButtonUp) { return; }

            // skip if it was teleporting
            if (teleportCoroutine != null) { return; }

            // skip if the pointer leaves this object but the button isn't released
            if (eventData.GetPress()) { return; }

            OnPointerTeleport(eventData);
        }

        protected virtual void OnPointerTeleport(PointerEventData eventData)
        {
            if (!IsValidTeleportButton(eventData)) { return; }

            var hitResult = eventData.pointerCurrentRaycast;

            // check if hit something
            if (!hitResult.isValid) { return; }

            if (target == null || pivot == null)
            {
                FindTeleportPivotAndTarget();
            }

            var rotateHead = additionalTeleportRotation;
            if (rotateToHitObjectFront && target != null && pivot != null)
            {
                var headRotFrontOnFloor = Quaternion.LookRotation(Vector3.ProjectOnPlane(pivot.forward, target.up), target.up);
                rotateHead = Quaternion.Inverse(headRotFrontOnFloor) * hitResult.gameObject.transform.rotation * rotateHead;
            }

            var headVector = target != null && pivot != null ? Vector3.ProjectOnPlane(pivot.position - target.position, target.up) : Vector3.zero;
            var hitPos = teleportToHitObjectPivot ? hitResult.gameObject.transform.position : hitResult.worldPosition;
            var targetPos = hitPos - (rotateHead * headVector);
            var targetRot = target != null ? target.rotation * rotateHead : rotateHead;

            abort = false;

            if (useSteamVRFade && fadeDuration > 0f && !VRModule.isSteamVRPluginDetected)
            {
                Debug.LogWarning("Install SteamVR plugin and enable SteamVRModule support to enable fading");
            }

            var delay = Mathf.Max(0f, fadeDuration * 0.5f);
            if (OnBeforeAnyTeleport != null) { OnBeforeAnyTeleport.Invoke(this, hitResult, delay); }
            if (onBeforeTeleport != null) { onBeforeTeleport.Invoke(this, hitResult, delay); }

            if (abort) { return; }

            teleportCoroutine = StartCoroutine(StartTeleport(hitResult, targetPos, targetRot, delay));
        }

        protected bool IsValidTeleportButton(PointerEventData eventData)
        {
            if (primaryTeleportButton > 0ul)
            {
                VivePointerEventData viveEventData;
                if (eventData.TryGetViveButtonEventData(out viveEventData) && IsPrimeryTeleportButtonOn(viveEventData.viveButton)) { return true; }
            }

            return secondaryTeleportButton > 0u && IsSecondaryTeleportButtonOn((TeleportButton)eventData.button);
        }
#if VIU_STEAMVR && UNITY_STANDALONE
        private bool m_steamVRFadeInitialized;
        private bool m_isSteamVRFading;

        public IEnumerator StartTeleport(RaycastResult hitResult, Vector3 position, Quaternion rotation, float delay)
        {
            if (useSteamVRFade && VRModule.activeModule == VRModuleActiveEnum.SteamVR && !Mathf.Approximately(delay, 0f))
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

                m_isSteamVRFading = true;
                SteamVR_Fade.Start(new Color(0f, 0f, 0f, 1f), delay);

                yield return new WaitForSeconds(delay);
                yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position

                TeleportTarget(position, rotation);

                if (OnAfterAnyTeleport != null) { OnAfterAnyTeleport.Invoke(this, hitResult, delay); }
                if (onAfterTeleport != null) { onAfterTeleport.Invoke(this, hitResult, delay); }

                SteamVR_Fade.Start(new Color(0f, 0f, 0f, 0f), delay);

                yield return new WaitForSeconds(delay);

                m_isSteamVRFading = false;
            }
            else
            {
                if (delay > 0) { yield return new WaitForSeconds(delay); }

                yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position

                TeleportTarget(position, rotation);

                if (OnAfterAnyTeleport != null) { OnAfterAnyTeleport.Invoke(this, hitResult, delay); }
                if (onAfterTeleport != null) { onAfterTeleport.Invoke(this, hitResult, delay); }

                if (delay > 0) { yield return new WaitForSeconds(delay); }
            }

            teleportCoroutine = null;
        }

        public virtual void AbortTeleport()
        {
            abort = true;

            if (teleportCoroutine != null)
            {
                StopCoroutine(teleportCoroutine);
                teleportCoroutine = null;
            }

            if (m_isSteamVRFading)
            {
                m_isSteamVRFading = false;
                SteamVR_Fade.Start(new Color(0f, 0f, 0f, 0f), 0.001f);
            }
        }
#else
        public IEnumerator StartTeleport(RaycastResult hitResult, Vector3 position, Quaternion rotation, float delay)
        {
            if (delay > 0) { yield return new WaitForSeconds(delay); }

            yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position

            TeleportTarget(position, rotation);

            if (OnAfterAnyTeleport != null) { OnAfterAnyTeleport.Invoke(this, hitResult, delay); }
            if (onAfterTeleport != null) { onAfterTeleport.Invoke(this, hitResult, delay); }

            if (delay > 0) { yield return new WaitForSeconds(delay); }

            teleportCoroutine = null;
        }

        public virtual void AbortTeleport()
        {
            abort = true;

            if (teleportCoroutine != null)
            {
                StopCoroutine(teleportCoroutine);
                teleportCoroutine = null;
            }
        }
#endif
        protected void TeleportTarget(Vector3 position, Quaternion rotation)
        {
            if (target == null) { return; }
            target.position = position;
            target.rotation = rotation;
        }
    }
}