//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

// This component shows the status that interacting with ColliderEventCaster
public class MaterialChanger : MonoBehaviour
    , IColliderEventHoverEnterHandler
    , IColliderEventHoverExitHandler
    , IColliderEventPressEnterHandler
    , IColliderEventPressExitHandler
{
    private readonly static List<Renderer> s_rederers = new List<Renderer>();

    [NonSerialized]
    private Material currentMat;

    public Material Normal;
    public Material Heightlight;
    public Material Pressed;
    public Material dragged;

    public ColliderButtonEventData.InputButton heighlightButton = ColliderButtonEventData.InputButton.Trigger;

    private HashSet<ColliderHoverEventData> hovers = new HashSet<ColliderHoverEventData>();
    private HashSet<ColliderButtonEventData> presses = new HashSet<ColliderButtonEventData>();
    private IndexedSet<ColliderButtonEventData> drags = new IndexedSet<ColliderButtonEventData>();

    public static void SetAllChildrenHeighlightButton(GameObject parent, ColliderButtonEventData.InputButton button)
    {
        var matChangers = ListPool<MaterialChanger>.Get();
        parent.GetComponentsInChildren(matChangers);
        for (int i = matChangers.Count - 1; i >= 0; --i) { matChangers[i].heighlightButton = button; }
        ListPool<MaterialChanger>.Release(matChangers);
    }

    private void Start()
    {
        UpdateMaterialState();
    }

    public void OnColliderEventHoverEnter(ColliderHoverEventData eventData)
    {
        hovers.Add(eventData);

        UpdateMaterialState();
    }

    public void OnColliderEventHoverExit(ColliderHoverEventData eventData)
    {
        hovers.Remove(eventData);

        UpdateMaterialState();
    }

    public void OnColliderEventPressEnter(ColliderButtonEventData eventData)
    {
        if (eventData.button != heighlightButton) { return; }

        presses.Add(eventData);

        // check if this evenData is dragging me(or ancestry of mine)
        for (int i = eventData.draggingHandlers.Count - 1; i >= 0; --i)
        {
            if (transform.IsChildOf(eventData.draggingHandlers[i].transform))
            {
                drags.AddUnique(eventData);
                break;
            }
        }

        UpdateMaterialState();
    }

    public void OnColliderEventPressExit(ColliderButtonEventData eventData)
    {
        presses.Remove(eventData);

        UpdateMaterialState();
    }

    private void LateUpdate()
    {
        UpdateMaterialState();
    }

    private void OnDisable()
    {
        hovers.Clear();
        presses.Clear();
        drags.Clear();
    }

    private void UpdateMaterialState()
    {
        var targetMat = default(Material);

        if (drags.Count > 0)
        {
            drags.RemoveAll(e => !e.isDragging);
        }

        if (drags.Count > 0)
        {
            targetMat = dragged;
        }
        else if (presses.Count > 0)
        {
            targetMat = Pressed;
        }
        else if (hovers.Count > 0)
        {
            targetMat = Heightlight;
        }
        else
        {
            targetMat = Normal;
        }

        if (ChangeProp.Set(ref currentMat, targetMat))
        {
            SetChildRendererMaterial(targetMat);
        }
    }

    private void SetChildRendererMaterial(Material targetMat)
    {
        GetComponentsInChildren(true, s_rederers);

        if (s_rederers.Count > 0)
        {
            for (int i = s_rederers.Count - 1; i >= 0; --i)
            {
                s_rederers[i].sharedMaterial = targetMat;
            }

            s_rederers.Clear();
        }
    }
}
