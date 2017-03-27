using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;
using System.Collections.Generic;
using UnityEngine;

public class ShowMenuOnClick : MonoBehaviour
    , IColliderEventClickHandler
    , IColliderEventPressEnterHandler
    , IColliderEventPressExitHandler
{
    public GameObject effectMenu;
    public ControllerManagerSample controllerManager;
    [SerializeField]
    private ControllerButton m_activeButton = ControllerButton.Trigger;

    public Transform buttonObject;
    public Vector3 buttonDownDisplacement;

    private Vector3 buttonOriginPosition;
    private bool menuVisible = false;

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
        buttonOriginPosition = buttonObject.position;
        SetMenuVisible(menuVisible);
    }

#if UNITY_EDITOR

    protected virtual void OnValidate()
    {
        activeButton = m_activeButton;
    }

#endif

    public void SetMenuVisible(bool value)
    {
        menuVisible = value;
        effectMenu.gameObject.SetActive(value);
        controllerManager.rightLaserPointerActive = value;
        controllerManager.leftLaserPointerActive = value;
        controllerManager.UpdateActivity();
    }

    public void OnColliderEventClick(ColliderButtonEventData eventData)
    {
        if (pressingEvents.Contains(eventData) && pressingEvents.Count == 1)
        {
            SetMenuVisible(!menuVisible);
        }
    }

    public void OnColliderEventPressEnter(ColliderButtonEventData eventData)
    {
        if (eventData.IsViveButton(m_activeButton) && eventData.clickingHandlers.Contains(gameObject) && pressingEvents.Add(eventData) && pressingEvents.Count == 1)
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