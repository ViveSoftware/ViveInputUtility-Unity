//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/UI Pointer/Vive Raycaster (VR Controller Input)", 4)]
    // Customized Pointer3DRaycaster for Vive controllers.
    public class ViveRaycaster : Pointer3DRaycaster, IViveRoleComponent
    {
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        [FormerlySerializedAs("m_mouseBtnLeft")]
        [CustomOrderedEnum]
        private ControllerButton m_mouseButtonLeft = ControllerButton.Trigger;
        [SerializeField]
        [FormerlySerializedAs("m_mouseBtnMiddle")]
        [CustomOrderedEnum]
        private ControllerButton m_mouseButtonMiddle = ControllerButton.Grip;
        [SerializeField]
        [FormerlySerializedAs("m_mouseBtnRight")]
        [CustomOrderedEnum]
        private ControllerButton m_mouseButtonRight = ControllerButton.Pad;
        [SerializeField]
        [FormerlySerializedAs("m_buttonEvents")]
        [FlagsFromEnum(typeof(ControllerButton))]
        private uint m_additionalButtons = 0;
        [SerializeField]
        private ScrollType m_scrollType = ScrollType.Auto;
        [SerializeField]
        private Vector2 m_scrollDeltaScale = new Vector2(1f, -1f);

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
        public ControllerButton mouseButtonLeft { get { return m_mouseButtonLeft; } }
        public ControllerButton mouseButtonMiddle { get { return m_mouseButtonMiddle; } }
        public ControllerButton mouseButtonRight { get { return m_mouseButtonRight; } }
        public uint additionalButtonMask { get { return m_additionalButtons; } }
        public ScrollType scrollType { get { return m_scrollType; } set { m_scrollType = value; } }
        public Vector2 scrollDeltaScale { get { return m_scrollDeltaScale; } set { m_scrollDeltaScale = value; } }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            FilterOutAssignedButton();
        }
#endif
        protected void FilterOutAssignedButton()
        {
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_mouseButtonLeft);
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_mouseButtonMiddle);
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_mouseButtonRight);
        }

        protected override void Start()
        {
            base.Start();

            buttonEventDataList.Add(new VivePointerEventData(this, EventSystem.current, m_mouseButtonLeft, PointerEventData.InputButton.Left));
            buttonEventDataList.Add(new VivePointerEventData(this, EventSystem.current, m_mouseButtonRight, PointerEventData.InputButton.Right));
            buttonEventDataList.Add(new VivePointerEventData(this, EventSystem.current, m_mouseButtonMiddle, PointerEventData.InputButton.Middle));

            FilterOutAssignedButton();

            var mouseBtn = PointerEventData.InputButton.Middle + 1;
            var addBtns = m_additionalButtons;
            for (ControllerButton btn = 0; addBtns > 0u; ++btn, addBtns >>= 1)
            {
                if ((addBtns & 1u) == 0u) { continue; }

                buttonEventDataList.Add(new VivePointerEventData(this, EventSystem.current, btn, mouseBtn++));
            }
        }

        public override Vector2 GetScrollDelta()
        {
            return ViveInput.GetScrollDelta(m_viveRole, m_scrollType, m_scrollDeltaScale);
        }
    }
}