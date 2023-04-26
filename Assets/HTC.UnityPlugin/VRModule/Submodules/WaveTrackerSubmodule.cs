//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;

#if VIU_WAVEVR_TRACKER
using Wave.XR.Settings;
#endif

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class WaveTrackerSubmodule : VRModule.SubmoduleBase
    {
#if VIU_WAVEVR_TRACKER
        private static readonly string log_prefix = "[" + typeof(WaveTrackerSubmodule).Name + "] ";

        public override bool ShouldActiveModule() { return VRModuleSettings.activateWaveTrackerSubmodule; }

        protected override void OnActivated()
        {
            ActivateTracker(true);
        }

        protected override void OnDeactivated()
        {
            ActivateTracker(false);
        }

        private void ActivateTracker(bool enable)
        {
            WaveXRSettings settings = WaveXRSettings.GetInstance();
            if (settings != null && settings.EnableTracker != enable)
            {
                settings.EnableTracker = enable;
                Debug.Log(log_prefix + " ActivateTracker " + (settings.EnableTracker ? "Activate." : "Deactivate."));
                SettingsHelper.SetBool(WaveXRSettings.EnableTrackerText, settings.EnableTracker);
            }
        }
#endif
    }
}