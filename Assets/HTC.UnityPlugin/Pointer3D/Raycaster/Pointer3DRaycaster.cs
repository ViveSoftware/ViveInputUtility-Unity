//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HTC.UnityPlugin.Pointer3D
{
    public enum RaycastMode
    {
        DefaultRaycast,
        Projection,
        Projectile,
    }

    // Contains and handles Pointer3DEventDatas, derived class should implement their own Pointer3DEventData
    // and add into buttonEventDataList, Pointer3DInputModule then will send eventData to the raycast target.
    public class Pointer3DRaycaster : BaseFallbackCamRaycaster
    {
        public const float MIN_SEGMENT_DISTANCE = 0.01f;

        private ReadOnlyCollection<Pointer3DEventData> buttonEventDataListReadOnly;
        private ReadOnlyCollection<RaycastResult> sortedRaycastResultsReadOnly;
        private ReadOnlyCollection<Vector3> breakPointsReadOnly;

        protected readonly List<Pointer3DEventData> buttonEventDataList = new List<Pointer3DEventData>();
        protected readonly List<RaycastResult> sortedRaycastResults = new List<RaycastResult>();
        protected readonly List<Vector3> breakPoints = new List<Vector3>();

        public float dragThreshold = 0.02f;
        public float clickInterval = 0.3f;

        public bool showDebugRay = true;
        
        public Pointer3DEventData HoverEventData { get { return buttonEventDataList.Count > 0 ? buttonEventDataList[0] : null; } }

        public ReadOnlyCollection<Pointer3DEventData> ButtonEventDataList
        {
            get { return buttonEventDataListReadOnly ?? (buttonEventDataListReadOnly = buttonEventDataList.AsReadOnly()); }
        }

        public ReadOnlyCollection<RaycastResult> SortedRaycastResults
        {
            get { return sortedRaycastResultsReadOnly ?? (sortedRaycastResultsReadOnly = sortedRaycastResults.AsReadOnly()); }
        }

        public ReadOnlyCollection<Vector3> BreakPoints
        {
            get { return breakPointsReadOnly ?? (breakPointsReadOnly = breakPoints.AsReadOnly()); }
        }

        public virtual Vector2 GetScrollDelta()
        {
            return Vector2.zero;
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // auto create RaySegmentGenerator if raycast mode is set when using previous version
            // and reset to DefaultRaycast since raycastMode is obsoleted
            if (m_raycastMode != RaycastMode.DefaultRaycast)
            {
                if (Application.isPlaying)
                {
                    SetLagacyRaycastMode(m_raycastMode);
                    m_raycastMode = RaycastMode.DefaultRaycast;
                }
                else
                {
                    // force saving changes of raycastMode
                    // use delayCall because sometimes scene is not loaded and can't be marked dirty at this time
                    UnityEditor.EditorApplication.delayCall += new UnityEditor.EditorApplication.CallbackFunction(() =>
                    {
                        Debug.LogWarning("The RaycastMode." + m_raycastMode + " setting has been replaced by adding " + (m_raycastMode == RaycastMode.Projection ? "ProjectionGenerator" : "ProjectileGenerator")
                            + " component. Please save the scene to preserve this changes.");
                        SetLagacyRaycastMode(m_raycastMode);
                        m_raycastMode = RaycastMode.DefaultRaycast;
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                        UnityEditor.EditorUtility.SetDirty(this);
                    });
                }
            }
        }
#endif
        protected override void Start()
        {
            base.Start();

            if (m_raycastMode != RaycastMode.DefaultRaycast)
            {
                SetLagacyRaycastMode(m_raycastMode);
                m_raycastMode = RaycastMode.DefaultRaycast;
            }
        }

        // override OnEnable & OnDisable on purpose so that this BaseRaycaster won't be registered into RaycasterManager
        protected override void OnEnable()
        {
            //base.OnEnable();
            Pointer3DInputModule.AddRaycaster(this);
        }

        protected override void OnDisable()
        {
            //base.OnDisable();
            Pointer3DInputModule.RemoveRaycaster(this);
        }

        public virtual void CleanUpRaycast()
        {
            sortedRaycastResults.Clear();
            breakPoints.Clear();
        }

        // invoke by Pointer3DInputModule
        public virtual void Raycast()
        {
            sortedRaycastResults.Clear();
            breakPoints.Clear();

            var zScale = transform.lossyScale.z;
            var amountDistance = (FarDistance - NearDistance) * zScale;
            var origin = transform.TransformPoint(0f, 0f, NearDistance);

            breakPoints.Add(origin);

            bool hasNext = true;
            Vector3 direction;
            float distance;
            Ray ray;
            RaycastResult firstHit = default(RaycastResult);

            var generator = CurrentSegmentGenerator();
            if (ReferenceEquals(generator, null))
            {
                // process default raycast

                direction = transform.forward;
                distance = amountDistance;
                ray = new Ray(origin, direction);

                // move event camera in place
                eventCamera.farClipPlane = eventCamera.nearClipPlane + distance;
                eventCamera.transform.position = ray.origin - (ray.direction * eventCamera.nearClipPlane);
                eventCamera.transform.rotation = Quaternion.LookRotation(ray.direction, transform.up);

                ForeachRaycastMethods(ray, distance, sortedRaycastResults);

                firstHit = FirstRaycastResult();
                breakPoints.Add(firstHit.isValid ? firstHit.worldPosition : ray.GetPoint(distance));
#if UNITY_EDITOR
                if (showDebugRay)
                {
                    Debug.DrawLine(breakPoints[0], breakPoints[1], firstHit.isValid ? Color.green : Color.red);
                }
#endif
            }
            else
            {
                generator.ResetSegments();

                do
                {
                    hasNext = generator.NextSegment(out direction, out distance);

                    if (distance < MIN_SEGMENT_DISTANCE)
                    {
                        Debug.LogWarning("RaySegment.distance cannot smaller than " + MIN_SEGMENT_DISTANCE + "! distance=" + distance.ToString("0.000"));
                        break;
                    }

                    distance *= zScale;

                    if (distance < amountDistance)
                    {
                        amountDistance -= distance;
                    }
                    else
                    {
                        distance = amountDistance;
                        amountDistance = 0f;
                    }

                    ray = new Ray(origin, direction);

                    // move event camera in place
                    eventCamera.farClipPlane = eventCamera.nearClipPlane + distance;
                    eventCamera.transform.position = ray.origin - (ray.direction * eventCamera.nearClipPlane);
                    eventCamera.transform.rotation = Quaternion.LookRotation(ray.direction, transform.up);

                    ForeachRaycastMethods(ray, distance, sortedRaycastResults);

                    firstHit = FirstRaycastResult();
                    // end loop if raycast hit
                    if (firstHit.isValid)
                    {
                        breakPoints.Add(firstHit.worldPosition);
#if UNITY_EDITOR
                        if (showDebugRay)
                        {
                            Debug.DrawLine(breakPoints[breakPoints.Count - 2], breakPoints[breakPoints.Count - 1], Color.green);
                        }
#endif
                        break;
                    }
                    // otherwise, shift to next iteration
                    origin = ray.GetPoint(distance);
                    breakPoints.Add(origin);
#if UNITY_EDITOR
                    if (showDebugRay)
                    {
                        Debug.DrawLine(breakPoints[breakPoints.Count - 2], breakPoints[breakPoints.Count - 1], Color.red);
                    }
#endif
                }
                while (hasNext && amountDistance > 0f);
            }
        }
        // called by StandaloneInputModule, not supported
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) { }

        public RaycastResult FirstRaycastResult()
        {
            for (int i = 0, imax = sortedRaycastResults.Count; i < imax; ++i)
            {
                if (!sortedRaycastResults[i].isValid) { continue; }
                return sortedRaycastResults[i];
            }
            return default(RaycastResult);
        }

        #region managing segment generators
        private IndexedSet<IRaySegmentGenerator> generators = new IndexedSet<IRaySegmentGenerator>();

        public void AddGenerator(IRaySegmentGenerator generator)
        {
            generators.AddUnique(generator);
        }

        public void RemoveGenerator(IRaySegmentGenerator generator)
        {
            generators.Remove(generator);
        }

        // returns first enabled generator in generators, doesn't gaurantee any orders
        public IRaySegmentGenerator CurrentSegmentGenerator()
        {
            for (int i = 0, imax = generators.Count; i < imax; ++i)
            {
                if (generators[i].enabled) { return generators[i]; }
            }
            return null;
        }

        // enable or create TGenerator, disable others
        public TGenerator ForceEnableSegmentGenerator<TGenerator>() where TGenerator : MonoBehaviour, IRaySegmentGenerator
        {
            var gen = default(TGenerator);
            var genFound = false;

            var gens = ListPool<IRaySegmentGenerator>.Get();
            GetComponents(gens);
            for (int i = 0, imax = gens.Count; i < imax; ++i)
            {
                if (genFound || !(gens[i] is TGenerator))
                {
                    gens[i].enabled = false;
                }
                else
                {
                    gens[i].enabled = true;
                    gen = gens[i] as TGenerator;
                    genFound = true;
                }
            }
            ListPool<IRaySegmentGenerator>.Release(gens);

            if (!genFound)
            {
                gen = gameObject.AddComponent<TGenerator>();
            }

            return gen;
        }

        public void ForceDisableSegmentGenerators()
        {
            var gens = ListPool<IRaySegmentGenerator>.Get();
            GetComponents(gens);
            for (int i = 0, imax = gens.Count; i < imax; ++i)
            {
                gens[i].enabled = false;
            }
            ListPool<IRaySegmentGenerator>.Release(gens);
        }
        #endregion

        #region obsolete interfaces
        [HideInInspector]
        [SerializeField]
        private RaycastMode m_raycastMode = RaycastMode.DefaultRaycast;
        [HideInInspector]
        [SerializeField]
        private float m_velocity = 2f;
        [HideInInspector]
        [SerializeField]
        private Vector3 m_gravity = Vector3.down;

        [Obsolete("Please use component ProjectionGenerator / ProjectileGenerator")]
        public RaycastMode raycastMode
        {
            get { return m_raycastMode; }
            set
            {
                m_raycastMode = value;
                SetLagacyRaycastMode(value);
            }
        }

        [Obsolete("Please use ProjectionGenerator.velocity / ProjectileGenerator.velocity")]
        public float velocity
        {
            get { return m_velocity; }
            set
            {
                m_velocity = value;
                var gen = CurrentSegmentGenerator();
                if (gen != null)
                {
                    if (gen is ProjectionGenerator) { ((ProjectionGenerator)gen).velocity = value; }
                    else if (gen is ProjectileGenerator) { ((ProjectileGenerator)gen).velocity = value; }
                }
            }
        }

        [Obsolete("Please use ProjectionGenerator.gravity / ProjectileGenerator.gravity")]
        public Vector3 gravity
        {
            get { return m_gravity; }
            set
            {
                m_gravity = value;
                var gen = CurrentSegmentGenerator();
                if (gen != null)
                {
                    if (gen is ProjectionGenerator) { ((ProjectionGenerator)gen).gravity = value; }
                    else if (gen is ProjectileGenerator) { ((ProjectileGenerator)gen).gravity = value; }
                }
            }
        }

        [Obsolete("Please use component ProjectionGenerator / ProjectileGenerator")]
        protected virtual void InitSegmentGenerator() { }

        // for backward compatible
        private void SetLagacyRaycastMode(RaycastMode mode)
        {
            switch (mode)
            {
                case RaycastMode.Projection:
                    {
                        var gen = ForceEnableSegmentGenerator<ProjectionGenerator>();
                        gen.velocity = m_velocity;
                        gen.gravity = m_gravity;
                        break;
                    }
                case RaycastMode.Projectile:
                    {
                        var gen = ForceEnableSegmentGenerator<ProjectileGenerator>();
                        gen.velocity = m_velocity;
                        gen.gravity = m_gravity;
                        break;
                    }
                default:
                    {
                        ForceDisableSegmentGenerators();
                        break;
                    }
            }
        }
        #endregion

        public override string ToString()
        {
            var str = string.Empty;
            str += "Raycaster path: " + Pointer3DInputModule.PrintGOPath(gameObject) + "(" + GetType().Name + ")\n";
            str += "Raycaster transform: " + "pos" + transform.position.ToString("0.00") + " " + "rot" + transform.eulerAngles.ToString("0.0") + "\n";

            for (int i = 0, imax = buttonEventDataList.Count; i < imax; ++i)
            {
                var eventData = buttonEventDataList[i];
                if (eventData == null) { continue; }

                if (eventData.eligibleForClick || (i == 0 && eventData.pointerEnter != null)) // is hover event?
                {
                    str += "<b>EventData: [" + i + "]</b>\n";
                    str += eventData.ToString();
                }
            }

            return str;
        }
    }
}