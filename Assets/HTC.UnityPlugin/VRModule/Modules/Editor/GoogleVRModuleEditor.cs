//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using SymbolRequirement = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirement;
using SymbolRequirementCollection = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirementCollection;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class GoogleVRSymbolRequirementCollection : SymbolRequirementCollection
    {
        public GoogleVRSymbolRequirementCollection()
        {
            Add(new SymbolRequirement()
            {
                symbol = "VIU_GOOGLEVR_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportDaydream,
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_GOOGLEVR",
                reqTypeNames = new string[] { "GvrUnitySdkVersion" },
                reqFileNames = new string[] { "GvrUnitySdkVersion.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_GOOGLEVR_1_150_0_NEWER",
                reqTypeNames = new string[] { "GvrControllerInputDevice" },
                reqFileNames = new string[] { "GvrControllerInputDevice.cs" },
            });
        }
    }
}