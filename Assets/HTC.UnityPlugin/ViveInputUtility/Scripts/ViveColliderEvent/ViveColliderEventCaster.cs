//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.ColliderEvent;
using HTC.UnityPlugin.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace HTC.UnityPlugin.Vive
{
    [AddComponentMenu("HTC/VIU/Object Grabber/Vive Collider Event Caster (Grabber)", 2)]
    public class ViveColliderEventCaster : ColliderEventCaster, IViveRoleComponent
    {
        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);
        [SerializeField]
        [CustomOrderedEnum]
        private ControllerButton m_buttonTrigger = ControllerButton.Trigger;
        [SerializeField]
        [CustomOrderedEnum]
        private ControllerButton m_buttonPadOrStick = ControllerButton.Pad;
        [SerializeField]
        [CustomOrderedEnum]
        private ControllerButton m_buttonGripOrHandTrigger = ControllerButton.Grip;
        [SerializeField]
        [CustomOrderedEnum]
        private ControllerButton m_buttonFunctionKey = ControllerButton.Menu;
        [SerializeField]
        [FormerlySerializedAs("m_buttonEvents")]
        [FlagsFromEnum(typeof(ControllerButton))]
        private uint m_additionalButtons = 0;
        [SerializeField]
        private ScrollType m_scrollType = ScrollType.Auto;
        [SerializeField]
        private Vector2 m_scrollDeltaScale = new Vector2(1f, -1f);

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
        public ScrollType scrollType { get { return m_scrollType; } set { m_scrollType = value; } }
        public Vector2 scrollDeltaScale { get { return m_scrollDeltaScale; } set { m_scrollDeltaScale = value; } }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            FilterOutAssignedButton();
        }
#endif
        protected void FilterOutAssignedButton()
        {
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_buttonTrigger);
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_buttonPadOrStick);
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_buttonFunctionKey);
            m_additionalButtons = EnumUtils.UnsetFlag(m_additionalButtons, (int)m_buttonGripOrHandTrigger);
        }

        protected virtual void Start()
        {
            buttonEventDataList.Add(new ViveColliderButtonEventData(this, m_buttonTrigger, ColliderButtonEventData.InputButton.Trigger));
            buttonEventDataList.Add(new ViveColliderButtonEventData(this, m_buttonPadOrStick, ColliderButtonEventData.InputButton.PadOrStick));
            buttonEventDataList.Add(new ViveColliderButtonEventData(this, m_buttonFunctionKey, ColliderButtonEventData.InputButton.FunctionKey));
            buttonEventDataList.Add(new ViveColliderButtonEventData(this, m_buttonGripOrHandTrigger, ColliderButtonEventData.InputButton.GripOrHandTrigger));

            FilterOutAssignedButton();

            var eventBtn = ColliderButtonEventData.InputButton.GripOrHandTrigger + 1;
            var addBtns = m_additionalButtons;
            for (ControllerButton btn = 0; addBtns > 0u; ++btn, addBtns >>= 1)
            {
                if ((addBtns & 1u) == 0u) { continue; }

                buttonEventDataList.Add(new ViveColliderButtonEventData(this, btn, eventBtn++));
            }

            axisEventDataList.Add(new ViveColliderPadAxisEventData(this));
            axisEventDataList.Add(new ViveColliderTriggerAxisEventData(this));
        }
    }
}