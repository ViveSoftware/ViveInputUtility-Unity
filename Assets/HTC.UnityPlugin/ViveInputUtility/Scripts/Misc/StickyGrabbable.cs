//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

public class StickyGrabbable : MonoBehaviour, IColliderEventPressDownHandler
{
    [Serializable]
    public class UnityEventGrabbable : UnityEvent<StickyGrabbable> { }

    public const float MIN_FOLLOWING_DURATION = 0.02f;
    public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
    public const float MAX_FOLLOWING_DURATION = 0.5f;

    private OrderedIndexedTable<ColliderButtonEventData, Pose> eventList = new OrderedIndexedTable<ColliderButtonEventData, Pose>();
    private ColliderButtonEventData m_bannedEventData;

    public bool alignPosition;
    public bool alignRotation;
    public Vector3 alignPositionOffset;
    public Vector3 alignRotationOffset;
    [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
    public float followingDuration = DEFAULT_FOLLOWING_DURATION;
    public bool overrideMaxAngularVelocity = true;

    [SerializeField]
    private ColliderButtonEventData.InputButton m_grabButton = ColliderButtonEventData.InputButton.Trigger;
    [SerializeField]
    private bool m_toggleToRelease = true;
    [SerializeField]
    private bool m_multipleGrabbers = false;

    public UnityEventGrabbable afterGrabbed = new UnityEventGrabbable();
    public UnityEventGrabbable beforeRelease = new UnityEventGrabbable();

    public ColliderButtonEventData.InputButton grabButton { get { return m_grabButton; } set { m_grabButton = value; } }

    public bool isGrabbed { get { return eventList.Count > 0; } }

    public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? eventList.GetLastKey() : null; } }

    private Pose GetEventPose(ColliderButtonEventData eventData)
    {
        var grabberTransform = eventData.eventCaster.transform;
        return new Pose(grabberTransform);
    }

    protected virtual void OnDisable()
    {
        Release();

        var rigid = GetComponent<Rigidbody>();
        if (rigid != null)
        {
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    public virtual void OnColliderEventPressDown(ColliderButtonEventData eventData)
    {
        if (eventData.button != m_grabButton || eventList.ContainsKey(eventData) || eventData == m_bannedEventData) { return; }

        var casterPose = GetEventPose(eventData);
        var offsetPose = Pose.FromToPose(casterPose, new Pose(transform));

        if (alignPosition) { offsetPose.pos = Vector3.zero; }
        if (alignRotation) { offsetPose.rot = Quaternion.identity; }

        if (!m_multipleGrabbers && eventList.Count > 0)
        {
            Release();
        }

        eventList.AddUniqueKey(eventData, offsetPose);

        if (afterGrabbed != null)
        {
            afterGrabbed.Invoke(this);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isGrabbed) { return; }

        var rigid = GetComponent<Rigidbody>();
        if (rigid != null)
        {
            // if rigidbody exists, follow eventData caster using physics
            var casterPose = GetEventPose(grabbedEvent);
            var offsetPose = eventList.GetLastValue();

            if (alignPosition) { offsetPose.pos = alignPositionOffset; }
            if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

            var targetPose = casterPose * offsetPose;
            Pose.SetRigidbodyVelocity(rigid, targetPose.pos, followingDuration);
            Pose.SetRigidbodyAngularVelocity(rigid, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
        }
    }

    protected virtual void Update()
    {
        if (isGrabbed && GetComponent<Rigidbody>() == null)
        {
            // if rigidbody doesn't exist, just move to eventData caster's pose
            var casterPose = GetEventPose(grabbedEvent);
            var offsetPose = eventList.GetLastValue();

            if (alignPosition) { offsetPose.pos = alignPositionOffset; }
            if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

            var targetPose = casterPose * offsetPose;

            transform.position = targetPose.pos;
            transform.rotation = targetPose.rot;
        }

        // resolve banned event data
        m_bannedEventData = null;

        // toggle grab button to release
        if (m_toggleToRelease && isGrabbed)
        {
            var released = grabbedEvent.GetPressDown();
            if (released)
            {
                m_bannedEventData = grabbedEvent;
                if (beforeRelease != null)
                {
                    beforeRelease.Invoke(this);
                }
            }

            eventList.RemoveAll((obj) => obj.Key.GetPressDown());

            if (released && isGrabbed && afterGrabbed != null)
            {
                afterGrabbed.Invoke(this);
            }
        }
    }

    public void Release()
    {
        if (!isGrabbed) { return; }

        if (beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.Clear();
    }
}
