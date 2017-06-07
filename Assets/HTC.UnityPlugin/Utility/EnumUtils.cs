//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    public static class EnumUtils
    {
        public static int GetMinValue(Type enumType)
        {
            var result = int.MinValue;
            foreach (int value in Enum.GetValues(enumType))
            {
                result = Mathf.Max(result, value);
            }

            return result;
        }

        public static int GetMaxValue(Type enumType)
        {
            var result = int.MinValue;
            foreach (int value in Enum.GetValues(enumType))
            {
                result = Mathf.Max(result, value);
            }

            return result;
        }
    }
}