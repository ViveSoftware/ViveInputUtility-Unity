//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public class StandaloneEventData : Pointer3DEventData
    {
        public enum StandaloneButton
        {
            Fire1,
            Fire2,
            Fire3,
            MouseLeft,
            MouseMiddle,
            MouseRight,
        }

        public readonly StandaloneButton standaloneButton;

        public StandaloneEventData(Pointer3DRaycaster ownerRaycaster, EventSystem eventSystem, StandaloneButton sbtn, InputButton ibtn) : base(ownerRaycaster, eventSystem)
        {
            standaloneButton = sbtn;
            button = ibtn;
        }

        public override bool GetPress()
        {
            switch (standaloneButton)
            {
                case StandaloneButton.Fire1: return Input.GetButton("Fire1");
                case StandaloneButton.Fire2: return Input.GetButton("Fire2");
                case StandaloneButton.Fire3: return Input.GetButton("Fire3");
                case StandaloneButton.MouseLeft: return Input.GetMouseButton(0);
                case StandaloneButton.MouseRight: return Input.GetMouseButton(1);
                case StandaloneButton.MouseMiddle: return Input.GetMouseButton(2);
            }
            return false;
        }

        public override bool GetPressDown()
        {
            switch (standaloneButton)
            {
                case StandaloneButton.Fire1: return Input.GetButtonDown("Fire1");
                case StandaloneButton.Fire2: return Input.GetButtonDown("Fire2");
                case StandaloneButton.Fire3: return Input.GetButtonDown("Fire3");
                case StandaloneButton.MouseLeft: return Input.GetMouseButtonDown(0);
                case StandaloneButton.MouseRight: return Input.GetMouseButtonDown(1);
                case StandaloneButton.MouseMiddle: return Input.GetMouseButtonDown(2);
            }
            return false;
        }

        public override bool GetPressUp()
        {
            switch (standaloneButton)
            {
                case StandaloneButton.Fire1: return Input.GetButtonUp("Fire1");
                case StandaloneButton.Fire2: return Input.GetButtonUp("Fire2");
                case StandaloneButton.Fire3: return Input.GetButtonUp("Fire3");
                case StandaloneButton.MouseLeft: return Input.GetMouseButtonUp(0);
                case StandaloneButton.MouseRight: return Input.GetMouseButtonUp(1);
                case StandaloneButton.MouseMiddle: return Input.GetMouseButtonUp(2);
            }
            return false;
        }
    }
}