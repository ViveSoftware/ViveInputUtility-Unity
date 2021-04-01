﻿//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using SymbolRequirement = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirement;
using SymbolRequirementCollection = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirementCollection;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class ViveHandTrackingRequirementCollection : SymbolRequirementCollection
    {
        public ViveHandTrackingRequirementCollection()
        {
            Add(new SymbolRequirement()
            {
                symbol = "VIU_VIVE_HAND_TRACKING",
                reqTypeNames = new string[] { "ViveHandTracking.GestureInterface", "ViveHandTracking.GestureOption" },
                reqFileNames = new string[] { "aristo_interface.dll", "GestureInterface.cs" },
            });
        }
    }
}