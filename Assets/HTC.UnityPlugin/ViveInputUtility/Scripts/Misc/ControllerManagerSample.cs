//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using UnityEngine;

public class ControllerManagerSample : MonoBehaviour
{
    public enum CustomModelActiveModeEnum
    {
        None,
        ActiveOnGripped,
        ToggleByDoubleGrip,
    }

    public enum LaserPointerActiveModeEnum
    {
        None,
        ToggleByMenuClick,
    }

    public enum CurvePointerActiveModeEnum
    {
        None,
        ActiveOnPadPressed,
        ToggleByPadDoubleClick
    }

    // after changing following public fields in playing mode, call UpdateStatus() to apply changes
    [Header("Mode Settings")]
    public bool hideRenderModelOnGrab = true;

    public CustomModelActiveModeEnum customModelActiveMode;
    public LaserPointerActiveModeEnum laserPointerActiveMode;
    public CurvePointerActiveModeEnum curvePointerActiveMode;

    [Header("Right controller")]
    public GameObject rightRenderModel;
    public GameObject rightCustomModel;

    public GameObject rightGrabber;
    public GameObject rightLaserPointer;
    public GameObject rightCurvePointer;

    [Header("Left controller")]
    public GameObject leftRenderModel;
    public GameObject leftCustomModel;

    public GameObject leftGrabber;
    public GameObject leftLaserPointer;
    public GameObject leftCurvePointer;

    private bool m_rightCustomModelActive;
    private bool m_rightLaserPointerActive;
    private bool m_rightCurvePointerActive;

    private bool m_leftCustomModelActive;
    private bool m_leftLaserPointerActive;
    private bool m_leftCurvePointerActive;

    private HashSet<GameObject> rightGrabbingSet = new HashSet<GameObject>();
    private HashSet<GameObject> leftGrabbingSet = new HashSet<GameObject>();

    //properties
    public bool rightGrabberActive
    {
        get { return !m_rightLaserPointerActive && !m_rightCurvePointerActive && !m_rightCustomModelActive; }
    }

    public bool rightLaserPointerActive
    {
        get { return m_rightLaserPointerActive; }
        set { SetRightLaserPointerActive(value); }
    }

    public bool rightCurvePointerActive
    {
        get { return m_rightCurvePointerActive; }
        set { SetRightCurvePointerActive(value); }
    }

    public bool rightCustomModelActive
    {
        get { return m_rightCustomModelActive; }
        set { SetRightCustomModelActive(value); }
    }

    public bool leftGrabberActive
    {
        get { return !m_leftLaserPointerActive && !m_leftCurvePointerActive && !m_leftCustomModelActive; }
    }

    public bool leftLaserPointerActive
    {
        get { return m_leftLaserPointerActive; }
        set { SetLeftLaserPointerActive(value); }
    }

    public bool leftCurvePointerActive
    {
        get { return m_leftCurvePointerActive; }
        set { SetLeftLaserPointerActive(value); }
    }

    public bool leftCustomModelActive
    {
        get { return m_leftCustomModelActive; }
        set { SetLeftCustomModelActive(value); }
    }

    public bool SetRightLaserPointerActive(bool value)
    {
        if (ChangeProp.Set(ref m_rightLaserPointerActive, value))
        {
            if (value) { m_rightCurvePointerActive = false; m_rightCustomModelActive = false; }
            return true;
        }
        return false;
    }

    public bool SetRightCurvePointerActive(bool value)
    {
        if (ChangeProp.Set(ref m_rightCurvePointerActive, value))
        {
            if (value) { m_rightLaserPointerActive = false; m_rightCustomModelActive = false; }
            return true;
        }
        return false;
    }

    public bool SetRightCustomModelActive(bool value)
    {
        if (ChangeProp.Set(ref m_rightCustomModelActive, value))
        {
            if (value) { m_rightLaserPointerActive = false; m_rightCurvePointerActive = false; }
            return true;
        }
        return false;
    }

    public bool SetLeftLaserPointerActive(bool value)
    {
        if (ChangeProp.Set(ref m_leftLaserPointerActive, value))
        {
            if (value) { m_leftCurvePointerActive = false; m_leftCustomModelActive = false; }
            return true;
        }
        return false;
    }

    public bool SetLeftCurvePointerActive(bool value)
    {
        if (ChangeProp.Set(ref m_leftCurvePointerActive, value))
        {
            if (value) { m_leftLaserPointerActive = false; m_leftCustomModelActive = false; }
            return true;
        }
        return false;
    }

    public bool SetLeftCustomModelActive(bool value)
    {
        if (ChangeProp.Set(ref m_leftCustomModelActive, value))
        {
            if (value) { m_leftLaserPointerActive = false; m_leftCurvePointerActive = false; }
            return true;
        }
        return false;
    }

    public void ToggleRightLaserPointer() { rightLaserPointerActive = !rightLaserPointerActive; }
    public void ToggleRightCurvePointer() { rightCurvePointerActive = !rightCurvePointerActive; }
    public void ToggleRightCustomModel() { rightCustomModelActive = !rightCustomModelActive; }
    public void ToggleLeftLaserPointer() { leftLaserPointerActive = !leftLaserPointerActive; }
    public void ToggleLeftCurvePointer() { leftCurvePointerActive = !leftCurvePointerActive; }
    public void ToggleLeftCustomModel() { leftCustomModelActive = !leftCustomModelActive; }

#if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateActivity();
        }
    }

#endif

    protected virtual void Start()
    {
        m_rightLaserPointerActive = false;
        m_rightCustomModelActive = false;
        m_rightCurvePointerActive = false;
        m_leftLaserPointerActive = false;
        m_leftCustomModelActive = false;
        m_leftCurvePointerActive = false;

        UpdateActivity();
    }

    protected virtual void LateUpdate()
    {
        var needUpdate = false;

        switch (laserPointerActiveMode)
        {
            case LaserPointerActiveModeEnum.None:
                needUpdate |= SetRightLaserPointerActive(false);
                needUpdate |= SetLeftLaserPointerActive(false);
                break;

            case LaserPointerActiveModeEnum.ToggleByMenuClick:
                if (ViveInput.GetPressUpEx(HandRole.RightHand, ControllerButton.Menu))
                {
                    ToggleRightLaserPointer();
                    needUpdate = true;
                }

                if (ViveInput.GetPressUpEx(HandRole.LeftHand, ControllerButton.Menu))
                {
                    ToggleLeftLaserPointer();
                    needUpdate = true;
                }
                break;
        }

        switch (curvePointerActiveMode)
        {
            case CurvePointerActiveModeEnum.None:
                needUpdate |= SetRightCurvePointerActive(false);
                needUpdate |= SetLeftCurvePointerActive(false);
                break;

            case CurvePointerActiveModeEnum.ActiveOnPadPressed:
                needUpdate |= SetRightCurvePointerActive(ViveInput.GetPressEx(HandRole.RightHand, ControllerButton.Pad));
                needUpdate |= SetLeftCurvePointerActive(ViveInput.GetPressEx(HandRole.LeftHand, ControllerButton.Pad));
                break;

            case CurvePointerActiveModeEnum.ToggleByPadDoubleClick:
                if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.Pad) && ViveInput.ClickCountEx(HandRole.RightHand, ControllerButton.Pad) == 2)
                {
                    ToggleRightCurvePointer();
                    needUpdate = true;
                }

                if (ViveInput.GetPressDownEx(HandRole.LeftHand, ControllerButton.Pad) && ViveInput.ClickCountEx(HandRole.LeftHand, ControllerButton.Pad) == 2)
                {
                    ToggleLeftCurvePointer();
                    needUpdate = true;
                }
                break;
        }

        switch (customModelActiveMode)
        {
            case CustomModelActiveModeEnum.None:
                needUpdate |= ChangeProp.Set(ref m_rightCustomModelActive, false);
                needUpdate |= ChangeProp.Set(ref m_leftCustomModelActive, false);
                break;

            case CustomModelActiveModeEnum.ActiveOnGripped:
                needUpdate |= SetRightCustomModelActive(ViveInput.GetPressEx(HandRole.RightHand, ControllerButton.Grip));
                needUpdate |= SetLeftCustomModelActive(ViveInput.GetPressEx(HandRole.LeftHand, ControllerButton.Grip));
                break;

            case CustomModelActiveModeEnum.ToggleByDoubleGrip:
                if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.Grip) && ViveInput.ClickCountEx(HandRole.RightHand, ControllerButton.Grip) == 2)
                {
                    ToggleRightCustomModel();
                    needUpdate = true;
                }
                if (ViveInput.GetPressDownEx(HandRole.LeftHand, ControllerButton.Grip) && ViveInput.ClickCountEx(HandRole.LeftHand, ControllerButton.Grip) == 2)
                {
                    ToggleLeftCustomModel();
                    needUpdate = true;
                }
                break;
        }

        if (needUpdate) { UpdateActivity(); }
    }

    public void OnGrabbed(BasicGrabbable grabbedObj)
    {
        ViveColliderButtonEventData viveEventData;
        if (!grabbedObj.grabbedEvent.TryGetViveButtonEventData(out viveEventData)) { return; }

        switch (viveEventData.viveRole.ToRole<HandRole>())
        {
            case HandRole.RightHand:
                if (rightGrabbingSet.Add(grabbedObj.gameObject) && rightGrabbingSet.Count == 1)
                {
                    UpdateActivity();
                }
                break;

            case HandRole.LeftHand:
                if (leftGrabbingSet.Add(grabbedObj.gameObject) && leftGrabbingSet.Count == 1)
                {
                    UpdateActivity();
                }
                break;
        }
    }

    public void OnRelease(BasicGrabbable releasedObj)
    {
        ViveColliderButtonEventData viveEventData;
        if (!releasedObj.grabbedEvent.TryGetViveButtonEventData(out viveEventData)) { return; }

        switch (viveEventData.viveRole.ToRole<HandRole>())
        {
            case HandRole.RightHand:
                if (rightGrabbingSet.Remove(releasedObj.gameObject) && rightGrabbingSet.Count == 0)
                {
                    UpdateActivity();
                }
                break;

            case HandRole.LeftHand:
                if (leftGrabbingSet.Remove(releasedObj.gameObject) && leftGrabbingSet.Count == 0)
                {
                    UpdateActivity();
                }
                break;
        }
    }

    public void UpdateActivity()
    {
        var rightRenderModelShouldActive = !m_rightCustomModelActive && (!hideRenderModelOnGrab || rightGrabbingSet.Count == 0);
        var rightCustomModelShouldActive = m_rightCustomModelActive;
        var rightLaserPointerShouldActive = m_rightLaserPointerActive;
        var rightCurvePointerShouldActive = m_rightCurvePointerActive;
        var rightGraggerShouldActive = !m_rightLaserPointerActive && !m_rightCustomModelActive && !m_rightCurvePointerActive;

        if (rightRenderModel != null && rightRenderModel.activeSelf != rightRenderModelShouldActive)
        {
            rightRenderModel.SetActive(rightRenderModelShouldActive);
        }

        if (rightCustomModel != null && rightCustomModel.activeSelf != rightCustomModelShouldActive)
        {
            rightCustomModel.SetActive(rightCustomModelShouldActive);
        }

        if (rightLaserPointer != null && rightLaserPointer.activeSelf != rightLaserPointerShouldActive)
        {
            rightLaserPointer.SetActive(rightLaserPointerShouldActive);
        }

        if (rightCurvePointer != null && rightCurvePointer.activeSelf != rightCurvePointerShouldActive)
        {
            rightCurvePointer.SetActive(rightCurvePointerShouldActive);
        }

        if (rightGrabber != null && rightGrabber.activeSelf != rightGraggerShouldActive)
        {
            rightGrabber.SetActive(rightGraggerShouldActive);
        }

        var leftRenderModelShouldActive = !m_leftCustomModelActive && (!hideRenderModelOnGrab || leftGrabbingSet.Count == 0);
        var leftCustomModelShouldActive = m_leftCustomModelActive;
        var leftLaserPointerShouldActive = m_leftLaserPointerActive;
        var leftCurvePointerShouldActive = m_leftCurvePointerActive;
        var leftGraggerShouldActive = !m_leftLaserPointerActive && !m_leftCustomModelActive && !m_leftCurvePointerActive;

        if (leftRenderModel != null && leftRenderModel.activeSelf != leftRenderModelShouldActive)
        {
            leftRenderModel.SetActive(leftRenderModelShouldActive);
        }

        if (leftCustomModel != null && leftCustomModel.activeSelf != leftCustomModelShouldActive)
        {
            leftCustomModel.SetActive(leftCustomModelShouldActive);
        }

        if (leftLaserPointer != null && leftLaserPointer.activeSelf != leftLaserPointerShouldActive)
        {
            leftLaserPointer.SetActive(leftLaserPointerShouldActive);
        }

        if (leftCurvePointer != null && leftCurvePointer.activeSelf != leftCurvePointerShouldActive)
        {
            leftCurvePointer.SetActive(leftCurvePointerShouldActive);
        }

        if (leftGrabber != null && leftGrabber.activeSelf != leftGraggerShouldActive)
        {
            leftGrabber.SetActive(leftGraggerShouldActive);
        }
    }
}