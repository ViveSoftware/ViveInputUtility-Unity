using HTC.UnityPlugin.PoseTracker;
using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public abstract class GrabbableBase<TGrabber> : BasePoseTracker where TGrabber : class, GrabbableBase<TGrabber>.IGrabber
    {
        public const float MIN_FOLLOWING_DURATION = 0.02f;
        public const float DEFAULT_FOLLOWING_DURATION = 0.04f;
        public const float MAX_FOLLOWING_DURATION = 0.5f;

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

        protected void OnGrabTransform()
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