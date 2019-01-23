//========= Copyright 2016-2019, HTC Corporation. All rights reserved. ===========

using UnityEngine;
using HTC.UnityPlugin.Utility;

namespace HTC.UnityPlugin.Pointer3D
{
    public class ProjectileGenerator : BaseRaySegmentGenerator
    {
        public float velocity = 2f;
        public Vector3 gravity = Vector3.down;

        private float m_velocity;
        private Vector3 m_gravity;

        private float maxHalfJourney;// maximum distance if projectile angle equals to 45 degree using given velocity
        private float accY;// vertical accelerate

        private Vector3 systemY;
        private Vector3 systemX;

        private Vector3 v0;// initial velocity vector
        private float v0X;// initial horizontal velocity
        private float v0Y;// initial vertical velocity
        private bool isHeighPeek;// if included angle between v0 and systemX is larger then 45 degree
        private float halfJourney;// half maximum distance of projectile

        private float contactPointTime;
        private float nextContactPointTime;
        private float rayDistance;
        private float nextRayDistance;

        private void CalculateAcc()
        {
            accY = -gravity.magnitude;
        }

        private void CalculatePeekDistanceMax()
        {
            maxHalfJourney = 0.5f * velocity * velocity / Mathf.Abs(accY);
        }

        protected override void Start()
        {
            base.Start();
            CalculateAcc();
            CalculatePeekDistanceMax();
        }

        public override void ResetSegments()
        {
            var velocityChanged = ChangeProp.Set(ref m_velocity, velocity);
            var gravityChanged = ChangeProp.Set(ref m_gravity, gravity);

            if (gravityChanged)
            {
                CalculateAcc();
            }

            if (velocityChanged || gravityChanged)
            {
                CalculatePeekDistanceMax();
            }

            systemY = -gravity;
            systemX = transform.forward;
            Vector3.OrthoNormalize(ref systemY, ref systemX);

            v0 = transform.forward * velocity;
            v0X = Vector3.Dot(v0, systemX);
            v0Y = Vector3.Dot(v0, systemY);
            isHeighPeek = Mathf.Abs(v0Y) > Mathf.Abs(v0X);

            contactPointTime = v0Y / accY;
            halfJourney = Mathf.Abs(contactPointTime);

            rayDistance = nextRayDistance = 0f;
        }

        public override bool NextSegment(out Vector3 direction, out float distance)
        {
            if (isHeighPeek)
            {
                if (contactPointTime < 0f)
                {
                    nextContactPointTime = 0f;
                }
                else
                {
                    nextContactPointTime = contactPointTime + halfJourney;
                }
            }
            else
            {
                if (contactPointTime < 0f)
                {
                    nextContactPointTime = 0f;
                }
                else if (contactPointTime == 0f)
                {
                    if (halfJourney == 0f)
                    {
                        nextContactPointTime = contactPointTime + maxHalfJourney * 0.3f;
                    }
                    else
                    {
                        nextContactPointTime = contactPointTime + halfJourney;
                    }
                }
                else
                {
                    // cap to maxHalfJourney to avoid small segment
                    //nextContactPointTime = contactPointTime + maxHalfJourney;
                    nextContactPointTime = contactPointTime + Mathf.Lerp(halfJourney, maxHalfJourney, 0.3f);
                }
            }

            var lastDistance = rayDistance;
            GetTangentLineIntersectDistance(contactPointTime, nextContactPointTime, out rayDistance, out nextRayDistance, out direction);
            distance = rayDistance + lastDistance;

            // shift for next iteration
            rayDistance = nextRayDistance;
            contactPointTime = nextContactPointTime;

            if (distance <= Pointer3DRaycaster.MIN_SEGMENT_DISTANCE)
            {
                distance = Pointer3DRaycaster.MIN_SEGMENT_DISTANCE;
                //NextSegment(out direction, out distance);
            }

            return true;
        }

        private void GetTangentLineIntersectDistance(float tA, float tB, out float dA, out float dB, out Vector3 directionA)
        {
            if (tA == tB)
            {
                dA = dB = 0f;

                directionA = (systemY * (accY * tA / v0X) + systemX).normalized;
            }
            else
            {
                var vA = new Vector2(v0X * tA, 0.5f * accY * tA * tA);
                var vB = new Vector2(v0X * tB, 0.5f * accY * tB * tB);
                var mA = accY * tA / v0X;
                var mB = accY * tB / v0X;

                // C is intersect point between line through A and line through B
                var vC = default(Vector2);
                vC.x = (mA * vA.x - mB * vB.x - vA.y + vB.y) / (mA - mB);
                vC.y = mA * (vC.x - vA.x) + vA.y;

                dA = (vC - vA).magnitude;
                dB = (vC - vB).magnitude;

                directionA = (systemY * mA + systemX).normalized;
            }
        }
    }
}