//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using UnityEngine;

namespace HTC.UnityPlugin.PoseTracker
{
    /// <summary>
    /// Describe a pose by position and rotation
    /// </summary>
    [Obsolete("Use HTC.UnityPlugin.Utility.RigidPose instead")]
    [Serializable]
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
            if (t == null)
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
            return
                a.pos.x == b.pos.x &&
                a.pos.y == b.pos.y &&
                a.pos.z == b.pos.z &&
                a.rot.x == b.rot.x &&
                a.rot.y == b.rot.y &&
                a.rot.z == b.rot.z &&
                a.rot.w == b.rot.w;
        }

        public static bool operator !=(Pose a, Pose b)
        {
            return !(a == b);
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

        // proper following duration is larger then 0.02 second, depends on the update rate
        public static void SetRigidbodyVelocity(Rigidbody rigidbody, Vector3 from, Vector3 to, float duration)
        {
            var diffPos = to - from;
            if (Mathf.Approximately(diffPos.sqrMagnitude, 0f))
            {
                rigidbody.velocity = Vector3.zero;
            }
            else
            {
                rigidbody.velocity = diffPos / duration;
            }
        }

        // proper folloing duration is larger then 0.02 second, depends on the update rate
        public static void SetRigidbodyAngularVelocity(Rigidbody rigidbody, Quaternion from, Quaternion to, float duration, bool overrideMaxAngularVelocity = true)
        {
            float angle;
            Vector3 axis;
            var fromToRot = to * Quaternion.Inverse(from);
            fromToRot.ToAngleAxis(out angle, out axis);
            while (angle > 180f) { angle -= 360f; }

            if (Mathf.Approximately(angle, 0f) || float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
            {
                rigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                angle *= Mathf.Deg2Rad / duration; // convert to radius speed
                if (overrideMaxAngularVelocity && rigidbody.maxAngularVelocity < angle) { rigidbody.maxAngularVelocity = angle; }
                rigidbody.angularVelocity = axis * angle;
            }
        }

        public static Pose FromToPose(Pose from, Pose to)
        {
            var invRot = Quaternion.Inverse(from.rot);
            return new Pose(invRot * (to.pos - from.pos), invRot * to.rot);
        }

        public static implicit operator RigidPose(Pose v)
        {
            return new RigidPose(v.pos, v.rot);
        }

        public static implicit operator Pose(RigidPose v)
        {
            return new Pose(v.pos, v.rot);
        }

        public override string ToString()
        {
            return "p" + pos.ToString() + "r" + rot.ToString();
        }

        public string ToString(string format)
        {
            return "p" + pos.ToString(format) + "r" + rot.ToString(format);
        }
    }
}