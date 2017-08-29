//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using Pose = HTC.UnityPlugin.PoseTracker.Pose;

public class BasicGrabbable : MonoBehaviour
    , IColliderEventDragStartHandler
    , IColliderEventDragFixedUpdateHandler
    , IColliderEventDragUpdateHandler
    , IColliderEventDragEndHandler
{
    [Serializable]
    public class UnityEventGrabbable : UnityEvent<BasicGrabbable> { }

    public const float MIN_FOLLOWING_DURATION = 0.02f;
    public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
    public const float MAX_FOLLOWING_DURATION = 0.5f;

    private OrderedIndexedTable<ColliderButtonEventData, Pose> eventList = new OrderedIndexedTable<ColliderButtonEventData, Pose>();

    public bool alignPosition;
    public bool alignRotation;
    public Vector3 alignPositionOffset;
    public Vector3 alignRotationOffset;
    [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
    public float followingDuration = DEFAULT_FOLLOWING_DURATION;
    public bool overrideMaxAngularVelocity = true;
    public bool unblockableGrab = true;

    [SerializeField]
    private ColliderButtonEventData.InputButton m_grabButton = ColliderButtonEventData.InputButton.Trigger;

    public UnityEventGrabbable afterGrabbed = new UnityEventGrabbable();
    public UnityEventGrabbable beforeRelease = new UnityEventGrabbable();
    public UnityEventGrabbable onDrop = new UnityEventGrabbable(); // change rigidbody drop velocity here

    private Pose m_prevPose = Pose.identity; // last frame world pose

    public ColliderButtonEventData.InputButton grabButton
    {
        get
        {
            return m_grabButton;
        }
        set
        {
            m_grabButton = value;
            // set all child MaterialChanger heighlightButton to value;
            var matChangers = ListPool<MaterialChanger>.Get();
            GetComponentsInChildren(matChangers);
            for (int i = matChangers.Count - 1; i >= 0; --i) { matChangers[i].heighlightButton = value; }
            ListPool<MaterialChanger>.Release(matChangers);
        }
    }

    public bool isGrabbed { get { return eventList.Count > 0; } }

    public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? eventList.GetLastKey() : null; } }

    // effected rigidbody
    public Rigidbody rigid { get; set; }

    private bool moveByVelocity { get { return !unblockableGrab && rigid != null && !rigid.isKinematic; } }

    private Pose GetEventPose(ColliderButtonEventData eventData)
    {
        var grabberTransform = eventData.eventCaster.transform;
        return new Pose(grabberTransform);
    }
#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        grabButton = m_grabButton;
    }
#endif

    protected virtual void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        grabButton = m_grabButton;
    }

    protected virtual void OnDisable()
    {
        if (isGrabbed && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.Clear();

        DoDrop();
    }

    public virtual void OnColliderEventDragStart(ColliderButtonEventData eventData)
    {
        if (eventData.button != m_grabButton) { return; }

        var casterPose = GetEventPose(eventData);
        var offsetPose = Pose.FromToPose(casterPose, new Pose(transform));

        if (alignPosition) { offsetPose.pos = Vector3.zero; }
        if (alignRotation) { offsetPose.rot = Quaternion.identity; }

        if (eventData != grabbedEvent && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.AddUniqueKey(eventData, offsetPose);

        if (afterGrabbed != null)
        {
            afterGrabbed.Invoke(this);
        }
    }

    public virtual void OnColliderEventDragFixedUpdate(ColliderButtonEventData eventData)
    {
        if (eventData != grabbedEvent) { return; }

        if (moveByVelocity)
        {
            // if rigidbody exists, follow eventData caster using physics
            var casterPose = GetEventPose(eventData);
            var offsetPose = eventList.GetLastValue();

            if (alignPosition) { offsetPose.pos = alignPositionOffset; }
            if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

            var targetPose = casterPose * offsetPose;
            Pose.SetRigidbodyVelocity(rigid, rigid.position, targetPose.pos, followingDuration);
            Pose.SetRigidbodyAngularVelocity(rigid, rigid.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
        }
    }

    public virtual void OnColliderEventDragUpdate(ColliderButtonEventData eventData)
    {
        if (eventData != grabbedEvent) { return; }

        if (!moveByVelocity)
        {
            // if rigidbody doesn't exist, just move to eventData caster's pose
            var casterPose = GetEventPose(eventData);
            var offsetPose = eventList.GetLastValue();

            if (alignPosition) { offsetPose.pos = alignPositionOffset; }
            if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

            m_prevPose = new Pose(transform);

            if (rigid != null)
            {
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            }

            var targetPose = casterPose * offsetPose;
            transform.position = targetPose.pos;
            transform.rotation = targetPose.rot;
        }
    }

    public virtual void OnColliderEventDragEnd(ColliderButtonEventData eventData)
    {
        var released = eventData == grabbedEvent;

        if (released && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.Remove(eventData);

        if (isGrabbed)
        {
            if (released && afterGrabbed != null)
            {
                afterGrabbed.Invoke(this);
            }
        }
        else
        {
            DoDrop();
        }
    }

    private void DoDrop()
    {
        if (!moveByVelocity && rigid != null && !rigid.isKinematic && m_prevPose != Pose.identity)
        {
            Pose.SetRigidbodyVelocity(rigid, m_prevPose.pos, transform.position, Time.deltaTime);
            Pose.SetRigidbodyAngularVelocity(rigid, m_prevPose.rot, transform.rotation, Time.deltaTime, overrideMaxAngularVelocity);

            m_prevPose = Pose.identity;
        }

        if (onDrop != null)
        {
            onDrop.Invoke(this);
        }
    }
}
