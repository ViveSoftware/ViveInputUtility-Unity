//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#if VIU_OCULUSVR
using HTC.UnityPlugin.Utility;
using System;
using System.Reflection;
using UnityEngine;

namespace HTC.UnityPlugin.Vive.OculusVRExtension
{
    public class OculusHandRenderModel : MonoBehaviour
    {
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

                FieldInfo updateRootScaleField = m_ovrSkeleton.GetType().GetField("_updateRootScale", BindingFlags.NonPublic | BindingFlags.Instance);
                updateRootScaleField.SetValue(m_ovrSkeleton, true);

                FieldInfo updateRootPoseField = m_ovrSkeleton.GetType().GetField("_updateRootPose", BindingFlags.NonPublic | BindingFlags.Instance);
                updateRootPoseField.SetValue(m_ovrSkeleton, true);

                MethodInfo skeletonInitializeMethod = m_ovrSkeleton.GetType().GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
                skeletonInitializeMethod.Invoke(m_ovrSkeleton, new object[] { });

                m_ovrMesh = gameObject.AddComponent<OVRMesh>();
                UpdateOvrMesh();

                MethodInfo meshInitializeMethod = m_ovrMesh.GetType().GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
                meshInitializeMethod.Invoke(m_ovrMesh, new object[] { m_isLeftHand ? OVRMesh.MeshType.HandLeft : OVRMesh.MeshType.HandRight });

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

        private void UpdateOvrHand()
        {
            try
            {
                FieldInfo handTypeField = m_ovrHand.GetType().GetField("HandType", BindingFlags.NonPublic | BindingFlags.Instance);
                handTypeField.SetValue(m_ovrHand, m_isLeftHand ? OVRHand.Hand.HandLeft : OVRHand.Hand.HandRight);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to update OVRHand: " + e);
            }
        }

        private void UpdateOvrSkeleton()
        {
            try
            {
                FieldInfo skeletonTypeField = m_ovrSkeleton.GetType().GetField("_skeletonType", BindingFlags.NonPublic | BindingFlags.Instance);
                skeletonTypeField.SetValue(m_ovrSkeleton, m_isLeftHand ? OVRSkeleton.SkeletonType.HandLeft : OVRSkeleton.SkeletonType.HandRight);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to update OVRHand: " + e);
            }
        }

        private void UpdateOvrMesh()
        {
            try
            {
                FieldInfo meshTypeField = m_ovrMesh.GetType().GetField("_meshType", BindingFlags.NonPublic | BindingFlags.Instance);
                meshTypeField.SetValue(m_ovrMesh, m_isLeftHand ? OVRMesh.MeshType.HandLeft : OVRMesh.MeshType.HandRight);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to update OVRHand: " + e);
            }
        }
    }
}
#endif