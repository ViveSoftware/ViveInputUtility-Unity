//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using System;
using System.Reflection;
using UnityEngine;
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

            Add(new SymbolRequirement
            {
                symbol = "VIU_OCULUSVR_AVATAR",
                reqTypeNames = new string[] { "OvrAvatar" },
                reqFileNames = new string[] { "OvrAvatar.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_1_32_0_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "OvrAvatarSDKManager",
                         name = "RequestAvatarSpecification",
                         argTypeNames = new string[]
                         {
                             "System.UInt64",
                             "specificationCallback",
                             "System.Boolean",
                             "ovrAvatarAssetLevelOfDetail",
                             "System.Boolean",
                         },
                         bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "OvrAvatarSDKManager.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_1_36_0_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "OvrAvatarSDKManager",
                         name = "RequestAvatarSpecification",
                         argTypeNames = new string[]
                         {
                             "System.UInt64",
                             "specificationCallback",
                             "System.Boolean",
                             "ovrAvatarAssetLevelOfDetail",
                             "System.Boolean",
                             "ovrAvatarLookAndFeelVersion",
                             "ovrAvatarLookAndFeelVersion",
                             "System.Boolean",
                         },
                         bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "OvrAvatarSDKManager.cs" },
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

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_16_0_OR_NEWER",
                reqTypeNames = new string[] { "OVRPlugin+SystemHeadset" },
                validateFunc = (req) =>
                {
                    Type oculusQuest;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("OVRPlugin+SystemHeadset", out oculusQuest) && oculusQuest.IsEnum)
                    {
                        if (Enum.IsDefined(oculusQuest, "Oculus_Link_Quest"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "OVRPlugin.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OCULUSVR_19_0_OR_NEWER",
                reqTypeNames = new string[] { "OVRPlugin+SystemHeadset" },
                validateFunc = (req) =>
                {
                    Type oculusGo;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("OVRPlugin+SystemHeadset", out oculusGo) && oculusGo.IsEnum)
                    {
                        if (!Enum.IsDefined(oculusGo, "Oculus_Go"))
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