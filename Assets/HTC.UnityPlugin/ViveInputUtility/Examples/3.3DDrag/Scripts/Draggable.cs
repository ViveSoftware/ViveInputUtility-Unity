//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.LiteCoroutineSystem;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using GrabberPool = HTC.UnityPlugin.Utility.ObjectPool<Draggable.Grabber>;

// demonstrate of dragging things useing built in EventSystem handlers
public class Draggable : GrabbableBase<PointerEventData, Draggable.Grabber>
    , IInitializePotentialDragHandler
    , IBeginDragHandler
    , IDragHandler
    , IEndDragHandler
{
    [Serializable]
    public class UnityEventDraggable : UnityEvent<Draggable> { }

    public class Grabber : GrabberBase<PointerEventData>
    {
        private static GrabberPool m_pool;
        private PointerEventData m_eventData;

        public static Grabber Get(PointerEventData eventData)
        {
            if (m_pool == null)
            {
                m_pool = new GrabberPool(() => new Grabber());
            }

            var grabber = m_pool.Get();
            grabber.m_eventData = eventData;
            return grabber;
        }

        public static void Release(Grabber grabber)
        {
            grabber.m_eventData = null;
            m_pool.Release(grabber);
        }

        public override PointerEventData eventData { get { return m_eventData; } }

        public override RigidPose grabberOrigin
        {
            get
            {
                var cam = eventData.pointerPressRaycast.module.eventCamera;
                var ray = cam.ScreenPointToRay(eventData.position);
                return new RigidPose(ray.origin, Quaternion.LookRotation(ray.direction, cam.transform.up)) * grabber2hit;
            }
        }

        public override RigidPose grabOffset { get { return hit2pivot; } set { hit2pivot = value; } }

        public RigidPose grabber2hit { get; set; }

        public RigidPose hit2pivot { get; set; }

        public float hitDistance
        {
            get { return grabber2hit.pos.z; }
            set
            {
                var p = grabber2hit;
                p.pos.z = value;
                grabber2hit = p;
            }
        }
    }

    private LiteCoroutine m_updateCoroutine;
    private LiteCoroutine m_physicsCoroutine;

    [FormerlySerializedAs("initGrabDistance")]
    [SerializeField]
    private float m_initGrabDistance = 0.5f;
    [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
    [FormerlySerializedAs("followingDuration")]
    [SerializeField]
    private float m_followingDuration = DEFAULT_FOLLOWING_DURATION;
    [FormerlySerializedAs("overrideMaxAngularVelocity")]
    [SerializeField]
    private bool m_overrideMaxAngularVelocity = true;
    [FormerlySerializedAs("unblockableGrab")]
    [SerializeField]
    private bool m_unblockableGrab = true;
    [SerializeField]
    [FormerlySerializedAs("m_scrollDelta")]
    private float m_scrollingSpeed = 0.01f;
    [SerializeField]
    private float m_minStretchScale = 1f;
    [SerializeField]
    private float m_maxStretchScale = 1f;
    [FormerlySerializedAs("afterGrabbed")]
    [SerializeField]
    private UnityEventDraggable m_afterGrabbed = new UnityEventDraggable();
    [FormerlySerializedAs("beforeRelease")]
    [SerializeField]
    private UnityEventDraggable m_beforeRelease = new UnityEventDraggable();
    [FormerlySerializedAs("onDrop")]
    [SerializeField]
    private UnityEventDraggable m_onDrop = new UnityEventDraggable(); // change rigidbody drop velocity here

    public bool isDragged { get { return isGrabbed; } }

    public PointerEventData draggedEvent { get { return isGrabbed ? currentGrabber.eventData : null; } }

    public float initGrabDistance { get { return m_initGrabDistance; } set { m_initGrabDistance = value; } }

    public override float followingDuration { get { return m_followingDuration; } set { m_followingDuration = Mathf.Clamp(value, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION); } }

    public override bool overrideMaxAngularVelocity { get { return m_overrideMaxAngularVelocity; } set { m_overrideMaxAngularVelocity = value; } }

    public bool unblockableGrab { get { return m_unblockableGrab; } set { m_unblockableGrab = value; } }

    public override float minScaleOnStretch { get { return m_minStretchScale; } set { m_minStretchScale = value; } }

    public override float maxScaleOnStretch { get { return m_maxStretchScale; } set { m_maxStretchScale = value; } }

    public UnityEventDraggable afterGrabbed { get { return m_afterGrabbed; } }

    public UnityEventDraggable beforeRelease { get { return m_beforeRelease; } }

    public UnityEventDraggable onDrop { get { return m_onDrop; } }

    private bool moveByVelocity { get { return !unblockableGrab && grabRigidbody != null && !grabRigidbody.isKinematic; } }

    [Obsolete("Use grabRigidbody instead")]
    public Rigidbody rigid { get { return grabRigidbody; } set { grabRigidbody = value; } }

    public float scrollingSpeed { get { return m_scrollingSpeed; } set { m_scrollingSpeed = value; } }

    protected override void Awake()
    {
        base.Awake();

        afterGrabberGrabbed += () => m_afterGrabbed.Invoke(this);
        beforeGrabberReleased += () => m_beforeRelease.Invoke(this);
        onGrabberDrop += () => m_onDrop.Invoke(this);
    }

    protected virtual void OnDisable() { ForceRelease(); }

    protected override Grabber CreateGrabber(PointerEventData eventData)
    {
        var hitResult = eventData.pointerPressRaycast;
        float distance;
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Middle:
            case PointerEventData.InputButton.Right:
                distance = Mathf.Min(hitResult.distance, initGrabDistance);
                break;
            case PointerEventData.InputButton.Left:
                distance = hitResult.distance;
                break;
            default:
                return null;
        }

        var grabber = Grabber.Get(eventData);
        grabber.grabber2hit = new RigidPose(new Vector3(0f, 0f, distance), Quaternion.identity);
        grabber.hit2pivot = RigidPose.FromToPose(new RigidPose(hitResult.worldPosition, hitResult.module.eventCamera.transform.rotation), new RigidPose(transform));
        return grabber;
    }

    protected override void DestoryGrabber(Grabber grabber)
    {
        Grabber.Release(grabber);
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (AddGrabber(eventData))
        {
            if (m_updateCoroutine.IsNullOrDone())
            {
                LiteCoroutine.StartCoroutine(ref m_updateCoroutine, DragUpdate(), false);

                if (moveByVelocity)
                {
                    LiteCoroutine.StartCoroutine(ref m_physicsCoroutine, PhysicsGrabUpdate(), false);
                }
            }
        }
    }

    private static WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    private IEnumerator PhysicsGrabUpdate()
    {
        yield return waitForFixedUpdate;

        while (isGrabbed)
        {
            OnGrabRigidbody();

            yield return waitForFixedUpdate;
        }

        yield break;
    }

    private IEnumerator DragUpdate()
    {
        yield return null;

        while (isGrabbed)
        {
            for (int i = allGrabbers.Count - 1; i >= 0; --i)
            {
                var grabber = allGrabbers.GetValueByIndex(i);
                var scrollDelta = grabber.eventData.scrollDelta * m_scrollingSpeed;
                if (scrollDelta != Vector2.zero)
                {
                    grabber.hitDistance = Mathf.Max(0f, grabber.hitDistance + scrollDelta.y);
                }
            }

            if (!moveByVelocity)
            {
                RecordLatestPosesForDrop(Time.time, 0.05f);
                OnGrabTransform();
            }

            yield return null;
        }
    }

    public virtual void OnDrag(PointerEventData eventData) { }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        RemoveGrabber(eventData);
    }
}
