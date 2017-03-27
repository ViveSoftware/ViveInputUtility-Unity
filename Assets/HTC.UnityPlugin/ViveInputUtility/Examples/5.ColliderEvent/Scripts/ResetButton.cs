using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using UnityEngine;

public class ResetButton : MonoBehaviour
    , IColliderEventPressUpHandler
    , IColliderEventPressEnterHandler
    , IColliderEventPressExitHandler
{
    public Transform effectTarget;
    public Transform buttonObject;
    public Vector3 buttonDownDisplacement;

    [SerializeField]
    private ControllerButton m_activeButton = ControllerButton.Trigger;

    private Vector3 resetPosition;
    private Quaternion resetRotation;

    private Vector3 buttonOriginPosition;

    private HashSet<ColliderButtonEventData> pressingEvents = new HashSet<ColliderButtonEventData>();

    public ControllerButton activeButton
    {
        get
        {
            return m_activeButton;
        }
        set
        {
            m_activeButton = value;
            // set all child MaterialChanger heighlightButton to value;
            var changers = ListPool<MaterialChanger>.Get();
            GetComponentsInChildren(changers);
            for (int i = changers.Count - 1; i >= 0; --i) { changers[i].heighlightButton = value; }
            ListPool<MaterialChanger>.Release(changers);
        }
    }

    private void Start()
    {
        resetPosition = effectTarget.position;
        resetRotation = effectTarget.rotation;

        buttonOriginPosition = buttonObject.position;
    }
#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        activeButton = m_activeButton;
    }
#endif
    public void OnColliderEventPressUp(ColliderButtonEventData eventData)
    {
        if (pressingEvents.Contains(eventData) && pressingEvents.Count == 1)
        {
            var rigid = effectTarget.GetComponent<Rigidbody>();
            if (rigid != null)
            {
                rigid.MovePosition(resetPosition);
                rigid.MoveRotation(resetRotation);
                rigid.velocity = Vector3.zero;
                //rigid.angularVelocity = Vector3.zero;
            }
            else
            {
                effectTarget.position = resetPosition;
                effectTarget.rotation = resetRotation;
            }
        }
    }

    public void OnColliderEventPressEnter(ColliderButtonEventData eventData)
    {
        if (eventData.IsViveButton(m_activeButton) && pressingEvents.Add(eventData) && pressingEvents.Count == 1)
        {
            buttonObject.position = buttonOriginPosition + buttonDownDisplacement;
        }
    }

    public void OnColliderEventPressExit(ColliderButtonEventData eventData)
    {
        if (pressingEvents.Remove(eventData) && pressingEvents.Count == 0)
        {
            buttonObject.position = buttonOriginPosition;
        }
    }
}
