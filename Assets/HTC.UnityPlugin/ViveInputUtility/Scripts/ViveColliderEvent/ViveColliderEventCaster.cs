//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using UnityEngine;
using System;
using HTC.UnityPlugin.Utility;

namespace HTC.UnityPlugin.Vive
{
    public class ViveColliderEventCaster : ColliderEventCaster, IViveRoleComponent
    {
        [Obsolete]
        public enum ButtonEventSource
        {
            BothHands,
            RightHandOnly,
            LeftHandOnly,
            None,
        }

        [HideInInspector]
        [SerializeField]
        [Obsolete]
        private ButtonEventSource buttonEventSource;

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        [FlagsFromEnum(typeof(ControllerButton))]
        private int m_buttonEvents = (1 << (int)ControllerButton.Trigger) | (1 << (int)ControllerButton.Pad) | (1 << (int)ControllerButton.Grip);

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            var serializedObject = new UnityEditor.SerializedObject(this);

            var btnEventSrcProp = serializedObject.FindProperty("buttonEventSource");
            var btnEventSrc = btnEventSrcProp.intValue;

            if (btnEventSrc != 3) // ButtonEventSource.None
            {
                btnEventSrcProp.intValue = 3;

                if (!Application.isPlaying)
                {
                    serializedObject.ApplyModifiedProperties();
                }

                switch (btnEventSrc)
                {
                    case 2:
                        viveRole.SetEx(HandRole.LeftHand);
                        break;
                    case 0:
                    case 1:
                    default:
                        viveRole.SetEx(HandRole.RightHand);
                        break;
                }

                if (!Application.isPlaying)
                {
                    serializedObject.Update();
                }
            }
            serializedObject.Dispose();
        }
#endif
        protected virtual void Start()
        {
            for (var i = ControllerButton.Trigger; i <= ControllerButton.FullTrigger; ++i)
            {
                if ((m_buttonEvents & (1 << (int)i)) == 0) { continue; }
                buttonEventDataList.Add(new ViveColliderButtonEventData(this, m_viveRole, i));
            }

            axisEventDataList.Add(new ViveColliderTriggerValueEventData(this, m_viveRole));
            axisEventDataList.Add(new ViveColliderPadAxisEventData(this, m_viveRole));
        }
    }
}