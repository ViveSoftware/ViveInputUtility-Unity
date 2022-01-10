//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    [RequireComponent(typeof(BasePoseTracker))]
    public abstract class BasePoseModifier : MonoBehaviour, IPoseModifier
    {
        [SerializeField]
        private int m_priority;

        public BasePoseTracker baseTracker { get; protected set; }

        public virtual int priority
        {
            get { return m_priority; }
            set
            {
                if (m_priority != value)
                {
                    m_priority = value;
                    // let tracker refresh order
                    if (baseTracker.RemoveModifier(this))
                    {
                        baseTracker.AddModifier(this);
                    }
                };
            }
        }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            priority = m_priority;
        }
#endif
        protected virtual void Awake()
        {
            baseTracker = GetComponent<BasePoseTracker>();
            baseTracker.AddModifier(this);
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy()
        {
            baseTracker.RemoveModifier(this);
        }

        [Obsolete]
        public virtual void ModifyPose(ref Pose pose, Transform origin) { }
        [Obsolete]
        public virtual void ModifyPose(ref RigidPose pose, Transform origin) { }

        public virtual void ModifyPose(ref RigidPose pose, bool useLocal) { }
    }
}