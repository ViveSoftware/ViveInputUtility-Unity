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
        [Serializable]
        public class UnityEventGrabbable : UnityEvent<StickyGrabbable> { }

        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        private OrderedIndexedTable<ColliderButtonEventData, RigidPose> eventList = new OrderedIndexedTable<ColliderButtonEventData, RigidPose>();
        private ColliderButtonEventData m_bannedEventData;

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

        public bool isGrabbed { get { return eventList.Count > 0; } }

        public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? eventList.GetLastKey() : null; } }

        // effected rigidbody
        public Rigidbody rigid { get; set; }

        private bool moveByVelocity { get { return !unblockableGrab && rigid != null && !rigid.isKinematic; } }

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
            if (eventData.button != m_grabButton || eventList.ContainsKey(eventData) || eventData == m_bannedEventData) { return; }

            var casterPose = GetEventPose(eventData);
            var offsetPose = RigidPose.FromToPose(casterPose, new RigidPose(transform));

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
            if (isGrabbed && moveByVelocity)
            {
                // if rigidbody exists, follow eventData caster using physics
                var casterPose = GetEventPose(grabbedEvent);
                var offsetPose = eventList.GetLastValue();

                if (alignPosition) { offsetPose.pos = alignPositionOffset; }
                if (alignRotation) { offsetPose.rot = Quaternion.Euler(alignRotationOffset); }

                var targetPose = casterPose * offsetPose;
                RigidPose.SetRigidbodyVelocity(rigid, rigid.position, targetPose.pos, followingDuration);
                RigidPose.SetRigidbodyAngularVelocity(rigid, rigid.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
            }
        }

        protected virtual void Update()
        {
            if (isGrabbed && !moveByVelocity)
            {
                // if rigidbody doesn't exist, just move to eventData caster's pose
                var casterPose = GetEventPose(grabbedEvent);
                var offsetPose = eventList.GetLastValue();

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

            // reset banned event data
            m_bannedEventData = null;

            // toggle grab button to release
            if (m_toggleToRelease && isGrabbed)
            {
                var released = grabbedEvent.GetPressDown();
                if (released)
                {
                    m_bannedEventData = grabbedEvent; // ban this press down event so that it won't trigger another grab in OnColliderEventPressDown

                    if (beforeRelease != null)
                    {
                        beforeRelease.Invoke(this);
                    }
                }

                eventList.RemoveAll((obj) => obj.Key.GetPressDown());

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
            if (!moveByVelocity && rigid != null && !rigid.isKinematic && m_prevPose != RigidPose.identity)
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