using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

// demonstrate of dragging things useing built in EventSystem handlers
public class Draggable : GrabbableBase<Draggable.Grabber>
    , IInitializePotentialDragHandler
    , IBeginDragHandler
    , IDragHandler
    , IEndDragHandler
{
    [Serializable]
    public class UnityEventDraggable : UnityEvent<Draggable> { }

    public class Grabber : IGrabber
    {
        private static ObjectPool<Grabber> m_pool;

        public static Grabber Get(PointerEventData eventData)
        {
            if (m_pool == null)
            {
                m_pool = new ObjectPool<Grabber>(() => new Grabber());
            }

            var grabber = m_pool.Get();
            grabber.eventData = eventData;
            return grabber;
        }

        public static void Release(Grabber grabber)
        {
            grabber.eventData = null;
            m_pool.Release(grabber);
        }

        public PointerEventData eventData { get; private set; }

        public RigidPose grabberOrigin
        {
            get
            {
                var cam = eventData.pointerPressRaycast.module.eventCamera;
                var ray = cam.ScreenPointToRay(eventData.position);
                return new RigidPose(ray.origin, Quaternion.LookRotation(ray.direction, cam.transform.up));
            }
        }

        public RigidPose grabOffset { get { return grabber2hit * hit2pivot; } set { } }

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

    private IndexedTable<PointerEventData, Grabber> m_eventGrabberSet;

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
    [FormerlySerializedAs("afterGrabbed")]
    [SerializeField]
    private UnityEventDraggable m_afterGrabbed = new UnityEventDraggable();
    [FormerlySerializedAs("beforeRelease")]
    [SerializeField]
    private UnityEventDraggable m_beforeRelease = new UnityEventDraggable();
    [FormerlySerializedAs("onDrop")]
    [SerializeField]
    private UnityEventDraggable m_onDrop = new UnityEventDraggable(); // change rigidbody drop velocity here
    [SerializeField]
    [FormerlySerializedAs("m_scrollDelta")]
    private float m_scrollingSpeed = 0.01f;

    public bool isDragged { get { return isGrabbed; } }

    public PointerEventData draggedEvent { get { return isGrabbed ? currentGrabber.eventData : null; } }

    public float initGrabDistance { get { return m_initGrabDistance; } set { m_initGrabDistance = value; } }

    public override float followingDuration { get { return m_followingDuration; } set { m_followingDuration = Mathf.Clamp(value, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION); } }

    public override bool overrideMaxAngularVelocity { get { return m_overrideMaxAngularVelocity; } set { m_overrideMaxAngularVelocity = value; } }

    public bool unblockableGrab { get { return m_unblockableGrab; } set { m_unblockableGrab = value; } }

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

    protected virtual void OnDisable()
    {
        ClearGrabbers(true);
        ClearEventGrabberSet();
    }

    private void ClearEventGrabberSet()
    {
        if (m_eventGrabberSet == null) { return; }

        for (int i = m_eventGrabberSet.Count - 1; i >= 0; --i)
        {
            Grabber.Release(m_eventGrabberSet.GetValueByIndex(i));
        }

        m_eventGrabberSet.Clear();
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        var hitDistance = 0f;

        switch (eventData.button)
        {
            case PointerEventData.InputButton.Middle:
            case PointerEventData.InputButton.Right:
                hitDistance = Mathf.Min(eventData.pointerPressRaycast.distance, m_initGrabDistance);
                break;
            case PointerEventData.InputButton.Left:
                hitDistance = eventData.pointerPressRaycast.distance;
                break;
            default:
                return;
        }

        var grabber = Grabber.Get(eventData);
        grabber.grabber2hit = new RigidPose(new Vector3(0f, 0f, hitDistance), Quaternion.identity);
        grabber.hit2pivot = RigidPose.FromToPose(grabber.grabberOrigin * grabber.grabber2hit, new RigidPose(transform));

        if (m_eventGrabberSet == null) { m_eventGrabberSet = new IndexedTable<PointerEventData, Grabber>(); }
        m_eventGrabberSet.Add(eventData, grabber);

        AddGrabber(grabber);
    }

    protected virtual void FixedUpdate()
    {
        if (isGrabbed && moveByVelocity)
        {
            OnGrabRigidbody();
        }
    }

    protected virtual void Update()
    {
        if (!isGrabbed) { return; }

        if (!moveByVelocity)
        {
            RecordLatestPosesForDrop(Time.time, 0.05f);
            OnGrabTransform();
        }

        var scrollDelta = currentGrabber.eventData.scrollDelta * m_scrollingSpeed;
        if (scrollDelta != Vector2.zero)
        {
            currentGrabber.hitDistance = Mathf.Max(0f, currentGrabber.hitDistance + scrollDelta.y);
        }
    }

    public virtual void OnDrag(PointerEventData eventData) { }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (m_eventGrabberSet == null) { return; }

        Grabber grabber;
        if (!m_eventGrabberSet.TryGetValue(eventData, out grabber)) { return; }

        RemoveGrabber(grabber);
        m_eventGrabberSet.Remove(eventData);
        Grabber.Release(grabber);
    }
}
