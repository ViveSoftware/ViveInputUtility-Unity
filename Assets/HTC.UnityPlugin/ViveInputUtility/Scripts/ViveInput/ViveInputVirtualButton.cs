//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HTC.UnityPlugin.Vive
{
    // Use this helper component to combine multiple Vive inputs into one virtual button
    public class ViveInputVirtualButton : MonoBehaviour
    {
        public enum InputsOperatorEnum
        {
            Or,
            And,
        }

        [Serializable]
        public class InputEntry
        {
            public ViveRoleProperty viveRole = ViveRoleProperty.New(HandRole.RightHand);
            [CustomOrderedEnum]
            public ControllerButton button = ControllerButton.Trigger;
        }

        [Serializable]
        public struct OutputEventArgs
        {
            public ViveInputVirtualButton senderObj;
            public ButtonEventType eventType;
        }

        [Serializable]
        public class OutputEvent : UnityEvent<OutputEventArgs> { }

        [SerializeField]
        private InputsOperatorEnum m_combineInputsOperator = InputsOperatorEnum.Or;
        [SerializeField]
        private List<InputEntry> m_inputs = new List<InputEntry>();
        [SerializeField]
        private OutputEvent m_onVirtualPress = new OutputEvent();
        [SerializeField]
        private OutputEvent m_onVirtualClick = new OutputEvent();
        [SerializeField]
        private OutputEvent m_onVirtualPressDown = new OutputEvent();
        [SerializeField]
        private OutputEvent m_onVirtualPressUp = new OutputEvent();
        [SerializeField]
        private List<GameObject> m_toggleGameObjectOnVirtualClick = new List<GameObject>();
        [SerializeField]
        private List<Behaviour> m_toggleComponentOnVirtualClick = new List<Behaviour>();

        private bool m_isUpdating;
        private int m_updatedFrameCount;
        private bool m_prevPressState = false;
        private bool m_currPressState = false;
        private float m_lastPressDownTime = 0f;
        private int m_clickCount = 0;

        [Obsolete("Use Behaviour.enable instead.")]
        public bool active { get { return enabled; } set { enabled = value; } }

        public InputsOperatorEnum combineInputsOperator { get { return m_combineInputsOperator; } }
        public List<InputEntry> inputs { get { return m_inputs; } }
        public List<GameObject> toggleGameObjectOnVirtualClick { get { return m_toggleGameObjectOnVirtualClick; } }
        public List<Behaviour> toggleComponentOnVirtualClick { get { return m_toggleComponentOnVirtualClick; } }

        public OutputEvent onPress { get { return m_onVirtualPress; } }
        public OutputEvent onClick { get { return m_onVirtualClick; } }
        public OutputEvent onPressDown { get { return m_onVirtualPressDown; } }
        public OutputEvent onPressUp { get { return m_onVirtualPressUp; } }

        private bool isPress { get { return m_currPressState; } }
        private bool isDown { get { return !m_prevPressState && m_currPressState; } }
        private bool isUp { get { return m_prevPressState && !m_currPressState; } }

#if UNITY_EDITOR
        private void Reset()
        {
            m_inputs.Add(new InputEntry()
            {
                viveRole = ViveRoleProperty.New(HandRole.RightHand),
                button = ControllerButton.Trigger,
            });
        }
#endif

        private void UpdateState()
        {
            if (!ChangeProp.Set(ref m_updatedFrameCount, Time.frameCount)) { return; }

            m_prevPressState = m_currPressState;
            m_currPressState = false;

            if (m_inputs.Count == 0) { return; }

            switch (m_combineInputsOperator)
            {
                case InputsOperatorEnum.Or:

                    m_currPressState = false;

                    for (int i = 0, imax = m_inputs.Count; i < imax; ++i)
                    {
                        if (ViveInput.GetPress(m_inputs[i].viveRole, m_inputs[i].button))
                        {
                            m_currPressState = true;
                            break;
                        }
                    }

                    break;
                case InputsOperatorEnum.And:

                    m_currPressState = true;

                    for (int i = 0, imax = m_inputs.Count; i < imax; ++i)
                    {
                        if (!ViveInput.GetPress(m_inputs[i].viveRole, m_inputs[i].button))
                        {
                            m_currPressState = false;
                            break;
                        }
                    }

                    break;
            }
        }

        private void Update()
        {
            m_isUpdating = true;

            UpdateState();

            var timeNow = Time.unscaledTime;
            // handle events
            if (isPress)
            {
                if (isDown)
                {
                    // record click count
                    if (timeNow - m_lastPressDownTime < ViveInput.clickInterval)
                    {
                        ++m_clickCount;
                    }
                    else
                    {
                        m_clickCount = 1;
                    }

                    // record press down time
                    m_lastPressDownTime = timeNow;

                    // PressDown event
                    if (m_onVirtualPressDown != null)
                    {
                        m_onVirtualPressDown.Invoke(new OutputEventArgs()
                        {
                            senderObj = this,
                            eventType = ButtonEventType.Down,
                        });
                    }
                }

                // Press event
                if (m_onVirtualPress != null)
                {
                    m_onVirtualPress.Invoke(new OutputEventArgs()
                    {
                        senderObj = this,
                        eventType = ButtonEventType.Press,
                    });
                }
            }
            else if (isUp)
            {
                // PressUp event
                if (m_onVirtualPressUp != null)
                {
                    m_onVirtualPressUp.Invoke(new OutputEventArgs()
                    {
                        senderObj = this,
                        eventType = ButtonEventType.Up,
                    });
                }

                if (timeNow - m_lastPressDownTime < ViveInput.clickInterval)
                {
                    for (int i = m_toggleGameObjectOnVirtualClick.Count - 1; i >= 0; --i)
                    {
                        if (m_toggleGameObjectOnVirtualClick[i] != null) { m_toggleGameObjectOnVirtualClick[i].SetActive(!m_toggleGameObjectOnVirtualClick[i].activeSelf); }
                    }

                    for (int i = m_toggleComponentOnVirtualClick.Count - 1; i >= 0; --i)
                    {
                        if (m_toggleComponentOnVirtualClick[i] != null) { m_toggleComponentOnVirtualClick[i].enabled = !m_toggleComponentOnVirtualClick[i].enabled; }
                    }

                    // Click event
                    if (m_onVirtualClick != null)
                    {
                        m_onVirtualClick.Invoke(new OutputEventArgs()
                        {
                            senderObj = this,
                            eventType = ButtonEventType.Click,
                        });
                    }
                }
            }

            if (!isActiveAndEnabled)
            {
                InternalDisable();
            }

            m_isUpdating = false;
        }

        private void OnDisable()
        {
            if (!m_isUpdating)
            {
                InternalDisable();
            }
        }

        private void InternalDisable()
        {
            var timeNow = Time.unscaledTime;

            // clean up
            m_prevPressState = m_currPressState;
            m_currPressState = false;

            if (isUp)
            {
                // PressUp event
                if (m_onVirtualPressUp != null)
                {
                    m_onVirtualPressUp.Invoke(new OutputEventArgs()
                    {
                        senderObj = this,
                        eventType = ButtonEventType.Up,
                    });
                }

                if (timeNow - m_lastPressDownTime < ViveInput.clickInterval)
                {
                    for (int i = m_toggleGameObjectOnVirtualClick.Count - 1; i >= 0; --i)
                    {
                        if (m_toggleGameObjectOnVirtualClick[i] != null) { m_toggleGameObjectOnVirtualClick[i].SetActive(!m_toggleGameObjectOnVirtualClick[i].activeSelf); }
                    }

                    for (int i = m_toggleComponentOnVirtualClick.Count - 1; i >= 0; --i)
                    {
                        if (m_toggleComponentOnVirtualClick[i] != null) { m_toggleComponentOnVirtualClick[i].enabled = !m_toggleComponentOnVirtualClick[i].enabled; }
                    }

                    // Click event
                    if (m_onVirtualClick != null)
                    {
                        m_onVirtualClick.Invoke(new OutputEventArgs()
                        {
                            senderObj = this,
                            eventType = ButtonEventType.Click,
                        });
                    }
                }
            }

            m_prevPressState = false;
        }
        
        public bool GetVirtualPress()
        {
            UpdateState();
            return isPress;
        }

        public bool GetVirtualPressDown()
        {
            UpdateState();
            return isDown;
        }

        public bool GetVirtualPressUp()
        {
            UpdateState();
            return isUp;
        }

        public int GetVirtualClickCount()
        {
            UpdateState();
            return m_clickCount;
        }

        public float GetLastVirtualPressDownTime()
        {
            UpdateState();
            return m_lastPressDownTime;
        }
    }
}