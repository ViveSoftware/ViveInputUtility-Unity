//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

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

        private static bool Vector3CubeApprox(Vector3 a, Vector3 b) { return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z); }

        private static bool Vector3SphereApprox(Vector3 a, Vector3 b) { return Mathf.Approximately((a - b).sqrMagnitude, 0f); }

        private static bool Vector2CubeApprox(Vector2 a, Vector2 b) { return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y); }

        private static bool Vector2SphereApprox(Vector2 a, Vector2 b) { return Mathf.Approximately((a - b).sqrMagnitude, 0f); }

        private static bool QuaternionApprox(Quaternion a, Quaternion b) { return Mathf.Approximately(Quaternion.Angle(a, b), 0f); }
    }
}