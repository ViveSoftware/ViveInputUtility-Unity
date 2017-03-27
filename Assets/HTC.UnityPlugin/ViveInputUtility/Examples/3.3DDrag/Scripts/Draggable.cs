using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

// demonstrate of dragging things useing built in EventSystem handlers
public class Draggable : MonoBehaviour
    , IInitializePotentialDragHandler
    , IBeginDragHandler
    , IDragHandler
    , IEndDragHandler
{
    public const float MIN_FOLLOWING_DURATION = 0.02f;
    public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
    public const float MAX_FOLLOWING_DURATION = 0.5f;

    private OrderedIndexedTable<PointerEventData, Pose> eventList = new OrderedIndexedTable<PointerEventData, Pose>();

    public float initGrabDistance = 0.5f;
    [Range(MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION)]
    public float followingDuration = DEFAULT_FOLLOWING_DURATION;
    public bool overrideMaxAngularVelocity = true;

    private Pose GetEventPose(PointerEventData eventData)
    {
        var cam = eventData.pointerPressRaycast.module.eventCamera;
        var ray = cam.ScreenPointToRay(eventData.position);
        return new Pose(ray.origin, Quaternion.LookRotation(ray.direction, cam.transform.up));
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
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

        eventList.AddUniqueKey(eventData, offsetPose);
    }

    private void FixedUpdate()
    {
        PointerEventData eventData;
        if (!eventList.TryGetLastKey(out eventData)) { return; }

        var rigid = GetComponent<Rigidbody>();
        if (ReferenceEquals(rigid, null)) { return; }

        // if rigidbody exists, follow eventData caster using physics
        var casterPose = GetEventPose(eventData);
        var offsetPose = eventList.GetLastValue();
        var targetPose = casterPose * offsetPose;

        // applying velocity
        var diffPos = targetPose.pos - rigid.position;
        if (Mathf.Approximately(diffPos.sqrMagnitude, 0f))
        {
            rigid.velocity = Vector3.zero;
        }
        else
        {
            rigid.velocity = diffPos / Mathf.Clamp(followingDuration, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION);
        }

        // applying angular velocity
        float angle;
        Vector3 axis;
        (targetPose.rot * Quaternion.Inverse(rigid.rotation)).ToAngleAxis(out angle, out axis);
        while (angle > 360f) { angle -= 360f; }

        if (Mathf.Approximately(angle, 0f) || float.IsNaN(axis.x))
        {
            rigid.angularVelocity = Vector3.zero;
        }
        else
        {
            angle *= Mathf.Deg2Rad / Mathf.Clamp(followingDuration, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION); // convert to radius speed
            if (overrideMaxAngularVelocity && rigid.maxAngularVelocity < angle) { rigid.maxAngularVelocity = angle; }
            rigid.angularVelocity = axis * angle;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventList.Count == 0 || !ReferenceEquals(eventData, eventList.GetLastKey())) { return; }

        if (!ReferenceEquals(GetComponent<Rigidbody>(), null)) { return; }

        // if rigidbody doen't exist, just move transform to eventData caster's pose
        var casterPose = GetEventPose(eventData);
        var offsetPose = eventList.GetLastValue();
        var targetPose = casterPose * offsetPose;

        transform.position = targetPose.pos;
        transform.rotation = targetPose.rot;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        eventList.Remove(eventData);
    }
}
