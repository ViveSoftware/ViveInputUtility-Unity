//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    public class CustomOrderedEnumAttribute : PropertyAttribute
    {
        public Type overrideEnumType { get; private set; }

        public CustomOrderedEnumAttribute(Type overrideEnumType = null)
        {
            this.overrideEnumType = overrideEnumType;
        }
    }
}