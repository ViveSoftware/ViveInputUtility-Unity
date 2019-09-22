//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using System;
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
                symbol = "VIU_OCULUSVR_DESKTOP_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportOculus,
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_ANDROID_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportOculusGo,
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR",
                reqTypeNames = new string[] { "OVRInput" },
                reqFileNames = new string[] { "OVRInput.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_1_37_0_OR_NEWER",
                reqTypeNames = new string[] { "OVRPlugin+SystemHeadset" },
                validateFunc = (req) =>
                {
                    Type oculusQuest;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("OVRPlugin+SystemHeadset", out oculusQuest) && oculusQuest.IsEnum)
                    {
                        if (Enum.IsDefined(oculusQuest, "Oculus_Quest") && Enum.IsDefined(oculusQuest, "Rift_S"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "OVRPlugin.cs" },
            });
        }
    }
}