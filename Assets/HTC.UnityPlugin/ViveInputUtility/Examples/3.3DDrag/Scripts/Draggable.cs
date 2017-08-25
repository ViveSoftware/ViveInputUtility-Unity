using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Pose = HTC.UnityPlugin.PoseTracker.Pose;

// demonstrate of dragging things useing built in EventSystem handlers
public class Draggable : MonoBehaviour
    , IInitializePotentialDragHandler
    , IBeginDragHandler
    , IDragHandler
    , IEndDragHandler
{
    [Serializable]
    public class UnityEventDraggable : UnityEvent<Draggable> { }

    public const float MIN_FOLLOWING_DURATION = 0.02f;
    public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
    public const float MAX_FOLLOWING_DURATION = 0.5f;

    private OrderedIndexedTable<PointerEventData, Pose> eventList = new OrderedIndexedTable<PointerEventData, Pose>();

    public float initGrabDistance = 0.5f;
    [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
    public float followingDuration = DEFAULT_FOLLOWING_DURATION;
    public bool overrideMaxAngularVelocity = true;
    public bool unblockableGrab = true;

    public UnityEventDraggable afterDragged = new UnityEventDraggable();
    public UnityEventDraggable beforeRelease = new UnityEventDraggable();
    public UnityEventDraggable onDrop = new UnityEventDraggable(); // change rigidbody drop velocity here

    private Pose m_prevPose = Pose.identity; // last frame world pose

    public bool isDragged { get { return eventList.Count > 0; } }

    public PointerEventData draggedEvent { get { return isDragged ? eventList.GetLastKey() : null; } }

    // effected rigidbody
    public Rigidbody rigid { get; set; }

    private bool moveByVelocity { get { return !unblockableGrab && rigid != null && !rigid.isKinematic; } }

    private Pose GetEventPose(PointerEventData eventData)
    {
        var cam = eventData.pointerPressRaycast.module.eventCamera;
        var ray = cam.ScreenPointToRay(eventData.position);
        return new Pose(ray.origin, Quaternion.LookRotation(ray.direction, cam.transform.up));
    }

    protected virtual void Awake()
    {
        rigid = GetComponent<Rigidbody>();
    }

    protected virtual void OnDisable()
    {
        if (isDragged && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.Clear();

        DoDrop();
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        var casterPose = GetEventPose(eventData);
        var offsetPose = new Pose();
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Middle:
            case PointerEventData.InputButton.Right:
                {
                    var hitResult = eventData.pointerPressRaycast;
                    var hitPose = new Pose(hitResult.worldPosition, casterPose.rot);

                    var caster2hit = new Pose(Vector3.forward * Mathf.Min(hitResult.distance, initGrabDistance), Quaternion.identity);
                    var hit2center = Pose.FromToPose(hitPose, new Pose(transform));

                    offsetPose = caster2hit * hit2center;
                    break;
                }
            case PointerEventData.InputButton.Left:
            default:
                {
                    offsetPose = Pose.FromToPose(casterPose, new Pose(transform));
                    break;
                }
        }

        if (eventData != draggedEvent && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.AddUniqueKey(eventData, offsetPose);

        if (afterDragged != null)
        {
            afterDragged.Invoke(this);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isDragged) { return; }

        if (moveByVelocity)
        {
            // if rigidbody exists, follow eventData caster using physics
            var casterPose = GetEventPose(draggedEvent);
            var offsetPose = eventList.GetLastValue();

            var targetPose = casterPose * offsetPose;
            Pose.SetRigidbodyVelocity(rigid, rigid.position, targetPose.pos, followingDuration);
            Pose.SetRigidbodyAngularVelocity(rigid, rigid.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (eventData != draggedEvent) { return; }

        if (!moveByVelocity)
        {
            // if rigidbody doen't exist, just move transform to eventData caster's pose
            var casterPose = GetEventPose(eventData);
            var offsetPose = eventList.GetLastValue();

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

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        var released = eventData == draggedEvent;
        if (released && beforeRelease != null)
        {
            beforeRelease.Invoke(this);
        }

        eventList.Remove(eventData);

        if (isDragged)
        {
            if (released && afterDragged != null)
            {
                afterDragged.Invoke(this);
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
