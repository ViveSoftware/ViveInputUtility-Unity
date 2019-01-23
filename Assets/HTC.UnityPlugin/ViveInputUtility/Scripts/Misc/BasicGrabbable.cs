//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using GrabberPool = HTC.UnityPlugin.Utility.ObjectPool<HTC.UnityPlugin.Vive.BasicGrabbable.Grabber>;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/Object Grabber/Basic Grabbable", 0)]
    public class BasicGrabbable : GrabbableBase<BasicGrabbable.Grabber>
        , IColliderEventDragStartHandler
        , IColliderEventDragFixedUpdateHandler
        , IColliderEventDragUpdateHandler
        , IColliderEventDragEndHandler
    {
        [Serializable]
        public class UnityEventGrabbable : UnityEvent<BasicGrabbable> { }

        public class Grabber : IGrabber
        {
            private static GrabberPool m_pool;

            public static Grabber Get(ColliderButtonEventData eventData)
            {
                if (m_pool == null)
                {
                    m_pool = new GrabberPool(() => new Grabber());
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

            public ColliderButtonEventData eventData { get; private set; }

            public RigidPose grabberOrigin
            {
                get
                {
                    return new RigidPose(eventData.eventCaster.transform);
                }
            }

            public RigidPose grabOffset { get; set; }
        }

        private IndexedTable<ColliderButtonEventData, Grabber> m_eventGrabberSet;

        public bool alignPosition;
        public bool alignRotation;
        public Vector3 alignPositionOffset;
        public Vector3 alignRotationOffset;

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
        private ColliderButtonEventData.InputButton m_grabButton = ColliderButtonEventData.InputButton.Trigger;
        [SerializeField]
        private bool m_allowMultipleGrabbers = true;
        [FormerlySerializedAs("afterGrabbed")]
        [SerializeField]
        private UnityEventGrabbable m_afterGrabbed = new UnityEventGrabbable();
        [FormerlySerializedAs("beforeRelease")]
        [SerializeField]
        private UnityEventGrabbable m_beforeRelease = new UnityEventGrabbable();
        [FormerlySerializedAs("onDrop")]
        [SerializeField]
        private UnityEventGrabbable m_onDrop = new UnityEventGrabbable(); // change rigidbody drop velocity here

        public override float followingDuration { get { return m_followingDuration; } set { m_followingDuration = Mathf.Clamp(value, MIN_FOLLOWING_DURATION, MAX_FOLLOWING_DURATION); } }

        public override bool overrideMaxAngularVelocity { get { return m_overrideMaxAngularVelocity; } set { m_overrideMaxAngularVelocity = value; } }

        public bool unblockableGrab { get { return m_unblockableGrab; } set { m_unblockableGrab = value; } }

        public UnityEventGrabbable afterGrabbed { get { return m_afterGrabbed; } }

        public UnityEventGrabbable beforeRelease { get { return m_beforeRelease; } }

        public UnityEventGrabbable onDrop { get { return m_onDrop; } }

        public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? currentGrabber.eventData : null; } }

        public ColliderButtonEventData.InputButton grabButton
        {
            get
            {
                return m_grabButton;
            }
            set
            {
                m_grabButton = value;
                MaterialChanger.SetAllChildrenHeighlightButton(gameObject, value);
            }
        }

        private bool moveByVelocity { get { return !unblockableGrab && grabRigidbody != null && !grabRigidbody.isKinematic; } }

        [Obsolete("Use grabRigidbody instead")]
        public Rigidbody rigid { get { return grabRigidbody; } set { grabRigidbody = value; } }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            MaterialChanger.SetAllChildrenHeighlightButton(gameObject, m_grabButton);
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            MaterialChanger.SetAllChildrenHeighlightButton(gameObject, m_grabButton);

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

        public virtual void OnColliderEventDragStart(ColliderButtonEventData eventData)
        {
            if (eventData.button != m_grabButton) { return; }

            if (!m_allowMultipleGrabbers)
            {
                ClearGrabbers(false);
                ClearEventGrabberSet();
            }

            var grabber = Grabber.Get(eventData);
            var offset = RigidPose.FromToPose(grabber.grabberOrigin, new RigidPose(transform));
            if (alignPosition) { offset.pos = alignPositionOffset; }
            if (alignRotation) { offset.rot = Quaternion.Euler(alignRotationOffset); }
            grabber.grabOffset = offset;

            if (m_eventGrabberSet == null) { m_eventGrabberSet = new IndexedTable<ColliderButtonEventData, Grabber>(); }
            m_eventGrabberSet.Add(eventData, grabber);

            AddGrabber(grabber);
        }

        public virtual void OnColliderEventDragFixedUpdate(ColliderButtonEventData eventData)
        {
            if (isGrabbed && moveByVelocity && currentGrabber.eventData == eventData)
            {
                OnGrabRigidbody();
            }
        }

        public virtual void OnColliderEventDragUpdate(ColliderButtonEventData eventData)
        {
            if (isGrabbed && !moveByVelocity && currentGrabber.eventData == eventData)
            {
                RecordLatestPosesForDrop(Time.time, 0.05f);
                OnGrabTransform();
            }
        }

        public virtual void OnColliderEventDragEnd(ColliderButtonEventData eventData)
        {
            if (m_eventGrabberSet == null) { return; }

            Grabber grabber;
            if (!m_eventGrabberSet.TryGetValue(eventData, out grabber)) { return; }

            RemoveGrabber(grabber);
            m_eventGrabberSet.Remove(eventData);
            Grabber.Release(grabber);
        }
    }
}