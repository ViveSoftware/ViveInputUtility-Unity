//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using HTC.UnityPlugin.Utility;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive.ExCamConfigInterface
{
    public class ExCamConfigInterfacePanelController : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_recenterButton;
        [SerializeField]
        private GameObject m_dirtySymbol;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_posX;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_posY;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_posZ;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_rotX;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_rotY;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_rotZ;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_ckR;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_ckG;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_ckB;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_ckA;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_fov;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_clipNear;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_clipFar;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_offsetNear;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_offsetFar;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_offsetHMD;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_frameSkip;
        [SerializeField]
        private ExCamConfigInterfaceDraggableLabel m_sceneResolutionScale;
        [SerializeField]
        private Toggle m_diableStandardAssets;

#if VIU_STEAMVR && UNITY_STANDALONE
        public float posX
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.x;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.x = value;

                    var pos = cam.transform.localPosition;
                    pos.x = value;
                    cam.transform.localPosition = pos;
                }
            }
        }

        public float posY
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.y;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.y = value;

                    var pos = cam.transform.localPosition;
                    pos.y = value;
                    cam.transform.localPosition = pos;
                }
            }
        }

        public float posZ
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.z;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.z = value;

                    var pos = cam.transform.localPosition;
                    pos.z = value;
                    cam.transform.localPosition = pos;
                }
            }
        }

        public float rotX
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.rx;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.rx = value;

                    var rot = cam.transform.localEulerAngles;
                    rot.x = value;
                    cam.transform.localEulerAngles = rot;
                }
            }
        }

        public float rotY
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.ry;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.ry = value;

                    var rot = cam.transform.localEulerAngles;
                    rot.y = value;
                    cam.transform.localEulerAngles = rot;
                }
            }
        }

        public float rotZ
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.rz;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.rz = value;

                    var rot = cam.transform.localEulerAngles;
                    rot.z = value;
                    cam.transform.localEulerAngles = rot;
                }
            }
        }

        public float fov
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.fov;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                Camera cam;
                if (TryGetTargetExCam(out excam, out cam))
                {
                    excam.config.fov = value;

                    cam.fieldOfView = value;
                }
            }
        }

        public float sceneResolutionScale
        {
            get
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    return excam.config.sceneResolutionScale;
                }
                else
                {
                    return 0f;
                }
            }
            set
            {
                SteamVR_ExternalCamera excam;
                if (TryGetTargetExCam(out excam))
                {
                    excam.config.sceneResolutionScale = value;
                    SteamVR_Camera.sceneResolutionScale = value;
                }
            }
        }
#if VIU_STEAMVR_1_2_2_OR_NEWER
        public float ckR { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.r : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.r = value; } } }
        public float ckG { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.g : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.g = value; } } }
        public float ckB { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.b : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.b = value; } } }
        public float ckA { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.a : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.a = value; } } }
#else
        public float ckR { get { return 0f; } set { } }
        public float ckG { get { return 0f; } set { } }
        public float ckB { get { return 0f; } set { } }
        public float ckA { get { return 0f; } set { } }
#endif
        public float clipNear { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.near : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.near = value; } } }
        public float clipFar { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.far : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.far = value; } } }
        public float offsetNear { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.nearOffset : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.nearOffset = value; } } }
        public float offsetFar { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.farOffset : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.farOffset = value; } } }
        public float offsetHMD { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.hmdOffset : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.hmdOffset = value; } } }
        public float frameSkip { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.frameSkip : 0f; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.frameSkip = value; } } }
        public bool diableStandardAssets { get { SteamVR_ExternalCamera excam; return TryGetTargetExCam(out excam) ? excam.config.disableStandardAssets : false; } set { SteamVR_ExternalCamera excam; if (TryGetTargetExCam(out excam)) { excam.config.disableStandardAssets = value; } } }

        public void SaveConfig()
        {
            SteamVR_ExternalCamera excam;
            if (TryGetTargetExCam(out excam) && !string.IsNullOrEmpty(excam.configPath))
            {
                try
                {
                    using (var outputFile = new StreamWriter(excam.configPath))
                    {
                        var configType = typeof(SteamVR_ExternalCamera.Config);
                        var config = excam.config;
                        foreach (var fieldInfo in configType.GetFields())
                        {
                            outputFile.WriteLine(fieldInfo.Name + "=" + fieldInfo.GetValue(config).ToString());
                        }
                    }
                }
                catch { }
            }
        }

        public void ReloadConfig()
        {
            SteamVR_ExternalCamera excam;
            if (TryGetTargetExCam(out excam))
            {
                excam.config = default(SteamVR_ExternalCamera.Config);
                excam.ReadConfig();

                ReloadFields();

                // sceneResolutionScale only update on SteamVR_ExternalCamera Enabled/Disabled
                SteamVR_Camera.sceneResolutionScale = sceneResolutionScale;
            }
        }

        private bool TryGetTargetExCam(out SteamVR_ExternalCamera excam)
        {
            if (!ExternalCameraHook.Active || ExternalCameraHook.Instance.externalCamera == null)
            {
                excam = null;
                return false;
            }
            else
            {
                excam = ExternalCameraHook.Instance.externalCamera;
                return true;
            }
        }

        private bool TryGetTargetExCam(out SteamVR_ExternalCamera excam, out Camera camera)
        {
            if (!TryGetTargetExCam(out excam))
            {
                camera = null;
                return false;
            }

            excam = ExternalCameraHook.Instance.externalCamera;
            var excamTrans = excam.transform;

            if (excamTrans.childCount <= 1)
            {
                camera = excam.GetComponentInChildren<Camera>();
            }
            else
            {
                // Locate the camera component on the last child and clean up other duplicated cameras
                // Note that SteamVR_ExternalCamera.ReadConfig triggers making a new clone from head camera
                // And ReadConfig is called when externalcamera.cfg is changed on disk
                var duplicateCamsObj = ListPool<GameObject>.Get();

                camera = null;
                for (int i = excamTrans.childCount - 1; i >= 0; --i)
                {
                    var cam = excamTrans.GetChild(i).GetComponent<Camera>();
                    if (cam == null) { continue; }

                    if (camera == null)
                    {
                        camera = cam;
                    }
                    else
                    {
                        duplicateCamsObj.Add(cam.gameObject);
                    }
                }

                for (int i = duplicateCamsObj.Count - 1; i >= 0; --i)
                {
                    Destroy(duplicateCamsObj[i]);
                }

                ListPool<GameObject>.Release(duplicateCamsObj);
            }

            return true;
        }

        public void RecenterExternalCameraPose()
        {
            SteamVR_ExternalCamera excam;
            Camera cam;
            if (TryGetTargetExCam(out excam, out cam))
            {
                Vector3 recenteredPos;
                Vector3 recenteredRot;

                var origin = ExternalCameraHook.Instance.origin;
                var root = origin != null ? origin : ExternalCameraHook.Instance.transform.parent;

                if (root == null)
                {
                    recenteredPos = cam.transform.position;
                    recenteredRot = root.eulerAngles;
                }
                else
                {
                    recenteredPos = root.InverseTransformPoint(cam.transform.position);
                    recenteredRot = (cam.transform.rotation * Quaternion.Inverse(root.rotation)).eulerAngles;
                }

                posX = recenteredPos.x;
                posY = recenteredPos.y;
                posZ = recenteredPos.z;
                rotX = recenteredRot.x;
                rotY = recenteredRot.y;
                rotZ = recenteredRot.z;

                ReloadFields();

                ExternalCameraHook.Instance.Recenter();
            }

            m_recenterButton.gameObject.SetActive(false);
        }
#else
        public float posX { get; set; }
        public float posY { get; set; }
        public float posZ { get; set; }
        public float rotX { get; set; }
        public float rotY { get; set; }
        public float rotZ { get; set; }
        public float ckR { get; set; }
        public float ckG { get; set; }
        public float ckB { get; set; }
        public float ckA { get; set; }
        public float fov { get; set; }
        public float clipNear { get; set; }
        public float clipFar { get; set; }
        public float offsetNear { get; set; }
        public float offsetFar { get; set; }
        public float offsetHMD { get; set; }
        public float frameSkip { get; set; }
        public float sceneResolutionScale { get; set; }
        public bool diableStandardAssets { get; set; }

        public void SaveConfig() { }

        public void ReloadConfig() { }
        
        public void RecenterExternalCameraPose() { }
#endif
        private ViveRoleProperty m_exCamViveRole;

        private void Awake()
        {
            if (EventSystem.current == null)
            {
                new GameObject("[EventSystem]", typeof(EventSystem)).AddComponent<StandaloneInputModule>();
            }
            else if (EventSystem.current.GetComponent<StandaloneInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
            }

#if !VIU_STEAMVR_1_2_2_OR_NEWER
            if (m_ckR != null)
            {
                m_ckR.transform.parent.gameObject.SetActive(false);
            }
#endif

#if UNITY_5_4_OR_NEWER
            // Disable the camera HMD tracking
            GetComponent<Canvas>().worldCamera.stereoTargetEye = StereoTargetEyeMask.None;
#endif

            // Force update layout
            transform.parent.gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            ReloadFields();

            if (m_exCamViveRole == null && ExternalCameraHook.Active)
            {
                m_exCamViveRole = ExternalCameraHook.Instance.viveRole;
                m_exCamViveRole.onDeviceIndexChanged += OnDeviceIndexChanged;
            }

            UpdateRecenterButtonVisible();
        }

        private void OnDisable()
        {
            if (m_exCamViveRole != null)
            {
                m_exCamViveRole.onDeviceIndexChanged -= OnDeviceIndexChanged;
                m_exCamViveRole = null;
            }
        }

        private void ReloadFields()
        {
            m_posX.fieldValue = posX;
            m_posY.fieldValue = posY;
            m_posZ.fieldValue = posZ;
            m_rotX.fieldValue = rotX;
            m_rotY.fieldValue = rotY;
            m_rotZ.fieldValue = rotZ;
            m_ckR.fieldValue = ckR;
            m_ckG.fieldValue = ckG;
            m_ckB.fieldValue = ckB;
            m_ckA.fieldValue = ckA;
            m_fov.fieldValue = fov;
            m_clipNear.fieldValue = clipNear;
            m_clipFar.fieldValue = clipFar;
            m_offsetNear.fieldValue = offsetNear;
            m_offsetFar.fieldValue = offsetFar;
            m_offsetHMD.fieldValue = offsetHMD;
            m_frameSkip.fieldValue = frameSkip;
            m_sceneResolutionScale.fieldValue = sceneResolutionScale;
            m_diableStandardAssets.isOn = diableStandardAssets;

            m_dirtySymbol.gameObject.SetActive(false);
        }

        private void OnDeviceIndexChanged(uint deviceIndex) { UpdateRecenterButtonVisible(); }

        private void UpdateRecenterButtonVisible()
        {
            if (!isActiveAndEnabled || m_recenterButton == null) { return; }

            if (ExternalCameraHook.Instance.isTrackingDevice)
            {
                m_recenterButton.gameObject.SetActive(false);
            }
            else
            {
                bool needRecenter;
                var origin = ExternalCameraHook.Instance.origin;
                if (origin == null)
                {
                    needRecenter = new RigidPose(ExternalCameraHook.Instance.transform, false) != RigidPose.identity;
                }
                else
                {
                    needRecenter = new RigidPose(ExternalCameraHook.Instance.transform, false) != new RigidPose(origin, false);
                }

                m_recenterButton.gameObject.SetActive(needRecenter);
            }
        }
    }
}