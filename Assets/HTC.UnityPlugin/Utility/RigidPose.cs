//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using System;
using UnityEngine;

namespace HTC.UnityPlugin.Utility
{
    [Serializable]
    public struct RigidPose
    {
        public Vector3 pos;
        public Quaternion rot;

        public static RigidPose identity
        {
            get { return new RigidPose(Vector3.zero, Quaternion.identity); }
        }

        public RigidPose(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }

        public RigidPose(Transform t, bool useLocal = false)
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

        public Vector3 forward { get { return rot * Vector3.forward; } }

        public Vector3 right { get { return rot * Vector3.right; } }

        public Vector3 up { get { return rot * Vector3.up; } }


        public override bool Equals(object o)
        {
            if (o is RigidPose)
            {
                var t = (RigidPose)o;
                return pos == t.pos && rot == t.rot;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() ^ rot.GetHashCode();
        }

        public static bool operator ==(RigidPose a, RigidPose b)
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

        public static bool operator !=(RigidPose a, RigidPose b)
        {
            return !(a == b);
        }

        public static RigidPose operator *(RigidPose a, RigidPose b)
        {
            return new RigidPose
            {
                rot = a.rot * b.rot,
                pos = a.pos + a.rot * b.pos
            };
        }

        public void Multiply(RigidPose a, RigidPose b)
        {
            rot = a.rot * b.rot;
            pos = a.pos + a.rot * b.pos;
        }

        public void Inverse()
        {
            rot = Quaternion.Inverse(rot);
            pos = -(rot * pos);
        }

        public RigidPose GetInverse()
        {
            var t = new RigidPose(pos, rot);
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

        public static RigidPose Lerp(RigidPose a, RigidPose b, float t)
        {
            return new RigidPose(Vector3.Lerp(a.pos, b.pos, t), Quaternion.Slerp(a.rot, b.rot, t));
        }

        public void Lerp(RigidPose to, float t)
        {
            pos = Vector3.Lerp(pos, to.pos, t);
            rot = Quaternion.Slerp(rot, to.rot, t);
        }

        public static RigidPose LerpUnclamped(RigidPose a, RigidPose b, float t)
        {
            return new RigidPose(Vector3.LerpUnclamped(a.pos, b.pos, t), Quaternion.SlerpUnclamped(a.rot, b.rot, t));
        }

        public void LerpUnclamped(RigidPose to, float t)
        {
            pos = Vector3.LerpUnclamped(pos, to.pos, t);
            rot = Quaternion.SlerpUnclamped(rot, to.rot, t);
        }

        public static void SetPose(Transform target, RigidPose pose, Transform origin = null)
        {
            if (origin != null && origin != target.parent)
            {
                target.position = origin.transform.TransformPoint(pose.pos);
                target.rotation = origin.rotation * pose.rot;
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

        public static RigidPose FromToPose(RigidPose from, RigidPose to)
        {
            var invRot = Quaternion.Inverse(from.rot);
            return new RigidPose(invRot * (to.pos - from.pos), invRot * to.rot);
        }

#if  UNITY_2017_2_OR_NEWER
        public static implicit operator RigidPose(Pose v)
        {
            return new RigidPose(v.position, v.rotation);
        }

        public static implicit operator Pose(RigidPose v)
        {
            return new Pose(v.pos, v.rot);
        }
#endif

        public override string ToString()
        {
            return "{p" + pos.ToString() + ",r" + rot.ToString() + "}";
        }

        public string ToString(string format)
        {
            return "{p" + pos.ToString(format) + ",r" + rot.ToString(format) + "}";
        }
    }
}