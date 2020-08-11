﻿//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using System;
using System.Reflection;
using HTC.UnityPlugin.Vive;
using SymbolRequirement = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirement;
using SymbolRequirementCollection = HTC.UnityPlugin.VRModuleManagement.VRModuleManagerEditor.SymbolRequirementCollection;

namespace HTC.UnityPlugin.VRModuleManagement
{
    public class WaveVRSymbolRequirementCollection : SymbolRequirementCollection
    {
        public WaveVRSymbolRequirementCollection()
        {
            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_SUPPORT",
                validateFunc = (req) => Vive.VIUSettingsEditor.supportWaveVR,
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR",
                reqTypeNames = new string[] { "WaveVR" },
                reqFileNames = new string[] { "WaveVR.cs" },
            });

            Add(new VRModuleManagerEditor.SymbolRequirement()
            {
                symbol = "VIU_WAVEXR_ESSENCE_RENDERMODEL",
                reqTypeNames = new string[] { "Wave.Essence.Controller.RenderModel", "Wave.Essence.Controller.ButtonEffect", "Wave.Essence.Controller.ShowIndicator" },
                reqFileNames = new string[] { "RenderModel.cs", "ButtonEffect.cs", "ShowIndicator.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_2_0_32_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "wvr.Interop",
                        name = "WVR_GetInputDeviceState",
                        argTypeNames = new string[]
                        {
                            "wvr.WVR_DeviceType",
                            "System.UInt32",
                            "System.UInt32&",
                            "System.UInt32&",
                            "wvr.WVR_AnalogState_t[]",
                            "System.UInt32",
                        },
                        bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                reqFileNames = new string[] { "wvr.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_2_1_0_OR_NEWER",
                reqTypeNames = new string[] { "wvr.WVR_InputId" },
                validateFunc = (req) =>
                {
                    Type wvrInputIdType;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("wvr.WVR_InputId", out wvrInputIdType) && wvrInputIdType.IsEnum)
                    {
                        if (Enum.IsDefined(wvrInputIdType, "WVR_InputId_Alias1_Digital_Trigger"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "wvr.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_3_0_0_OR_NEWER",
                reqTypeNames = new string[] { "wvr.WVR_Eye" },
                validateFunc = (req) =>
                {
                    Type wvrEyeType;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("wvr.WVR_Eye", out wvrEyeType) && wvrEyeType.IsEnum)
                    {
                        if (Enum.IsDefined(wvrEyeType, "WVR_Eye_Both"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "wvr.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_3_1_0_OR_NEWER",
                reqTypeNames = new string[] { "wvr.WVR_Intensity" },
                validateFunc = (req) =>
                {
                    Type wvrIntensityType;
                    if (SymbolRequirement.s_foundTypes.TryGetValue("wvr.WVR_Intensity", out wvrIntensityType) && wvrIntensityType.IsEnum)
                    {
                        if (Enum.IsDefined(wvrIntensityType, "WVR_Intensity_Normal"))
                        {
                            return true;
                        }
                    }
                    return false;
                },
                reqFileNames = new string[] { "wvr.cs" },
            });

            Add(new SymbolRequirement()
            {
                symbol = "VIU_WAVEVR_3_1_3_OR_NEWER",
                reqMethods = new SymbolRequirement.ReqMethodInfo[]
                {
                    new SymbolRequirement.ReqMethodInfo()
                    {
                        typeName = "wvr.Interop",
                        name = "WVR_PostInit",
                        bindingAttr = BindingFlags.Public | BindingFlags.Static,
                    }
                },
                reqFileNames = new string[] { "wvr.cs" },
            });
        }
    }
}