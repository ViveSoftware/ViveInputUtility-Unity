//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
#pragma warning disable 0067
using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Vive
{
    public abstract class GrabberBase
    {
        public abstract RigidPose grabberOrigin { get; }
        public abstract RigidPose grabOffset { get; set; }
    }

    public abstract class GrabberBase<TEventData> : GrabberBase where TEventData : BaseEventData
    {
        public abstract TEventData eventData { get; }
        public BaseEventData eventDataBase { get { return eventData; } }
    }

    public abstract class GrabbableBase : BasePoseTracker
    {
        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        private struct PoseSample
        {
            public float time;
            public RigidPose pose;
        }

        public abstract float followingDuration { get; set; }
        public abstract bool overrideMaxAngularVelocity { get; set; }
        public abstract bool isGrabbed { get; }
        public abstract bool isChangingGrabber { get; }
        public abstract GrabberBase currentGrabberBase { get; }
        public Rigidbody grabRigidbody { get; protected set; }

        private Queue<PoseSample> m_poseSamples = new Queue<PoseSample>();

        protected virtual void Awake()
        {
            grabRigidbody = GetComponent<Rigidbody>();
        }

        protected void RecordLatestPosesForDrop(float currentTime, float recordLength)
        {
            while (m_poseSamples.Count > 0 && (currentTime - m_poseSamples.Peek().time) > recordLength)
            {
                m_poseSamples.Dequeue();
            }

            m_poseSamples.Enqueue(new PoseSample()
            {
                time = currentTime,
                pose = new RigidPose(transform),
            });
        }

        protected virtual void OnGrabRigidbody()
        {
            var currentGrabber = currentGrabberBase;
            var targetPose = currentGrabber.grabberOrigin * currentGrabber.grabOffset;
            ModifyPose(ref targetPose, true);

            RigidPose.SetRigidbodyVelocity(grabRigidbody, grabRigidbody.position, targetPose.pos, followingDuration);
            RigidPose.SetRigidbodyAngularVelocity(grabRigidbody, grabRigidbody.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
        }

        protected virtual void OnGrabTransform()
        {
            var currentGrabber = currentGrabberBase;
            var targetPose = currentGrabber.grabberOrigin * currentGrabber.grabOffset;
            ModifyPose(ref targetPose, true);

            if (grabRigidbody != null)
            {
                grabRigidbody.velocity = Vector3.zero;
                grabRigidbody.angularVelocity = Vector3.zero;
            }

            transform.position = targetPose.pos;
            transform.rotation = targetPose.rot;
        }

        protected virtual void DoDrop()
        {
            if (grabRigidbody != null && !grabRigidbody.isKinematic && m_poseSamples.Count > 0)
            {
                var framePose = m_poseSamples.Dequeue();
                var deltaTime = Time.time - framePose.time;

                RigidPose.SetRigidbodyVelocity(grabRigidbody, framePose.pose.pos, transform.position, deltaTime);
                RigidPose.SetRigidbodyAngularVelocity(grabRigidbody, framePose.pose.rot, transform.rotation, deltaTime, overrideMaxAngularVelocity);

                m_poseSamples.Clear();
            }
        }
    }

    public abstract class GrabbableBase<TEventData, TGrabber> : GrabbableBase where TGrabber : GrabberBase<TEventData> where TEventData : BaseEventData
    {
        private OrderedIndexedTable<TEventData, TGrabber> m_grabbers = new OrderedIndexedTable<TEventData, TGrabber>();

        public IIndexedTableReadOnly<TEventData, TGrabber> allGrabbers { get { return m_grabbers.ReadOnly; } }
        public TGrabber currentGrabber { get { return m_grabbers.Count > 0 ? m_grabbers.GetLastValue() : null; } }
        public sealed override GrabberBase currentGrabberBase { get { return currentGrabber; } }
        public sealed override bool isGrabbed { get { return m_grabbers.Count > 0; } }
        public sealed override bool isChangingGrabber { get { return m_grabberChangingLock = true; } }

        private bool m_grabberChangingLock;
        public event Action afterGrabberGrabbed; // get grabber thst just perform grabb here
        public event Action beforeGrabberReleased; // get grabber that about to release here
        public event Action onGrabberDrop; // manually change drop velocity here

        protected bool IsGrabberExists(TEventData eventData)
        {
            return m_grabbers.ContainsKey(eventData);
        }

        protected bool TryGetExistsGrabber(TEventData eventData, out TGrabber grabber)
        {
            return m_grabbers.TryGetValue(eventData, out grabber);
        }

        protected abstract TGrabber CreateGrabber(TEventData eventData);

        protected abstract void DestoryGrabber(TGrabber grabber);

        protected bool AddGrabber(TEventData eventData)
        {
            if (m_grabbers.ContainsKey(eventData)) { return false; }

            if (!EnterGrabberChangingLock()) { return false; }
            try
            {
                var newGrabber = CreateGrabber(eventData);
                if (newGrabber == null) { return false; }
                Debug.Assert(newGrabber.eventData == eventData);

                if (isGrabbed) { beforeGrabberReleased(); }
                m_grabbers.Add(eventData, newGrabber);
                afterGrabberGrabbed();
                return true;
            }
            finally { ExitGrabberChangingLock(); }
        }

        protected bool RemoveGrabber(TEventData eventData)
        {
            TGrabber grabber;
            if (!m_grabbers.TryGetValue(eventData, out grabber)) { return false; }

            if (m_grabbers.Count == 1)
            {
                ClearGrabbers();
            }
            else if (grabber != currentGrabber)
            {
                m_grabbers.Remove(grabber.eventData);
            }
            else
            {
                if (!EnterGrabberChangingLock()) { return false; }
                try
                {
                    beforeGrabberReleased();
                    m_grabbers.Remove(grabber.eventData);
                    afterGrabberGrabbed();
                }
                finally { ExitGrabberChangingLock(); }
            }

            return true;
        }

        protected void ClearGrabbers()
        {
            if (!isGrabbed) { return; }

            if (!EnterGrabberChangingLock()) { return; }
            try
            {
                beforeGrabberReleased();
                m_grabbers.Clear();
                DoDrop();
                onGrabberDrop();
            }
            finally { ExitGrabberChangingLock(); }
        }

        public virtual void ForceRelease()
        {
            ClearGrabbers();
        }

        private bool EnterGrabberChangingLock()
        {
            if (m_grabberChangingLock)
            {
                Debug.LogWarning("[" + GetType().Name + "] Add/Remove grabber in ");
                return false;
            }

            m_grabberChangingLock = true;
            return true;
        }

        private void ExitGrabberChangingLock()
        {
            m_grabberChangingLock = false;
        }
    }

    [Obsolete("Use GrabbableBase.Generic<TGrabber> instead")]
    public abstract class GrabbableBase<TGrabber> : BasePoseTracker where TGrabber : class, GrabbableBase<TGrabber>.IGrabber
    {
        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

        [Obsolete("Use GrabbableBase instead")]
        public interface IGrabber
        {
            RigidPose grabberOrigin { get; }
            RigidPose grabOffset { get; }
        }

        private struct PoseSample
        {
            public float time;
            public RigidPose pose;
        }

        private Queue<PoseSample> m_poseSamples = new Queue<PoseSample>();
        private OrderedIndexedSet<TGrabber> m_grabbers = new OrderedIndexedSet<TGrabber>();
        private bool m_grabMutex;
        private Action m_afterGrabberGrabbed;
        private Action m_beforeGrabberReleased;
        private Action m_onGrabberDrop;

        public virtual float followingDuration { get { return DEFAULT_FOLLOWING_DURATION; } set { } }
        public virtual bool overrideMaxAngularVelocity { get { return true; } set { } }

        public TGrabber currentGrabber { get; private set; }
        public bool isGrabbed { get { return currentGrabber != null; } }
        public Rigidbody grabRigidbody { get; set; }

        public event Action afterGrabberGrabbed { add { m_afterGrabberGrabbed += value; } remove { m_afterGrabberGrabbed -= value; } }
        public event Action beforeGrabberReleased { add { m_beforeGrabberReleased += value; } remove { m_beforeGrabberReleased -= value; } }
        public event Action onGrabberDrop { add { m_onGrabberDrop += value; } remove { m_onGrabberDrop -= value; } }

        protected virtual void Awake()
        {
            grabRigidbody = GetComponent<Rigidbody>();
        }

        protected bool AddGrabber(TGrabber grabber)
        {
            if (grabber == null || m_grabbers.Contains(grabber)) { return false; }

            CheckRecursiveException("AddGrabber");

            if (isGrabbed && m_beforeGrabberReleased != null)
            {
                m_grabMutex = true;
                m_beforeGrabberReleased();
                m_grabMutex = false;
            }

            m_grabbers.Add(grabber);
            currentGrabber = grabber;

            if (m_afterGrabberGrabbed != null)
            {
                m_afterGrabberGrabbed();
            }

            return true;
        }

        protected bool RemoveGrabber(TGrabber grabber)
        {
            if (grabber == null || !m_grabbers.Contains(grabber)) { return false; }

            CheckRecursiveException("RemoveGrabber");

            if (m_grabbers.Count == 1)
            {
                ClearGrabbers(true);
            }
            else if (grabber == currentGrabber)
            {
                if (m_beforeGrabberReleased != null)
                {
                    m_grabMutex = true;
                    m_beforeGrabberReleased();
                    m_grabMutex = false;
                }

                m_grabbers.Remove(grabber);
                currentGrabber = m_grabbers.GetLast();

                if (m_afterGrabberGrabbed != null)
                {
                    m_afterGrabberGrabbed();
                }
            }
            else
            {
                m_grabbers.Remove(grabber);
            }

            return true;
        }

        protected void ClearGrabbers(bool doDrop)
        {
            if (m_grabbers.Count == 0) { return; }

            CheckRecursiveException("ClearGrabbers");

            if (m_beforeGrabberReleased != null)
            {
                m_grabMutex = true;
                m_beforeGrabberReleased();
                m_grabMutex = false;
            }

            m_grabbers.Clear();
            currentGrabber = null;

            if (doDrop)
            {
                if (grabRigidbody != null && !grabRigidbody.isKinematic && m_poseSamples.Count > 0)
                {
                    var framePose = m_poseSamples.Dequeue();
                    var deltaTime = Time.time - framePose.time;

                    RigidPose.SetRigidbodyVelocity(grabRigidbody, framePose.pose.pos, transform.position, deltaTime);
                    RigidPose.SetRigidbodyAngularVelocity(grabRigidbody, framePose.pose.rot, transform.rotation, deltaTime, overrideMaxAngularVelocity);

                    m_poseSamples.Clear();
                }

                if (m_onGrabberDrop != null)
                {
                    m_onGrabberDrop();
                }
            }
        }

        protected void OnGrabRigidbody()
        {
            var targetPose = currentGrabber.grabberOrigin * currentGrabber.grabOffset;
            ModifyPose(ref targetPose, null);

            RigidPose.SetRigidbodyVelocity(grabRigidbody, grabRigidbody.position, targetPose.pos, followingDuration);
            RigidPose.SetRigidbodyAngularVelocity(grabRigidbody, grabRigidbody.rotation, targetPose.rot, followingDuration, overrideMaxAngularVelocity);
        }

        protected virtual void OnGrabTransform()
        {
            var targetPose = currentGrabber.grabberOrigin * currentGrabber.grabOffset;
            ModifyPose(ref targetPose, null);

            if (grabRigidbody != null)
            {
                grabRigidbody.velocity = Vector3.zero;
                grabRigidbody.angularVelocity = Vector3.zero;
            }

            transform.position = targetPose.pos;
            transform.rotation = targetPose.rot;
        }

        protected void RecordLatestPosesForDrop(float currentTime, float recordLength)
        {
            while (m_poseSamples.Count > 0 && (currentTime - m_poseSamples.Peek().time) > recordLength)
            {
                m_poseSamples.Dequeue();
            }

            m_poseSamples.Enqueue(new PoseSample()
            {
                time = currentTime,
                pose = new RigidPose(transform),
            });
        }

        private void CheckRecursiveException(string func)
        {
            if (!m_grabMutex) { return; }
            throw new Exception("[" + func + "] Cannot Add/Remove Grabber recursivly");
        }
    }
}