//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

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
        private bool m_active = true;
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

        private int m_updatedFrameCount;
        private bool m_updateActivated = false;
        private bool m_prevState = false;
        private bool m_currState = false;
        private float m_lastPressDownTime = 0f;
        private int m_clickCount = 0;

        public bool active
        {
            get
            {
                return m_active;
            }
            set
            {
                m_active = value;
                TryListenUpdateEvent();
            }
        }

        public InputsOperatorEnum combineInputsOperator { get { return m_combineInputsOperator; } }
        public List<InputEntry> inputs { get { return m_inputs; } }
        public List<GameObject> toggleGameObjectOnVirtualClick { get { return m_toggleGameObjectOnVirtualClick; } }
        public List<Behaviour> toggleComponentOnVirtualClick { get { return m_toggleComponentOnVirtualClick; } }

        public OutputEvent onPress { get { return m_onVirtualPress; } }
        public OutputEvent onClick { get { return m_onVirtualClick; } }
        public OutputEvent onPressDown { get { return m_onVirtualPressDown; } }
        public OutputEvent onPressUp { get { return m_onVirtualPressUp; } }

        private bool isPress { get { return m_currState; } }
        private bool isDown { get { return !m_prevState && m_currState; } }
        private bool isUp { get { return m_prevState && !m_currState; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            TryListenUpdateEvent();
        }

        private void Reset()
        {
            m_inputs.Add(new InputEntry()
            {
                viveRole = ViveRoleProperty.New(HandRole.RightHand),
                button = ControllerButton.Trigger,
            });
        }
#endif

        private void Awake()
        {
            TryListenUpdateEvent();
        }

        private void TryListenUpdateEvent()
        {
            if (enabled && Application.isPlaying && m_active && !m_updateActivated)
            {
                // register update event
                ViveInput.onUpdate += OnInputStateUpdated;
                m_updateActivated = true;
                ViveInput.Initialize();
            }
        }

        private void UpdateState()
        {
            if (!ChangeProp.Set(ref m_updatedFrameCount, Time.frameCount)) { return; }

            m_prevState = m_currState;
            m_currState = false;

            if (m_inputs.Count == 0) { return; }

            switch (m_combineInputsOperator)
            {
                case InputsOperatorEnum.Or:

                    m_currState = false;

                    for (int i = 0, imax = m_inputs.Count; i < imax; ++i)
                    {
                        if (ViveInput.GetPress(m_inputs[i].viveRole, m_inputs[i].button))
                        {
                            m_currState = true;
                            break;
                        }
                    }

                    break;
                case InputsOperatorEnum.And:

                    m_currState = true;

                    for (int i = 0, imax = m_inputs.Count; i < imax; ++i)
                    {
                        if (!ViveInput.GetPress(m_inputs[i].viveRole, m_inputs[i].button))
                        {
                            m_currState = false;
                            break;
                        }
                    }

                    break;
            }
        }

        private void OnInputStateUpdated()
        {
            var timeNow = Time.unscaledTime;

            if (m_active)
            {
                UpdateState();

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
            }
            else
            {
                // unregister update event
                ViveInput.onUpdate -= OnInputStateUpdated;
                m_updateActivated = false;

                // clean up
                m_prevState = m_currState;
                m_currState = false;

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

                m_prevState = false;
            }
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

        public void ToggleActive()
        {
            active = !active;
        }
    }
}