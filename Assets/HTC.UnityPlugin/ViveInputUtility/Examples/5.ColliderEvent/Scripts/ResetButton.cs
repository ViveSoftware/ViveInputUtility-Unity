using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

public class ResetButton : MonoBehaviour
    , IColliderEventPressEnterHandler
    , IColliderEventPressExitHandler
{
    public Transform[] effectTargets;
    public Transform buttonObject;
    public Vector3 buttonDownDisplacement;

    [SerializeField]
    private ColliderButtonEventData.InputButton m_activeButton = ColliderButtonEventData.InputButton.Trigger;

    private RigidPose[] storedPoses;

    private HashSet<ColliderButtonEventData> pressingEvents = new HashSet<ColliderButtonEventData>();

    public ColliderButtonEventData.InputButton activeButton { get { return m_activeButton; } set { m_activeButton = value; } }

    private void Start()
    {
        StorePoses();
    }

    public void OnColliderEventPressEnter(ColliderButtonEventData eventData)
    {
        if (eventData.button == m_activeButton && pressingEvents.Add(eventData) && pressingEvents.Count == 1)
        {
            buttonObject.localPosition += buttonDownDisplacement;
        }
    }

    public void OnColliderEventPressExit(ColliderButtonEventData eventData)
    {
        if (pressingEvents.Remove(eventData) && pressingEvents.Count == 0)
        {
            buttonObject.localPosition -= buttonDownDisplacement;

            // check if event caster is still hovering this object
            foreach (var c in eventData.eventCaster.enteredColliders)
            {
                if (c.transform.IsChildOf(transform))
                {
                    DoReset();
                    return;
                }
            }
        }
    }

    public void StorePoses()
    {
        if (effectTargets == null)
        {
            storedPoses = null;
            return;
        }

        if (storedPoses == null || storedPoses.Length != effectTargets.Length)
        {
            storedPoses = new RigidPose[effectTargets.Length];
        }

        for (int i = 0; i < effectTargets.Length; ++i)
        {
            storedPoses[i] = new RigidPose(effectTargets[i]);
        }
    }

    public void DoReset()
    {
        if (effectTargets == null) { return; }

        for (int i = 0; i < effectTargets.Length; ++i)
        {
            var rigid = effectTargets[i].GetComponent<Rigidbody>();
            if (rigid != null)
            {
                rigid.MovePosition(storedPoses[i].pos);
                rigid.MoveRotation(storedPoses[i].rot);
                rigid.velocity = Vector3.zero;
                //rigid.angularVelocity = Vector3.zero;
            }
            else
            {
                effectTargets[i].position = storedPoses[i].pos;
                effectTargets[i].rotation = storedPoses[i].rot;
            }
        }
    }
}
