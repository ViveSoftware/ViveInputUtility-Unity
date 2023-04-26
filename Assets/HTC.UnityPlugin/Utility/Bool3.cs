//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public struct Bool3
    {
        public bool x;
        public bool y;
        public bool z;

        public Bool3(bool x, bool y, bool z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public bool Any { get { return x || y || z; } }

        public bool All { get { return x && y && z; } }

        public static Vector3 OverwriteVector3(Vector3 src, Bool3 applyAxis, Vector3 dst)
        {
            if (applyAxis.x) { dst.x = src.x; }
            if (applyAxis.y) { dst.y = src.y; }
            if (applyAxis.z) { dst.z = src.z; }
            return dst;
        }

        public static Bool3 AllTrue { get { return new Bool3(true, true, true); } }
    }
}