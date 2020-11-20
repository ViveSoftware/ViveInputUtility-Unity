using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.Vive;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ViveTouchRaycaster : Pointer3DRaycaster
{
    [SerializeField]
    float fingerRadius = 0.02f;

    public float FingerRadius { get { return fingerRadius; } }

    protected override void Start()
    {
        base.Start();
        buttonEventDataList.Add(new TouchPointerEventData(this, EventSystem.current));
    }
}
public class TouchPointerEventData : Pointer3DEventData
{
    public ViveTouchRaycaster touchRaycaster { get; private set; }

    public TouchPointerEventData(ViveTouchRaycaster ownerRaycaster, EventSystem eventSystem) : base(ownerRaycaster, eventSystem)
    {
        this.touchRaycaster = ownerRaycaster;
    }

    public override bool GetPress()
    {
        return (touchRaycaster.FirstRaycastResult().distance < touchRaycaster.FingerRadius + ViveFingerPoseTracker.HitOffset);
    }

    public override bool GetPressDown() { return false; }

    public override bool GetPressUp() { return false; }
}