//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HTC.UnityPlugin.Vive.VIUExample
{
    public class SelectColorUIController : MonoBehaviour
    {
        [Serializable]
        public class UnityEventColor : UnityEvent<Color> { }

        public Toggle[] colorToggles;
        public UnityEventColor onColorSelected;

        private UnityAction<bool>[] toggleCallbacks;

        private void OnEnable()
        {
            if (colorToggles != null && colorToggles.Length > 0)
            {
                if (toggleCallbacks == null || toggleCallbacks.Length != colorToggles.Length)
                {
                    toggleCallbacks = new UnityAction<bool>[colorToggles.Length];
                }

                for (int i = 0, imax = colorToggles.Length; i < imax; ++i)
                {
                    var callback = toggleCallbacks[i];
                    if (callback == null)
                    {
                        callback = CreateAction(colorToggles[i]);
                        toggleCallbacks[i] = callback;
                    }

                    colorToggles[i].onValueChanged.AddListener(callback);
                }
            }
        }

        private UnityAction<bool> CreateAction(Toggle toggle)
        {
            return isOn => OnToggleValueChanged(toggle, isOn);
        }

        private void OnToggleValueChanged(Toggle toggle, bool isOn)
        {
            if (isOn && onColorSelected != null)
            {
                onColorSelected.Invoke(toggle.targetGraphic.color);
            }
        }

        private void OnDisable()
        {
            if (toggleCallbacks != null)
            {
                for (int i = 0, imax = toggleCallbacks.Length; i < imax; ++i)
                {
                    colorToggles[i].onValueChanged.RemoveListener(toggleCallbacks[i]);
                }
            }
        }
    }
}