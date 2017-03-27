//========= Copyright 2016-2017, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    /// <summary>
    /// Describe a pose by position and rotation
    /// </summary>
    public struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;

        public static Pose identity
        {
            get { return new Pose(Vector3.zero, Quaternion.identity); }
        }

        public Pose(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }

        public Pose(Transform t, bool useLocal = false)
        {
            if(t == null)
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
            }
            else if (!useLocal)
            {
                pos = t.position;
                rot = t.rotation;
            }
            else
            {
                pos = t.localPosition;
                rot = t.localRotation;
            }
        }

        public Pose(Valve.VR.HmdMatrix34_t pose)
        {
            var m = Matrix4x4.identity;

            m[0, 0] = pose.m0;
            m[0, 1] = pose.m1;
            m[0, 2] = -pose.m2;
            m[0, 3] = pose.m3;

            m[1, 0] = pose.m4;
            m[1, 1] = pose.m5;
            m[1, 2] = -pose.m6;
            m[1, 3] = pose.m7;

            m[2, 0] = -pose.m8;
            m[2, 1] = -pose.m9;
            m[2, 2] = pose.m10;
            m[2, 3] = -pose.m11;

            pos = GetPosition(m);
            rot = GetRotation(m);
        }

        private static Vector3 GetPosition(Matrix4x4 matrix)
        {
            var x = matrix.m03;
            var y = matrix.m13;
            var z = matrix.m23;

            return new Vector3(x, y, z);
        }

        private static Quaternion GetRotation(Matrix4x4 matrix)
        {
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
            q.x = _copysign(q.x, matrix.m21 - matrix.m12);
            q.y = _copysign(q.y, matrix.m02 - matrix.m20);
            q.z = _copysign(q.z, matrix.m10 - matrix.m01);
            return q;
        }

        private static float _copysign(float sizeval, float signval)
        {
            return Mathf.Sign(signval) == 1 ? Mathf.Abs(sizeval) : -Mathf.Abs(sizeval);
        }

        public override bool Equals(object o)
        {
            if (o is Pose)
            {
                Pose t = (Pose)o;
                return pos == t.pos && rot == t.rot;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() ^ rot.GetHashCode();
        }

        public static bool operator ==(Pose a, Pose b)
        {
            return a.pos == b.pos && a.rot == b.rot;
        }

        public static bool operator !=(Pose a, Pose b)
        {
            return a.pos != b.pos || a.rot != b.rot;
        }

        public static Pose operator *(Pose a, Pose b)
        {
            return new Pose
            {
                rot = a.rot * b.rot,
                pos = a.pos + a.rot * b.pos
            };
        }

        public void Multiply(Pose a, Pose b)
        {
            rot = a.rot * b.rot;
            pos = a.pos + a.rot * b.pos;
        }

        public void Inverse()
        {
            rot = Quaternion.Inverse(rot);
            pos = -(rot * pos);
        }

        public Pose GetInverse()
        {
            var t = new Pose(pos, rot);
            t.Inverse();
            return t;
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            return Quaternion.Inverse(rot) * (point - pos);
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            return pos + (rot * point);
        }

        public static Pose Lerp(Pose a, Pose b, float t)
        {
            return new Pose(Vector3.Lerp(a.pos, b.pos, t), Quaternion.Slerp(a.rot, b.rot, t));
        }

        public void Lerp(Pose to, float t)
        {
            pos = Vector3.Lerp(pos, to.pos, t);
            rot = Quaternion.Slerp(rot, to.rot, t);
        }

        public static Pose LerpUnclamped(Pose a, Pose b, float t)
        {
            return new Pose(Vector3.LerpUnclamped(a.pos, b.pos, t), Quaternion.SlerpUnclamped(a.rot, b.rot, t));
        }

        public void LerpUnclamped(Pose to, float t)
        {
            pos = Vector3.LerpUnclamped(pos, to.pos, t);
            rot = Quaternion.SlerpUnclamped(rot, to.rot, t);
        }

        public static void SetPose(Transform target, Pose pose, Transform origin = null)
        {
            if (origin != null && origin != target.parent)
            {
                pose = new Pose(origin) * pose;
                pose.pos.Scale(origin.localScale);
                target.position = pose.pos;
                target.rotation = pose.rot;
            }
            else
            {
                target.localPosition = pose.pos;
                target.localRotation = pose.rot;
            }
        }

        public static Pose FromToPose(Pose from, Pose to)
        {
            var invRot = Quaternion.Inverse(from.rot);
            return new Pose(invRot * (to.pos - from.pos), invRot * to.rot);
        }
    }
}