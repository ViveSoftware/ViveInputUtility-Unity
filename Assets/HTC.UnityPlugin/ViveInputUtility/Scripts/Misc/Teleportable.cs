//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Vive;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Teleportable : MonoBehaviour
    , IPointerExitHandler
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

    public TeleportButton teleportButton = TeleportButton.Pad;

    private Coroutine teleportCoroutine;

    public ControllerButton teleportViveButton
    {
        get
        {
            switch (teleportButton)
            {
                case TeleportButton.Pad:
                    return ControllerButton.Pad;

                case TeleportButton.Grip:
                    return ControllerButton.Grip;

                case TeleportButton.Trigger:
                default:
                    return ControllerButton.Trigger;
            }
        }
    }
#if UNITY_EDITOR
    private void Reset()
    {
        FindTeleportPivotAndTarget();
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

    public void OnPointerExit(PointerEventData eventData)
    {
        // skip if it was teleporting
        if (teleportCoroutine != null) { return; }

        VivePointerEventData viveEventData;
        if (!eventData.TryGetViveButtonEventData(out viveEventData)) { return; }

        // don't teleport if it was not releasing the button
        if (viveEventData.eligibleForClick) { return; }

        if (viveEventData.viveButton != teleportViveButton) { return; }

        var hitResult = eventData.pointerCurrentRaycast;
        if (!hitResult.isValid) { return; }

        if (target == null || pivot == null)
        {
            FindTeleportPivotAndTarget();
        }

        var headVector = Vector3.ProjectOnPlane(pivot.position - target.position, target.up);
        var targetPos = hitResult.worldPosition - headVector;

        teleportCoroutine = StartCoroutine(StartTeleport(targetPos, fadeDuration));
    }
#if VIU_STEAMVR
    private bool m_steamVRFadeInitialized;

    public IEnumerator StartTeleport(Vector3 position, float duration)
    {
        var halfDuration = Mathf.Max(0f, duration * 0.5f);

        if (!Mathf.Approximately(halfDuration, 0f))
        {
            if (!m_steamVRFadeInitialized)
            {
                // add SteamVR_Fade to the last rendered stereo camera
                var fadeScripts = FindObjectsOfType<SteamVR_Fade>();
                if (fadeScripts == null || fadeScripts.Length <= 0)
                {
                    var topCam = SteamVR_Render.Top().gameObject;
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
        {
            yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position
            target.position = position;
        }

        teleportCoroutine = null;
    }
#else
    public IEnumerator StartTeleport(Vector3 position, float duration)
    {
        var halfDuration = Mathf.Max(0f, duration * 0.5f);

        if (!Mathf.Approximately(halfDuration, 0f))
        {
            Debug.LogWarning("SteamVR plugin not found! install it to enable SteamVR_Fade!");
            fadeDuration = 0f;
        }

        yield return new WaitForEndOfFrame(); // to avoid from rendering guideline in wrong position
        target.position = position;

        teleportCoroutine = null;
    }
#endif
}