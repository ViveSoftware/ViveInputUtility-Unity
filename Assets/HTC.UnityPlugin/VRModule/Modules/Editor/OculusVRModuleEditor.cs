//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using SymbolRequirement = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirement;
using SymbolRequirementCollection = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirementCollection;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class OculusVRSymbolRequirementCollection : SymbolRequirementCollection
    {
        public OculusVRSymbolRequirementCollection()
        {
            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR",
                reqTypeNames = new string[] { "OVRInput" },
                reqFileNames = new string[] { "OVRInput.cs" },
            });
        }
    }
}