//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    public static class ChangeProp
    {
        public static bool Set<T>(ref T currentValue, T newValue, Func<T, T, bool> equalFunc = null)
        {
            if (equalFunc == null)
            {
                if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) { return false; }
            }
            else
            {
                if (equalFunc(currentValue, newValue)) { return false; }
            }

            currentValue = newValue;
            return true;
        }

        public static bool Vector3Equal(Vector3 a, Vector3 b) { return a == b; } // (a-b).mag < Vector3.kEpsilon

        public static bool Vector3AxisApprox(Vector3 a, Vector3 b) { return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z); }

        public static bool Vector3DistanceApprox(Vector3 a, Vector3 b) { return Mathf.Approximately((a - b).sqrMagnitude, 0f); }

        public static bool Vector2Equal(Vector2 a, Vector2 b) { return a == b; } // (a-b).mag < Vector2.kEpsilon

        public static bool Vector2AxisApprox(Vector2 a, Vector2 b) { return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y); }

        public static bool Vector2DistanceApprox(Vector2 a, Vector2 b) { return Mathf.Approximately((a - b).sqrMagnitude, 0f); }

        public static bool QuaternionEqual(Quaternion a, Quaternion b) { return a == b; } // Dot(a,b) > 1f - Quaternion.kEpsilon

        public static bool QuaternionAngleApprox(Quaternion a, Quaternion b) { return Mathf.Approximately(Quaternion.Angle(a, b), 0f); }

        public static bool StringEmptyEqual(string a, string b)
        {
            var aEmpty = string.IsNullOrEmpty(a);
            var bEmpty = string.IsNullOrEmpty(b);
            return aEmpty ? bEmpty : (!bEmpty && a == b);
        }
    }
}