//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using UnityEngine;

namespace HTC.UnityPlugin.Vive.VIUExample
{
    public class ModeManager : MonoBehaviour
    {
        public enum Mode
        {
            Default,
            Teleport,
            Menu,
        }

        public Mode initMode = Mode.Teleport;
        public DefaultTooltipRenderer tooltipRenderer;
        public DefaultTooltipRenderDataAsset teleportTooltip;
        public DefaultTooltipRenderDataAsset menuTooltip;
        public GameObject teleportPointers;
        public GameObject selectColorUI;
        private Mode currentMode;

        private void Awake()
        {
            EnterMode(initMode);
        }

        public void SwitchToTeleportMode() { SwitchMode(Mode.Teleport); }

        public void SwitchToMenuMode() { SwitchMode(Mode.Menu); }

        private void SwitchMode(Mode mode)
        {
            if (currentMode != mode)
            {
                ExitMode(currentMode);
                currentMode = mode;
                EnterMode(mode);
            }
        }

        private void EnterMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.Default: break;
                case Mode.Teleport: EnterTeleportMode(); break;
                case Mode.Menu: EnterMenuMode(); break;
            }
        }

        private void ExitMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.Default: break;
                case Mode.Teleport: ExitTeleportMode(); break;
                case Mode.Menu: ExitMenuMode(); break;
            }
        }

        private void EnterTeleportMode()
        {
            ViveInput.AddListenerEx(HandRole.RightHand, ControllerButton.Menu, ButtonEventType.Down, SwitchToMenuMode);
            teleportPointers.SetActive(true);
            tooltipRenderer.SetTooltipData(teleportTooltip);
        }

        private void ExitTeleportMode()
        {
            ViveInput.RemoveListenerEx(HandRole.RightHand, ControllerButton.Menu, ButtonEventType.Down, SwitchToMenuMode);
            teleportPointers.SetActive(false);
            tooltipRenderer.ClearTooltipData();
        }

        private void EnterMenuMode()
        {
            ViveInput.AddListenerEx(HandRole.RightHand, ControllerButton.Menu, ButtonEventType.Down, SwitchToTeleportMode);
            selectColorUI.SetActive(true);
            tooltipRenderer.SetTooltipData(menuTooltip);
        }

        private void ExitMenuMode()
        {
            ViveInput.RemoveListenerEx(HandRole.RightHand, ControllerButton.Menu, ButtonEventType.Down, SwitchToTeleportMode);
            selectColorUI.SetActive(false);
            tooltipRenderer.ClearTooltipData();
        }
    }
}