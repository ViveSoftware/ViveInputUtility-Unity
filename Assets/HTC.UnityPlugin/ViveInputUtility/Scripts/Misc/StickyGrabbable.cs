//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/Object Grabber/Sticky Grabbable", 1)]
    public class StickyGrabbable : MonoBehaviour, IColliderEventPressDownHandler
    {
        private enum EventGrabState
        {
            JustGrabbed,
            Grabbing,
            JustReleased,
        }

        private struct GrabData
        {
            public RigidPose offset;
            public EventGrabState grabState;
        }

        [Serializable]
        public class UnityEventGrabbable : UnityEvent<StickyGrabbable> { }

        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        private OrderedIndexedTable<ColliderButtonEventData, GrabData> m_eventList;
        private int m_toggleReleaseCheckedFrame = -1;

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
        [SerializeField]
        private bool m_toggleToRelease = true;
        [SerializeField]
        private bool m_multipleGrabbers = false;

        public UnityEventGrabbable afterGrabbed = new UnityEventGrabbable();
        public UnityEventGrabbable beforeRelease = new UnityEventGrabbable();
        public UnityEventGrabbable onDrop = new UnityEventGrabbable(); // change rigidbody drop velocity here

        private RigidPose m_prevPose = RigidPose.identity; // last frame world pose

        public ColliderButtonEventData.InputButton grabButton { get { return m_grabButton; } set { m_grabButton = value; } }

        public bool isGrabbed
        {
            get
            {
                if (m_eventList == null) { return false; }
                if (m_eventList.Count == 0) { return false; }
                return m_eventList.GetLastValue().grabState != EventGrabState.JustReleased;
            }
        }

        public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? eventList.GetLastKey() : null; } }

        private GrabData grabbedData { get { return isGrabbed ? eventList.GetLastValue() : default(GrabData); } }

        // effected rigidbody
        public Rigidbody rigid { get; set; }

        private OrderedIndexedTable<ColliderButtonEventData, GrabData> eventList { get { return m_eventList ?? (m_eventList = new OrderedIndexedTable<ColliderButtonEventData, GrabData>()); } }

        private bool moveByVelocity { get { return !unblockableGrab && rigid != null && !rigid.isKinematic; } }

        private bool isToggleReleaseCheckedThisFrame { get { return !m_toggleToRelease || m_toggleReleaseCheckedFrame == Time.frameCount; } }

        private RigidPose GetEventPose(ColliderButtonEventData eventData)
        {
            var grabberTransform = eventData.eventCaster.transform;
            return new RigidPose(grabberTransform);
        }

        protected virtual void Awake()
        {
            rigid = GetComponent<Rigidbody>();
        }

        protected virtual void OnDisable()
        {
            Release();
        }

        public virtual void OnColliderEventPressDown(ColliderButtonEventData eventData)
        {
            GrabData grabData;

            if (eventData.button != m_grabButton) { return; }

            if (eventList.TryGetValue(eventData, out grabData))
            {
                switch (grabData.grabState)
                {
                    case EventGrabState.JustReleased:
                        eventList.Remove(eventData);
                        if (m_toggleReleaseCheckedFrame == Time.frameCount)
                        {
                            // skip when this event just released at the same frame
                            return;
                        }
                        break;
                    case EventGrabState.JustGrabbed:
                    case EventGrabState.Grabbing:
                    default:
                        return;
                }
            }

            if (!m_multipleGrabbers)
            {
                Release();
            }

            var casterPose = GetEventPose(eventData);
            var offsetPose = RigidPose.FromToPose(casterPose, new RigidPose(transform));

            if (alignPosition) { offsetPose.pos = Vector3.zero; }
            if (alignRotation) { offsetPose.rot = Quaternion.identity; }

            grabData = new GrabData()
            {
                offset = offsetPose,
                grabState = isToggleReleaseCheckedThisFrame ? EventGrabState.Grabbing : EventGrabState.JustGrabbed,
            };

            eventList.AddUniqueKey(eventData, grabData);

            if (afterGrabbed != null)
            {
                afterGrabbed.Invoke(this);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (isGrabbed && moveByVelocity)
            {
                // if rigidbody exists, follow eventData caster using physics
                var casterPose = GetEventPose(grabbedEvent);
                var offsetPose = grabbedData.offset;

                if (alignPosition) { offsetPose.pos = alignPositionOffset; }
                if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

                var targetPose = casterPose * offsetPose;
                RigidPose.SetRigidbodyVelocity(rigid, rigid.position, targetPose.pos, followingDuration);
                RigidPose.SetRigidbodyAngularVelocity(rigid, rigid.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
            }
        }

        protected virtual void Update()
        {
            if (!isGrabbed) { return; }

            if (!moveByVelocity)
            {
                // if rigidbody doesn't exist, just move to eventData caster's pose
                var casterPose = GetEventPose(grabbedEvent);
                var offsetPose = grabbedData.offset;

                if (alignPosition) { offsetPose.pos = alignPositionOffset; }
                if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

                m_prevPose = new RigidPose(transform);

                if (rigid != null)
                {
                    rigid.velocity = Vector3.zero;
                    rigid.angularVelocity = Vector3.zero;
                }

                var targetPose = casterPose * offsetPose;
                transform.position = targetPose.pos;
                transform.rotation = targetPose.rot;
            }

            // check toggle release
            if (m_toggleToRelease)
            {
                m_toggleReleaseCheckedFrame = Time.frameCount;
                var grabberChanged = false;

                for (int iCurrent = m_eventList.Count - 1, iEnd = 0; iCurrent >= iEnd; --iCurrent)
                {
                    var eventData = m_eventList.GetKeyByIndex(iCurrent);
                    var grabData = m_eventList.GetValueByIndex(iCurrent);
                    switch (grabData.grabState)
                    {
                        case EventGrabState.JustGrabbed:
                            grabData.grabState = EventGrabState.Grabbing;
                            m_eventList.SetValueByIndex(iCurrent, grabData);
                            break;
                        case EventGrabState.Grabbing:
                            if (eventData.GetPressDown())
                            {
                                if (!grabberChanged && iCurrent == m_eventList.Count - 1)
                                {
                                    grabberChanged = true;
                                }

                                m_eventList.RemoveAt(iCurrent);
                                grabData.grabState = EventGrabState.JustReleased;
                                m_eventList.Insert(0, eventData, grabData);

                                ++iEnd;
                                ++iCurrent;
                            }
                            break;
                        case EventGrabState.JustReleased:
                            m_eventList.Remove(eventData);
                            break;
                    }
                }

                if (grabberChanged)
                {
                    if (beforeRelease != null)
                    {
                        beforeRelease.Invoke(this);
                    }
                }

                if (isGrabbed)
                {
                    if (grabberChanged)
                    {
                        if (afterGrabbed != null)
                        {
                            afterGrabbed.Invoke(this);
                        }
                    }
                }
                else
                {
                    DoDrop();
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

            DoDrop();
        }

        private void DoDrop()
        {
            if (isActiveAndEnabled && !moveByVelocity && rigid != null && !rigid.isKinematic && m_prevPose != RigidPose.identity)
            {
                RigidPose.SetRigidbodyVelocity(rigid, m_prevPose.pos, transform.position, Time.deltaTime);
                RigidPose.SetRigidbodyAngularVelocity(rigid, m_prevPose.rot, transform.rotation, Time.deltaTime, overrideMaxAngularVelocity);

                m_prevPose = RigidPose.identity;
            }

            if (onDrop != null)
            {
                onDrop.Invoke(this);
            }
        }
    }
}