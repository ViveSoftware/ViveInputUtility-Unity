//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    public class FlagsFromEnumAttribute : PropertyAttribute
    {
        public Type EnumType { get; private set; }

        public FlagsFromEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}