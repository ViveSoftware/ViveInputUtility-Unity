//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System.Reflection;
using SymbolRequirement = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirement;
using SymbolRequirementCollection = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirementCollection;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class SteamVRSymbolRequirementCollection : SymbolRequirementCollection
    {
        public SteamVRSymbolRequirementCollection()
        {
            Add(new SymbolRequirement()
            {
                symbol = "VIU_OPENVR_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportOpenVR,
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_OPENVR_API",
                reqTypeNames = new string[] { "Valve.VR.OpenVR" },
                reqFileNames = new string[] { "openvr_api.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR",
                reqTypeNames = new string[] { "Valve.VR.OpenVR" },
                reqAnyTypeNames = new string[] { "SteamVR", "Valve.VR.SteamVR" },
                reqFileNames = new string[] { "openvr_api.cs", "SteamVR.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_1_1",
                reqTypeNames = new string[] { "SteamVR_Utils+Event" },
                reqFileNames = new string[] { "SteamVR_Utils.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_0_OR_NEWER",
                reqAnyTypeNames = new string[] { "SteamVR_Events", "Valve.VR.SteamVR_Events" },
                reqFileNames = new string[] { "SteamVR_Events.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_1_OR_NEWER",
                reqAnyMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "SteamVR_Events",
                         name = "System",
                         argTypeNames = new string[] { "Valve.VR.EVREventType" },
                         bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    },
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "Valve.VR.SteamVR_Events",
                         name = "System",
                         argTypeNames = new string[] { "Valve.VR.EVREventType" },
                         bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                reqFileNames = new string[] { "SteamVR_Events.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_2_OR_NEWER",
                reqAnyFields = new SymbolRequirement.ReqFieldInfo[]
                {
                    new SymbolRequirement.ReqFieldInfo()
                    {
                        typeName = "SteamVR_ExternalCamera+Config",
                        name = "r",
                        bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    },
                    new SymbolRequirement.ReqFieldInfo()
                    {
                        typeName = "Valve.VR.SteamVR_ExternalCamera+Config",
                        name = "r",
                        bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "SteamVR_ExternalCamera.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_1_2_3_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                         typeName = "Valve.VR.CVRSystem",
                         name = "IsInputAvailable",
                         bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
                reqFileNames = new string[] { "openvr_api.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_2_0_0_OR_NEWER",
                reqTypeNames = new string[] { "Valve.VR.SteamVR" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_2_1_0_OR_NEWER",
                reqTypeNames = new string[] { "Valve.VR.SteamVR_ActionSet_Manager" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_2_2_0_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "Valve.VR.SteamVR_ActionSet_Manager",
                        name = "UpdateActionStates",
                        argTypeNames = new string[]
                        {
                            "System.Boolean",
                        },
                        bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_2_4_0_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "Valve.VR.SteamVR_Input",
                        name = "GetActionsFilePath",
                        argTypeNames = new string[]
                        {
                            "System.Boolean",
                        },
                        bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_STEAMVR_2_6_0_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "Valve.VR.CVROverlay",
                        name = "ShowKeyboard",
                        argTypeNames = new string[]
                        {
                            "System.Int32",
                            "System.Int32",
                            "System.UInt32",
                            "System.String",
                            "System.UInt32",
                            "System.String",
                            "System.UInt64",
                        },
                        bindingAttr = BindingFlags.Public | BindingFlags.Instance,
                    }
                },
            });
        }
    }
}