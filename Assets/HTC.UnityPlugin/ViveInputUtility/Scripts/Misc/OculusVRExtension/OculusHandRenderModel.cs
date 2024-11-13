//========= Copyright 2016-2024, HTC Corporation. All rights reserved. ===========

using HTC.UnityPlugin.Utility;
using System;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    public class OculusHandRenderModel : MonoBehaviour
    {
#if VIU_OCULUSVR_20_0_OR_NEWER
        public static readonly bool SUPPORTED = true;
        private bool m_isLeftHand;
        private OVRHand m_ovrHand;
        private OVRSkeleton m_ovrSkeleton;
        private OVRMesh m_ovrMesh;

        public void Initialize(bool isLeftHand)
        {
            m_isLeftHand = isLeftHand;

            try
            {
                m_ovrHand = gameObject.AddComponent<OVRHand>();
                UpdateOvrHand();

                m_ovrSkeleton = gameObject.AddComponent<OVRSkeleton>();
                UpdateOvrSkeleton();

                var dataProviderField = typeof(OVRSkeleton).GetField("_dataProvider", BindingFlags.NonPublic | BindingFlags.Instance);
                var searchDataProviderMethod = typeof(OVRSkeleton).GetMethod("SearchSkeletonDataProvider", BindingFlags.NonPublic | BindingFlags.Instance);
                var updateRootScaleField = typeof(OVRSkeleton).GetField("_updateRootScale", BindingFlags.NonPublic | BindingFlags.Instance);
                var updateRootPoseField = typeof(OVRSkeleton).GetField("_updateRootPose", BindingFlags.NonPublic | BindingFlags.Instance);
                var skeletonInitializeMethod = typeof(OVRSkeleton).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);

                if (dataProviderField != null && searchDataProviderMethod != null)
                {
                    dataProviderField.SetValue(m_ovrSkeleton, searchDataProviderMethod.Invoke(m_ovrSkeleton, new object[] { }));
                }

                if (updateRootScaleField != null)
                {
                    updateRootScaleField.SetValue(m_ovrSkeleton, true);
                }

                if (updateRootPoseField != null)
                {
                    updateRootPoseField.SetValue(m_ovrSkeleton, false);
                }

                if (skeletonInitializeMethod != null)
                {
                    skeletonInitializeMethod.Invoke(m_ovrSkeleton, new object[] { });
                }


                m_ovrMesh = gameObject.AddComponent<OVRMesh>();
                UpdateOvrMesh();

                var meshInitializeMethod = typeof(OVRMesh).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);

                if (meshInitializeMethod != null)
                {
                    meshInitializeMethod.Invoke(m_ovrMesh, new object[] { m_isLeftHand ? OVRMesh.MeshType.HandLeft : OVRMesh.MeshType.HandRight });
                }

                gameObject.AddComponent<OVRMeshRenderer>();

                SkinnedMeshRenderer skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            }
            catch (Exception e)
            {
                Debug.LogError("OculusHandRenderModel initialize failed: " + e);
            }
        }

        public void SetHand(bool isLeftHand)
        {
            if (m_isLeftHand == isLeftHand)
            {
                return;
            }

            m_isLeftHand = isLeftHand;
            UpdateHand();
        }

        private void UpdateHand()
        {
            UpdateOvrHand();
            UpdateOvrSkeleton();
            UpdateOvrMesh();
        }

        private static FieldInfo OVRHand_HandType_Field = typeof(OVRHand).GetField("HandType", BindingFlags.NonPublic | BindingFlags.Instance);
        private void UpdateOvrHand()
        {
            if (OVRHand_HandType_Field != null)
            {
                OVRHand_HandType_Field.SetValue(m_ovrHand, m_isLeftHand ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight);
            }
            else
            {
                Debug.LogError("Failed to update OVRHand: OVRHand_HandType_Field not found");
            }
        }

        private static FieldInfo OVRSkeleton_skeletonType_Field = typeof(OVRSkeleton).GetField("_skeletonType", BindingFlags.NonPublic | BindingFlags.Instance);
        private void UpdateOvrSkeleton()
        {
            if (OVRSkeleton_skeletonType_Field != null)
            {
                OVRSkeleton_skeletonType_Field.SetValue(m_ovrSkeleton, m_isLeftHand ? OVRSkeleton.SkeletonType.HandLeft : OVRSkeleton.SkeletonType.HandRight);
            }
            else
            {
                Debug.LogError("Failed to update OvrSkeleton: OVRSkeleton_skeletonType_Field not found");
            }
        }

        private static FieldInfo OVRMesh_meshType_Field = typeof(OVRMesh).GetField("_meshType", BindingFlags.NonPublic | BindingFlags.Instance);
        private void UpdateOvrMesh()
        {
            if (OVRMesh_meshType_Field != null)
            {
                OVRMesh_meshType_Field.SetValue(m_ovrMesh, m_isLeftHand ? OVRMesh.MeshType.HandLeft : OVRMesh.MeshType.HandRight);
            }
            else
            {
                Debug.LogError("Failed to update OvrMesh: OVRMesh_meshType_Field not found");
            }
        }
#else
        public static readonly bool SUPPORTED = false;
        public void Initialize(bool isLeftHand) { }
        public void SetHand(bool isLeftHand) { }
#endif
    }
}