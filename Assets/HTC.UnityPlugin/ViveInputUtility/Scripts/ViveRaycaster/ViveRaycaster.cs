//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using HTC.UnityPlugin.Utility;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/Vive/Vive Raycaster")]
    // Custom Pointer3DRaycaster implement for Vive controller.
    public class ViveRaycaster : Pointer3DRaycaster, IViveRoleComponent
    {
        [Obsolete]
        public enum ButtonEventSource
        {
            AllButtons,
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
        public float scrollDeltaScale = 50f;

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

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
        protected override void Start()
        {
            base.Start();

            for (var i = ControllerButton.Trigger; i <= ControllerButton.FullTrigger; ++i)
            {
                if ((m_buttonEvents & (1 << (int)i)) == 0) { continue; }
                buttonEventDataList.Add(new VivePointerEventData(this, EventSystem.current, m_viveRole, i, (PointerEventData.InputButton)i));
            }
        }

        public override Vector2 GetScrollDelta()
        {
            return ViveInput.GetPadTouchDeltaEx(m_viveRole.roleType, m_viveRole.roleValue) * scrollDeltaScale;
        }
    }
}