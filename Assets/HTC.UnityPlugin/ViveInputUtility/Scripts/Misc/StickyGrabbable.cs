//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.LiteCoroutineSystem;
using HTC.UnityPlugin.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using GrabberPool = HTC.UnityPlugin.Utility.ObjectPool<HTC.UnityPlugin.Vive.StickyGrabbable.Grabber>;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("VIU/Object Grabber/Sticky Grabbable", 1)]
    public class StickyGrabbable : GrabbableBase<ColliderButtonEventData, StickyGrabbable.Grabber>
        , IColliderEventPressDownHandler
    {
        public class Grabber : GrabberBase<ColliderButtonEventData>
        {
            private static GrabberPool m_pool;
            private ColliderButtonEventData m_eventData;

            public static Grabber Get(ColliderButtonEventData eventData)
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

            public override ColliderButtonEventData eventData { get { return m_eventData; } }

            public override RigidPose grabberOrigin { get { return new RigidPose(eventData.eventCaster.transform); } }

            public override RigidPose grabOffset { get; set; }

            [Obsolete("This property nolonger used")]
            public int grabFrame { get; set; }
            [Obsolete("This property nolonger used")]
            public bool redeayForRelease { get; set; }
        }

        [Serializable]
        public class UnityEventGrabbable : UnityEvent<StickyGrabbable> { }

        private struct ButtonProcessedState
        {
            public bool isGrabbing;
            public int processedFrame;
        }

        // NOTE:
        // We can't make sure the excution order of OnColliderEventPressDown() and Update()
        // Hence log grabFrame to avoid redundant release in Update()
        // and redeayForRelease flag(remove grabber from m_eventGrabberSet one frame later) to avoid redundant grabbing in OnColliderEventPressDown()
        private IndexedTable<ColliderButtonEventData, ButtonProcessedState> m_buttonProcessedFrame = new IndexedTable<ColliderButtonEventData, ButtonProcessedState>();
        private LiteCoroutine m_updateCoroutine;
        private LiteCoroutine m_physicsCoroutine;

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
        [FlagsFromEnum(typeof(ControllerButton))]
        private ulong m_primaryGrabButton = 0ul;
        [SerializeField]
        [FlagsFromEnum(typeof(ColliderButtonEventData.InputButton))]
        private uint m_secondaryGrabButton = 1u << (int)ColliderButtonEventData.InputButton.Trigger;
        [SerializeField]
        [HideInInspector]
        private ColliderButtonEventData.InputButton m_grabButton = ColliderButtonEventData.InputButton.Trigger;
        [SerializeField]
        private bool m_toggleToRelease = true;
        [FormerlySerializedAs("m_multipleGrabbers")]
        [SerializeField]
        private bool m_allowMultipleGrabbers = false;
        [SerializeField]
        private bool m_grabOnLastEntered = false;
        [SerializeField]
        private float m_minStretchScale = 1f;
        [SerializeField]
        private float m_maxStretchScale = 1f;
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

        public override bool overrideMaxAngularVelocity { get { return m_overrideMaxAngularVelocity; } set { overrideMaxAngularVelocity = value; } }

        public bool unblockableGrab { get { return m_unblockableGrab; } set { m_unblockableGrab = value; } }

        public bool toggleToRelease { get { return m_toggleToRelease; } set { m_toggleToRelease = value; } }

        public bool grabOnLastEntered { get { return m_grabOnLastEntered; } set { m_grabOnLastEntered = value; } }

        public override float minScaleOnStretch { get { return m_minStretchScale; } set { m_minStretchScale = value; } }

        public override float maxScaleOnStretch { get { return m_maxStretchScale; } set { m_maxStretchScale = value; } }

        public UnityEventGrabbable afterGrabbed { get { return m_afterGrabbed; } }

        public UnityEventGrabbable beforeRelease { get { return m_beforeRelease; } }

        public UnityEventGrabbable onDrop { get { return m_onDrop; } }

        public ColliderButtonEventData grabbedEvent { get { return isGrabbed ? currentGrabber.eventData : null; } }

        public ulong primaryGrabButton { get { return m_primaryGrabButton; } set { m_primaryGrabButton = value; } }

        public uint secondaryGrabButton { get { return m_secondaryGrabButton; } set { m_secondaryGrabButton = value; } }

        public bool allowMultipleGrabbers { get { return m_allowMultipleGrabbers; } set { allowMultipleGrabbers = value; } }

        [Obsolete("Use IsSecondaryGrabButtonOn and SetSecondaryGrabButton instead")]
        public ColliderButtonEventData.InputButton grabButton
        {
            get
            {
                for (uint btn = 0u, btns = m_secondaryGrabButton; btns > 0u; btns >>= 1, ++btn)
                {
                    if ((btns & 1u) > 0u) { return (ColliderButtonEventData.InputButton)btn; }
                }
                return ColliderButtonEventData.InputButton.None;
            }
            set { m_secondaryGrabButton = 1u << (int)value; }
        }

        private bool moveByVelocity { get { return !unblockableGrab && grabRigidbody != null && !grabRigidbody.isKinematic; } }

        public bool IsPrimeryGrabButtonOn(ControllerButton btn) { return EnumUtils.GetFlag(m_primaryGrabButton, (int)btn); }

        public void SetPrimeryGrabButton(ControllerButton btn, bool isOn = true) { EnumUtils.SetFlag(ref m_primaryGrabButton, (int)btn, isOn); }

        public void ClearPrimeryGrabButton() { m_primaryGrabButton = 0ul; }

        public bool IsSecondaryGrabButtonOn(ColliderButtonEventData.InputButton btn) { return EnumUtils.GetFlag(m_secondaryGrabButton, (int)btn); }

        public void SetSecondaryGrabButton(ColliderButtonEventData.InputButton btn, bool isOn = true) { EnumUtils.SetFlag(ref m_secondaryGrabButton, (int)btn, isOn); }

        public void ClearSecondaryGrabButton() { m_secondaryGrabButton = 0u; }

        [Obsolete("Use grabRigidbody instead")]
        public Rigidbody rigid { get { return grabRigidbody; } set { grabRigidbody = value; } }

#if UNITY_EDITOR
        protected virtual void OnValidate() { RestoreObsoleteGrabButton(); }

        protected virtual void Reset() { m_grabOnLastEntered = true; }
#endif
        private void RestoreObsoleteGrabButton()
        {
            if (m_grabButton == ColliderButtonEventData.InputButton.Trigger) { return; }
            ClearSecondaryGrabButton();
            SetSecondaryGrabButton(m_grabButton, true);
            m_grabButton = ColliderButtonEventData.InputButton.Trigger;
        }

        protected override void Awake()
        {
            base.Awake();

            RestoreObsoleteGrabButton();

            afterGrabberGrabbed += () => m_afterGrabbed.Invoke(this);
            beforeGrabberReleased += () => m_beforeRelease.Invoke(this);
            onGrabberDrop += () => m_onDrop.Invoke(this);
        }

        protected virtual void OnDisable() { ForceRelease(); }

        protected override Grabber CreateGrabber(ColliderButtonEventData eventData)
        {
            var grabber = Grabber.Get(eventData);
            var offset = RigidPose.FromToPose(grabber.grabberOrigin, new RigidPose(transform));
            if (alignPosition) { offset.pos = alignPositionOffset; }
            if (alignRotation) { offset.rot = Quaternion.Euler(alignRotationOffset); }
            grabber.grabOffset = offset;

            return grabber;
        }

        protected override void DestoryGrabber(Grabber grabber)
        {
            Grabber.Release(grabber);
        }

        protected bool IsValidGrabButton(ColliderButtonEventData eventData)
        {
            if (m_primaryGrabButton > 0ul)
            {
                ViveColliderButtonEventData viveEventData;
                if (eventData.TryGetViveButtonEventData(out viveEventData) && IsPrimeryGrabButtonOn(viveEventData.viveButton)) { return true; }
            }

            return m_secondaryGrabButton > 0u && IsSecondaryGrabButtonOn(eventData.button);
        }

        public virtual void OnColliderEventPressDown(ColliderButtonEventData eventData)
        {
            if (!IsValidGrabButton(eventData)) { return; }

            Grabber grabber;
            if (TryGetExistsGrabber(eventData, out grabber)) { return; }

            var currentFrame = Time.frameCount;
            ButtonProcessedState pState;
            if (m_buttonProcessedFrame.TryGetValue(eventData, out pState))
            {
                // skip if button was just processed for release
                if (pState.processedFrame == currentFrame)
                {
                    Debug.Assert(!pState.isGrabbing);
                    return;
                }
            }

            if (!m_allowMultipleGrabbers) { ClearGrabbers(); }

            if (m_grabOnLastEntered && !eventData.eventCaster.lastEnteredCollider.transform.IsChildOf(transform)) { return; }

            if (AddGrabber(eventData))
            {
                m_buttonProcessedFrame[eventData] = new ButtonProcessedState()
                {
                    isGrabbing = true,
                    processedFrame = currentFrame
                };

                if (m_updateCoroutine.IsNullOrDone())
                {
                    LiteCoroutine.StartCoroutine(ref m_updateCoroutine, GrabUpdate(), false);

                    if (moveByVelocity)
                    {
                        LiteCoroutine.StartCoroutine(ref m_physicsCoroutine, PhysicsGrabUpdate(), false);
                    }
                }
            }
        }

        private IEnumerator PhysicsGrabUpdate()
        {
            yield return new WaitForFixedUpdate();

            while (isGrabbed)
            {
                OnGrabRigidbody();

                yield return new WaitForFixedUpdate();
            }

            yield break;
        }

        private IEnumerator GrabUpdate()
        {
            yield return null;

            while (isGrabbed)
            {
                if (!moveByVelocity)
                {
                    RecordLatestPosesForDrop(Time.time, 0.05f);
                    OnGrabTransform();
                }

                if (toggleToRelease && m_buttonProcessedFrame.Count > 0)
                {
                    var currentFrame = Time.frameCount;
                    for (int i = m_buttonProcessedFrame.Count - 1; i >= 0; --i)
                    {
                        var pState = m_buttonProcessedFrame.GetValueByIndex(i);
                        // skip if button was just processed for grab
                        if (pState.processedFrame == currentFrame)
                        {
                            Debug.Assert(pState.isGrabbing);
                            continue;
                        }

                        if (!pState.isGrabbing)
                        {
                            m_buttonProcessedFrame.RemoveAt(i);
                        }
                        else
                        {
                            var eventData = m_buttonProcessedFrame.GetKeyByIndex(i);
                            if (!eventData.GetPressDown()) { continue; }

                            if (RemoveGrabber(eventData))
                            {
                                m_buttonProcessedFrame.SetValueByIndex(i, new ButtonProcessedState()
                                {
                                    isGrabbing = false,
                                    processedFrame = currentFrame,
                                });
                            }
                        }
                    }
                }

                yield return null;
            }
        }
    }
}