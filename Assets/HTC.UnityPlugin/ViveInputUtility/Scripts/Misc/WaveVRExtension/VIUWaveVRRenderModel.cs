//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
#pragma warning disable 0414
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.VRModuleManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
using Wave.Native;
#endif

namespace HTC.UnityPlugin.Vive.WaveVRExtension
{
    // Only works in playing mode
    public class VIUWaveVRRenderModel : MonoBehaviour
    {
        private struct ChildTransforms
        {
            public Transform root;
            public Transform attach;
        }

        // Name of the sub-object which represents the "local" coordinate space for each component.
        public const string LOCAL_TRANSFORM_NAME = "attach";

        public const string MODEL_OVERRIDE_WARNNING = "Model override is really only meant to be used in " +
            "the scene view for lining things up.  Use tracked device " +
            "index instead to ensure the correct model is displayed for all users.";

        private const uint LEFT_INDEX = 1;
        private const uint RIGHT_INDEX = 2;

        [Tooltip(MODEL_OVERRIDE_WARNNING)]
        [SerializeField]
        private string m_modelOverride;

        [Tooltip("Shader to apply to model.")]
        [SerializeField]
        private Shader m_shaderOverride;

        [Tooltip("Update transforms of components at runtime to reflect user action.")]
        [SerializeField]
        private bool m_updateDynamically = true;

        private uint m_deviceIndex = VRModule.INVALID_DEVICE_INDEX;
        private MeshFilter m_meshFilter;
        private MeshRenderer m_meshRenderer;
        private IndexedTable<string, ChildTransforms> m_chilTransforms = new IndexedTable<string, ChildTransforms>();
        private IndexedTable<int, Material> m_materials = new IndexedTable<int, Material>();
        private HashSet<string> m_loadingRenderModels = new HashSet<string>();
        private bool m_isAppQuit;
        private WaitForEndOfFrame wfef = null;
        private GameObject[] childArray;
        private bool[] showState;
        private GameObject meshCom = null;
        private GameObject meshGO = null;
        private Mesh updateMesh;
        private List<Color32> colors = new List<Color32>();
        private Material ImgMaterial;
        private GameObject batteryGO = null;
        private MeshRenderer batteryMR = null;
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
        private ModelResource modelResource = new ModelResource();
        private BatteryIndicator currentBattery;
        private MeshObject[] pressObjectArrays = new MeshObject[11];
#endif
        private Material effectMat;
        private Color buttonEffectColor = new Color(0, 179, 227, 255);

        private static readonly string[] PressEffectNames = new string[] {
			//"__CM__HomeButton", // WVR_InputId_Alias1_System
			"__CM__AppButton", // WVR_InputId_Alias1_Menu
			"__CM__Grip", // WVR_InputId_Alias1_Grip
			//"__CM__DPad_Left", // DPad_Left
			//"__CM__DPad_Up", // DPad_Up
			//"__CM__DPad_Right", // DPad_Right
			//"__CM__DPad_Down", // DPad_Down
			//"__CM__VolumeUp", // VolumeUpKey
			//"__CM__VolumeDown", // VolumeDownKey
			"__CM__DigitalTriggerKey", // BumperKey in DS < 3.2
			"__CM__TouchPad", // TouchPad_Press
			"__CM__TriggerKey", // TriggerKey
			"__CM__ButtonA", // ButtonA
			"__CM__ButtonB", // ButtonB
			"__CM__ButtonX", // ButtonX
			"__CM__ButtonY", // ButtonY
			//"__CM__VolumeKey", // Volume
			//"__CM__VolumeKey", // Volume
			"__CM__BumperKey", // BumperKey in DS >= 3.2
			"__CM__Thumbstick", // Thumbstick
		};

        private string preferedModelName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_modelOverride)) { return m_modelOverride; }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return string.Empty;
                }
                else
#endif
                {
                    return VRModule.GetCurrentDeviceState(m_deviceIndex).renderModelName;
                }
            }
        }

        private Shader preferedShader { get { return m_shaderOverride == null ? Shader.Find("Standard") : m_shaderOverride; } }

        public bool updateDynamically { get { return m_updateDynamically; } set { m_updateDynamically = value; } }
        public bool isLoadingModel { get { return m_loadingRenderModels.Count > 0; } }
        public string loadedModelName { get; private set; }
        public bool isModelLoaded { get { return !string.IsNullOrEmpty(loadedModelName); } }
        public Shader loadedShader { get; private set; }

        public string modelOverride
        {
            get
            {
                return m_modelOverride;
            }
            set
            {
                m_modelOverride = value;
                LoadPreferedModel();
            }
        }

        public Shader shaderOverride
        {
            get
            {
                return m_shaderOverride;
            }
            set
            {
                m_shaderOverride = value;
                SetPreferedShader();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (!m_isAppQuit && this != null && isActiveAndEnabled)
                    {
                        LoadPreferedModel();
                        SetPreferedShader();
                    }
                };
            }
        }
#endif

        private void Update()
        {
            if (m_updateDynamically)
            {
#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL && UNITY_ANDROID
                updateBatteryLevel();
                CollectEffectObjects();

                var deviceState = VRModule.GetDeviceState(m_deviceIndex);

                if (preferedModelName.Contains("Right"))
                {
                    if (deviceState.GetButtonPress(VRModuleRawButton.Grip))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[0].hasEffect);
                        pressObjectArrays[1].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[1].effectMat;
                        pressObjectArrays[1].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[1].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[1].originMat;
                        pressObjectArrays[1].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.Trigger))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[4].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[4].effectMat;
                        pressObjectArrays[4].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[4].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[4].originMat;
                        pressObjectArrays[4].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.A))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[5].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[5].effectMat;
                        pressObjectArrays[5].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[5].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[5].originMat;
                        pressObjectArrays[5].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.ApplicationMenu))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[6].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[6].effectMat;
                        pressObjectArrays[6].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[6].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[6].originMat;
                        pressObjectArrays[6].gameObject.SetActive(false);
                    }
                }

                if (preferedModelName.Contains("Left"))
                {

                    if (deviceState.GetButtonPress(VRModuleRawButton.System))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[0].hasEffect);
                        pressObjectArrays[0].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[0].effectMat;
                        pressObjectArrays[0].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[0].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[0].originMat;
                        pressObjectArrays[0].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.Grip))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[0].hasEffect);
                        pressObjectArrays[1].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[1].effectMat;
                        pressObjectArrays[1].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[1].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[1].originMat;
                        pressObjectArrays[1].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.Trigger))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[4].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[4].effectMat;
                        pressObjectArrays[4].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[4].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[4].originMat;
                        pressObjectArrays[4].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.A))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[7].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[7].effectMat;
                        pressObjectArrays[7].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[7].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[7].originMat;
                        pressObjectArrays[7].gameObject.SetActive(false);
                    }

                    if (deviceState.GetButtonPress(VRModuleRawButton.ApplicationMenu))
                    {
                        //Debug.Log("hasEffect: " + pressObjectArrays[4].hasEffect);
                        pressObjectArrays[8].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[8].effectMat;
                        pressObjectArrays[8].gameObject.SetActive(true);
                    }
                    else
                    {
                        pressObjectArrays[8].gameObject.GetComponent<MeshRenderer>().material = pressObjectArrays[8].originMat;
                        pressObjectArrays[8].gameObject.SetActive(false);
                    }
                }
#endif
            }
        }

#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
        private void CollectEffectObjects() // collect controller object which has effect
        {
            if (preferedModelName.Contains("Right"))
            {
                Debug.Log("[DEBUG] load Materials/WaveColorOffsetMatR");
                effectMat = Resources.Load("Materials/WaveColorOffsetMatR") as Material;
            }
            else
            {
                Debug.Log("[DEBUG] load Materials/WaveColorOffsetMatL");
                effectMat = Resources.Load("Materials/WaveColorOffsetMatL") as Material;
            }
            //touchMat = new Material(Shader.Find("Unlit/Texture"));
            //if (useSystemConfig)
            //{
            //    Debug.Log("use system config in controller model!");
            //    ReadJsonValues();
            //}
            //else
            //{
            //    Log.w(LOG_TAG, "use custom config in controller model!");
            //}

            var ch = this.transform.childCount;
            //Debug.Log("[DEBUG] childCount: " + ch);
            effectMat.color = buttonEffectColor;

            //RenderModel wrm = this.GetComponent<RenderModel>();

            //if (wrm != null)
            //{
            //    mergeToOneBone = wrm.mergeToOneBone;
            //}
            //else
            //{
            //    mergeToOneBone = false;
            //}

            //isTouchPadSetting = GetTouchPadParam();

            for (var j = 0; j < PressEffectNames.Length; j++)
            {
                pressObjectArrays[j] = new MeshObject();
                pressObjectArrays[j].MeshName = PressEffectNames[j];
                pressObjectArrays[j].hasEffect = false;
                pressObjectArrays[j].gameObject = null;
                pressObjectArrays[j].originPosition = new Vector3(0, 0, 0);
                pressObjectArrays[j].originMat = null;
                pressObjectArrays[j].effectMat = null;

                for (int i = 0; i < ch; i++)
                {
                    GameObject CM = this.transform.GetChild(i).gameObject;
                    string[] t = CM.name.Split("."[0]);
                    var childname = t[0];
                    if (pressObjectArrays[j].MeshName == childname)
                    {
                        pressObjectArrays[j].gameObject = CM;
                        pressObjectArrays[j].originPosition = CM.transform.localPosition;
                        pressObjectArrays[j].originMat = CM.GetComponent<MeshRenderer>().material;
                        pressObjectArrays[j].effectMat = effectMat;
                        pressObjectArrays[j].hasEffect = true;

                        break;
                    }
                }

                //Debug.Log("Press " + pressObjectArrays[j].MeshName + " has effect: " + pressObjectArrays[j].hasEffect);
            }
        }
#endif

        private void OnEnable()
        {
            LoadPreferedModel();
        }

        private void OnDestroy()
        {
            ClearModel();
        }

        private void OnApplicationQuit()
        {
            m_isAppQuit = true;
        }

        public void ClearModel()
        {
            if (!isModelLoaded) { return; }
            if (m_meshRenderer != null) { Destroy(m_meshRenderer); }
            if (m_meshFilter != null) { Destroy(m_meshFilter); }

            for (int i = 0, imax = m_chilTransforms.Count; i < imax; ++i)
            {
                var c = m_chilTransforms.GetValueByIndex(i);
                if (c.root == null) { continue; }
                Destroy(c.root.gameObject);
            }

            m_chilTransforms.Clear();
            m_materials.Clear();
            loadedModelName = string.Empty;
            loadedShader = null;
        }

        private void SetPreferedShader()
        {
            SetShader(preferedShader);
        }

        private void SetShader(Shader newShader)
        {
            if (loadedShader == newShader) { return; }

            loadedShader = newShader;

            if (m_materials == null) { return; }

            for (int i = 0, imax = m_materials.Count; i < imax; ++i)
            {
                var mat = m_materials.GetValueByIndex(i);
                if (mat != null)
                {
                    mat.shader = newShader;
                }
            }
        }

        private void LoadPreferedModel()
        {
            LoadModel(preferedModelName);
        }

        private void LoadModel(string renderModelName)
        {
            Debug.Log(transform.parent.parent.name + " Try LoadModel " + renderModelName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("LoadModel failed! This function only works in playing mode");
                return;
            }
#endif
            if (string.IsNullOrEmpty(loadedModelName) && string.IsNullOrEmpty(renderModelName)) { return; }

            if (loadedModelName == renderModelName) { return; }

            if (m_loadingRenderModels.Contains(renderModelName)) { return; }

            ClearModel();

            if (!m_isAppQuit && !string.IsNullOrEmpty(renderModelName))
            {
                Debug.Log(transform.parent.parent.name + " LoadModel " + renderModelName);
                m_loadingRenderModels.Add(renderModelName);

#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
                IntPtr ctrlModel = IntPtr.Zero;
                int IntBits = IntPtr.Size;

                WVR_Result r = Interop.WVR_GetCurrentControllerModel(preferedModelName.Contains("Left") ? WVR_DeviceType.WVR_DeviceType_Controller_Left : WVR_DeviceType.WVR_DeviceType_Controller_Right, ref ctrlModel, true);

                if (r == WVR_Result.WVR_Success)
                {
                    if (ctrlModel != IntPtr.Zero)
                    {
                        WVR_CtrlerModel ctrl = (WVR_CtrlerModel)Marshal.PtrToStructure(ctrlModel, typeof(WVR_CtrlerModel));

                        //Debug.Log("render model name = " + ctrl.name + " , load from asset = " + ctrl.loadFromAsset);

                        WVR_CtrlerCompInfoTable cit = ctrl.compInfos;

                        int szStruct = Marshal.SizeOf(typeof(WVR_CtrlerCompInfo));

                        //Debug.Log("Controller component size = " + cit.size);

                        modelResource.FBXInfo = new FBXInfo_t[cit.size];
                        modelResource.sectionCount = cit.size;
                        modelResource.SectionInfo = new MeshInfo_t[cit.size];
                        modelResource.loadFromAsset = ctrl.loadFromAsset;

                        for (int i = 0; i < cit.size; i++)
                        {
                            WVR_CtrlerCompInfo wcci;

                            if (IntBits == 4)
                                wcci = (WVR_CtrlerCompInfo)Marshal.PtrToStructure(new IntPtr(cit.table.ToInt32() + (szStruct * i)), typeof(WVR_CtrlerCompInfo));
                            else
                                wcci = (WVR_CtrlerCompInfo)Marshal.PtrToStructure(new IntPtr(cit.table.ToInt64() + (szStruct * i)), typeof(WVR_CtrlerCompInfo));

                            modelResource.FBXInfo[i] = new FBXInfo_t();
                            modelResource.SectionInfo[i] = new MeshInfo_t();

                            modelResource.FBXInfo[i].meshName = Marshal.StringToHGlobalAnsi(wcci.name);
                            modelResource.SectionInfo[i]._active = wcci.defaultDraw;

                            //Debug.Log("Controller component name = " + wcci.name + ", tex index = " + wcci.texIndex + ", active= " + modelResource.SectionInfo[i]._active);

                            // local matrix
                            //Matrix4x4 lt = RigidTransform.toMatrix44(wcci.localMat, false);
                            //Matrix4x4 t = RigidTransform.RowColumnInverse(lt);
                            //Debug.Log(" matrix = (" + t.m00 + ", " + t.m01 + ", " + t.m02 + ", " + t.m03 + ")");
                            //Debug.Log(" matrix = (" + t.m10 + ", " + t.m11 + ", " + t.m12 + ", " + t.m13 + ")");
                            //Debug.Log(" matrix = (" + t.m20 + ", " + t.m21 + ", " + t.m22 + ", " + t.m23 + ")");
                            //Debug.Log(" matrix = (" + t.m30 + ", " + t.m31 + ", " + t.m32 + ", " + t.m33 + ")");

                            //curr.FBXInfo[i].matrix = RigidTransform.ToWVRMatrix(t, false);

                            WVR_VertexBuffer vertices = wcci.vertices;

                            if (vertices.dimension == 3)
                            {
                                uint verticesCount = (vertices.size / vertices.dimension);

                                //Debug.Log(" vertices size = " + vertices.size + ", dimension = " + vertices.dimension + ", count = " + verticesCount);

                                modelResource.SectionInfo[i]._vectice = new Vector3[verticesCount];
                                float[] verticeArray = new float[vertices.size];

                                Marshal.Copy(vertices.buffer, verticeArray, 0, verticeArray.Length);

                                int verticeIndex = 0;
                                int floatIndex = 0;

                                while (verticeIndex < verticesCount)
                                {
                                    modelResource.SectionInfo[i]._vectice[verticeIndex] = new Vector3();
                                    modelResource.SectionInfo[i]._vectice[verticeIndex].x = verticeArray[floatIndex++];
                                    modelResource.SectionInfo[i]._vectice[verticeIndex].y = verticeArray[floatIndex++];
                                    modelResource.SectionInfo[i]._vectice[verticeIndex].z = verticeArray[floatIndex++] * -1f;

                                    verticeIndex++;
                                }
                            }
                            else
                            {
                                Debug.Log("[WARNING] vertices buffer's dimension incorrect!");
                            }

                            // normals
                            WVR_VertexBuffer normals = wcci.normals;

                            if (normals.dimension == 3)
                            {
                                uint normalsCount = (normals.size / normals.dimension);
                                //Debug.Log(" normals size = " + normals.size + ", dimension = " + normals.dimension + ", count = " + normalsCount);
                                modelResource.SectionInfo[i]._normal = new Vector3[normalsCount];
                                float[] normalArray = new float[normals.size];

                                Marshal.Copy(normals.buffer, normalArray, 0, normalArray.Length);

                                int normalsIndex = 0;
                                int floatIndex = 0;

                                while (normalsIndex < normalsCount)
                                {
                                    modelResource.SectionInfo[i]._normal[normalsIndex] = new Vector3();
                                    modelResource.SectionInfo[i]._normal[normalsIndex].x = normalArray[floatIndex++];
                                    modelResource.SectionInfo[i]._normal[normalsIndex].y = normalArray[floatIndex++];
                                    modelResource.SectionInfo[i]._normal[normalsIndex].z = normalArray[floatIndex++];

                                    normalsIndex++;
                                }

                                //Debug.Log(" normals size = " + normals.size + ", dimension = " + normals.dimension + ", count = " + normalsCount);
                            }
                            else
                            {
                                Debug.Log("[WARNING] normals buffer's dimension incorrect!");
                            }

                            // texCoord
                            WVR_VertexBuffer texCoord = wcci.texCoords;

                            if (texCoord.dimension == 2)
                            {
                                uint uvCount = (texCoord.size / texCoord.dimension);
                                //Debug.Log(" texCoord size = " + texCoord.size + ", dimension = " + texCoord.dimension + ", count = " + uvCount);
                                modelResource.SectionInfo[i]._uv = new Vector2[uvCount];
                                float[] texCoordArray = new float[texCoord.size];

                                Marshal.Copy(texCoord.buffer, texCoordArray, 0, texCoordArray.Length);

                                int uvIndex = 0;
                                int floatIndex = 0;

                                while (uvIndex < uvCount)
                                {
                                    modelResource.SectionInfo[i]._uv[uvIndex] = new Vector2();
                                    modelResource.SectionInfo[i]._uv[uvIndex].x = texCoordArray[floatIndex++];
                                    modelResource.SectionInfo[i]._uv[uvIndex].y = texCoordArray[floatIndex++];

                                    uvIndex++;
                                }
                            }
                            else
                            {
                                Debug.Log("[WARNING] normals buffer's dimension incorrect!");
                            }

                            // indices
                            WVR_IndexBuffer indices = wcci.indices;
                            //Debug.Log(" indices size = " + indices.size);

                            modelResource.SectionInfo[i]._indice = new int[indices.size];
                            Marshal.Copy(indices.buffer, modelResource.SectionInfo[i]._indice, 0, modelResource.SectionInfo[i]._indice.Length);

                            uint indiceIndex = 0;

                            while (indiceIndex < indices.size)
                            {
                                int tmp = modelResource.SectionInfo[i]._indice[indiceIndex];
                                modelResource.SectionInfo[i]._indice[indiceIndex] = modelResource.SectionInfo[i]._indice[indiceIndex + 2];
                                modelResource.SectionInfo[i]._indice[indiceIndex + 2] = tmp;
                                indiceIndex += 3;
                            }
                        }

                        // Controller texture section
                        WVR_CtrlerTexBitmapTable wctbt = ctrl.bitmapInfos;
                        //Debug.Log("Controller textures = " + wctbt.size);
                        int bmStruct = Marshal.SizeOf(typeof(WVR_CtrlerTexBitmap));
                        modelResource.modelTextureCount = (int)wctbt.size;
                        modelResource.modelTextureInfo = new TextureInfo[wctbt.size];
                        modelResource.modelTexture = new Texture2D[wctbt.size];

                        for (int mt = 0; mt < wctbt.size; mt++)
                        {
                            TextureInfo ct = new TextureInfo();

                            WVR_CtrlerTexBitmap wctb;

                            if (IntBits == 4)
                                wctb = (WVR_CtrlerTexBitmap)Marshal.PtrToStructure(new IntPtr(wctbt.table.ToInt32() + (bmStruct * mt)), typeof(WVR_CtrlerTexBitmap));
                            else
                                wctb = (WVR_CtrlerTexBitmap)Marshal.PtrToStructure(new IntPtr(wctbt.table.ToInt64() + (bmStruct * mt)), typeof(WVR_CtrlerTexBitmap));

                            //Debug.Log(" [" + mt + "] bitmap width = " + wctb.width);
                            //Debug.Log(" [" + mt + "] bitmap height = " + wctb.height);
                            //Debug.Log(" [" + mt + "] bitmap stride = " + wctb.stride);
                            //Debug.Log(" [" + mt + "] bitmap format = " + wctb.format);
                            // bitmap size
                            var rawImageSize = wctb.height * wctb.stride;

                            ct.modelTextureData = new byte[rawImageSize];
                            Marshal.Copy(wctb.bitmap, ct.modelTextureData, 0, ct.modelTextureData.Length);
                            ct.width = (int)wctb.width;
                            ct.height = (int)wctb.height;
                            ct.stride = (int)wctb.stride;
                            ct.format = (int)wctb.format;
                            ct.size = (int)rawImageSize;

                            modelResource.modelTextureInfo[mt] = ct;
                        }

                        // Touchpad section
                        //Debug.Log("[DEBUG] ---  Get touch info from runtime  ---");
                        WVR_TouchPadPlane wtpp = ctrl.touchpadPlane;

                        modelResource.TouchSetting = new TouchSetting();
                        modelResource.TouchSetting.touchCenter.x = wtpp.center.v0 * 100f;
                        modelResource.TouchSetting.touchCenter.y = wtpp.center.v1 * 100f;
                        modelResource.TouchSetting.touchCenter.z = (-1.0f * wtpp.center.v2) * 100f;
                        //Debug.Log(" touchCenter! x: " + modelResource.TouchSetting.touchCenter.x + " ,y: " + modelResource.TouchSetting.touchCenter.y + " ,z: " + modelResource.TouchSetting.touchCenter.z);

                        modelResource.TouchSetting.raidus = wtpp.radius * 100;

                        modelResource.TouchSetting.touchptHeight = wtpp.floatingDistance * 100;

                        modelResource.isTouchSetting = wtpp.valid;

                        modelResource.TouchSetting.touchPtU.x = wtpp.u.v0;
                        modelResource.TouchSetting.touchPtU.y = wtpp.u.v1;
                        modelResource.TouchSetting.touchPtU.z = wtpp.u.v2;

                        modelResource.TouchSetting.touchPtV.x = wtpp.v.v0;
                        modelResource.TouchSetting.touchPtV.y = -1.0f * wtpp.v.v1;
                        modelResource.TouchSetting.touchPtV.z = wtpp.v.v2;

                        modelResource.TouchSetting.touchPtW.x = wtpp.w.v0;
                        modelResource.TouchSetting.touchPtW.y = wtpp.w.v1;
                        modelResource.TouchSetting.touchPtW.z = -1.0f * wtpp.w.v2;
                        //Debug.Log(" Floating distance : " + modelResource.TouchSetting.touchptHeight);

                        //Debug.Log(" touchPtW! x: " + modelResource.TouchSetting.touchPtW.x + " ,y: " + modelResource.TouchSetting.touchPtW.y + " ,z: " + modelResource.TouchSetting.touchPtW.z);
                        //Debug.Log(" touchPtU! x: " + modelResource.TouchSetting.touchPtU.x + " ,y: " + modelResource.TouchSetting.touchPtU.y + " ,z: " + modelResource.TouchSetting.touchPtU.z);
                        //Debug.Log(" touchPtV! x: " + modelResource.TouchSetting.touchPtV.x + " ,y: " + modelResource.TouchSetting.touchPtV.y + " ,z: " + modelResource.TouchSetting.touchPtV.z);
                        //Debug.Log(" raidus: " + modelResource.TouchSetting.raidus);
                        //Debug.Log(" isTouchSetting: " + modelResource.isTouchSetting);

                        // Battery section

                        //Debug.Log("[DEBUG] ---  Get battery info from runtime  ---");
                        WVR_BatteryLevelTable wblt = ctrl.batteryLevels;

                        List<BatteryIndicator> batteryTextureList = new List<BatteryIndicator>();
                        modelResource.batteryTextureList = batteryTextureList;

                        //Debug.Log("Battery levels = " + wblt.size);

                        int btStruct = Marshal.SizeOf(typeof(WVR_CtrlerTexBitmap));
                        int sizeInt = Marshal.SizeOf(typeof(int));

                        for (int b = 0; b < wblt.size; b++)
                        {
                            WVR_CtrlerTexBitmap batteryImage;
                            int batteryMin = 0;
                            int batteryMax = 0;

                            if (IntBits == 4)
                            {
                                batteryImage = (WVR_CtrlerTexBitmap)Marshal.PtrToStructure(new IntPtr(wblt.texTable.ToInt32() + (btStruct * b)), typeof(WVR_CtrlerTexBitmap));
                                batteryMin = (int)Marshal.PtrToStructure(new IntPtr(wblt.minTable.ToInt32() + (sizeInt * b)), typeof(int));
                                batteryMax = (int)Marshal.PtrToStructure(new IntPtr(wblt.maxTable.ToInt32() + (sizeInt * b)), typeof(int));
                            }
                            else
                            {
                                batteryImage = (WVR_CtrlerTexBitmap)Marshal.PtrToStructure(new IntPtr(wblt.texTable.ToInt64() + (btStruct * b)), typeof(WVR_CtrlerTexBitmap));
                                batteryMin = (int)Marshal.PtrToStructure(new IntPtr(wblt.minTable.ToInt64() + (sizeInt * b)), typeof(int));
                                batteryMax = (int)Marshal.PtrToStructure(new IntPtr(wblt.maxTable.ToInt64() + (sizeInt * b)), typeof(int));
                            }

                            BatteryIndicator tmpBI = new BatteryIndicator();
                            tmpBI.level = b;
                            tmpBI.min = (float)batteryMin;
                            tmpBI.max = (float)batteryMax;

                            var batteryImageSize = batteryImage.height * batteryImage.stride;

                            tmpBI.batteryTextureInfo = new TextureInfo();
                            tmpBI.batteryTextureInfo.modelTextureData = new byte[batteryImageSize];
                            Marshal.Copy(batteryImage.bitmap, tmpBI.batteryTextureInfo.modelTextureData, 0, tmpBI.batteryTextureInfo.modelTextureData.Length);
                            tmpBI.batteryTextureInfo.width = (int)batteryImage.width;
                            tmpBI.batteryTextureInfo.height = (int)batteryImage.height;
                            tmpBI.batteryTextureInfo.stride = (int)batteryImage.stride;
                            tmpBI.batteryTextureInfo.format = (int)batteryImage.format;
                            tmpBI.batteryTextureInfo.size = (int)batteryImageSize;
                            tmpBI.textureLoaded = true;
                            //Debug.Log(" Battery Level[" + tmpBI.level + "] min: " + tmpBI.min + " max: " + tmpBI.max + " loaded: " + tmpBI.textureLoaded + " w: " + batteryImage.width + " h: " + batteryImage.height + " size: " + batteryImageSize);

                            batteryTextureList.Add(tmpBI);
                        }
                        modelResource.isBatterySetting = true;

                        //Debug.Log("WVR_ReleaseControllerModel, ctrlModel IntPtr = " + ctrlModel.ToInt32());

                        //Debug.Log("Call WVR_ReleaseControllerModel");
                        Interop.WVR_ReleaseControllerModel(ref ctrlModel);
                    }
                    else
                    {
                        Debug.Log("[WARNING] WVR_GetCurrentControllerModel return model is null");
                    }

                    wfef = new WaitForEndOfFrame();

                    ImgMaterial = new Material(Shader.Find("Unlit/Texture"));
                    StartCoroutine(SpawnRenderModel());
                }
#endif
            }
        }

        string emitterMeshName = "__CM__Emitter";

#if VIU_WAVEXR_ESSENCE_CONTROLLER_MODEL
        IEnumerator SpawnRenderModel()
        {
            //while (true)
            //{
            //    if (modelResource != null)
            //    {
            //        if (modelResource.parserReady) break;
            //    }
            //    Debug.Log("[DEBUG] SpawnRenderModel is waiting");
            //    yield return wfef;
            //}

            //Debug.Log("[DEBUG] Start to spawn all meshes!");

            if (modelResource == null)
            {
                Debug.Log("[DEBUG] modelResource is null, skipping spawn objects");
                //mLoadingState = LoadingState.LoadingState_NOT_LOADED;
                yield return null;
            }

            int textureSize = modelResource.modelTextureCount;
            Debug.Log("[DEBUG] modelResource texture count = " + textureSize);

            for (int t = 0; t < textureSize; t++)
            {
                TextureInfo mainTexture = modelResource.modelTextureInfo[t];

                Texture2D modelpng = new Texture2D((int)mainTexture.width, (int)mainTexture.height, TextureFormat.RGBA32, false);
                modelpng.LoadRawTextureData(mainTexture.modelTextureData);
                modelpng.Apply();

                modelResource.modelTexture[t] = modelpng;
                Debug.Log("[DEBUG] Add [" + t + "] texture2D");
            }

            string meshName = "";
            childArray = new GameObject[modelResource.sectionCount];
            showState = new bool[modelResource.sectionCount];
            for (uint i = 0; i < modelResource.sectionCount; i++)
            {
                meshName = Marshal.PtrToStringAnsi(modelResource.FBXInfo[i].meshName);
                meshCom = null;
                meshGO = null;

                bool meshAlready = false;

                for (uint j = 0; j < i; j++)
                {
                    string tmp = Marshal.PtrToStringAnsi(modelResource.FBXInfo[j].meshName);

                    if (tmp.Equals(meshName))
                    {
                        meshAlready = true;
                    }
                }

                if (meshAlready)
                {
                    Debug.Log("[DEBUG] " + meshName + " is created! skip.");
                    continue;
                }

                updateMesh = new Mesh();
                meshCom = new GameObject();
                meshCom.AddComponent<MeshRenderer>();
                meshCom.AddComponent<MeshFilter>();
                meshGO = Instantiate(meshCom);
                meshGO.transform.parent = this.transform;
                meshGO.name = meshName;
                meshGO.transform.localPosition = Vector3.zero;
                meshGO.transform.localRotation = Quaternion.identity;
                childArray[i] = meshGO;
                //Matrix4x4 t = TransformConverter.RigidTransform.toMatrix44(modelResource.FBXInfo[i].matrix, false);

                //Vector3 x = TransformConverter.GetPosition(t);
                //meshGO.transform.localPosition = new Vector3(x.x, x.y, -x.z);

                //meshGO.transform.localRotation = TransformConverter.GetRotation(t);
                //Vector3 r = meshGO.transform.localEulerAngles;
                //meshGO.transform.localEulerAngles = new Vector3(-r.x, r.y, r.z);
                //meshGO.transform.localScale = TransformConverter.GetScale(t);

                //Debug.Log("[DEBUG] i = " + i + " MeshGO = " + meshName + ", localPosition: " + meshGO.transform.localPosition.x + ", " + meshGO.transform.localPosition.y + ", " + meshGO.transform.localPosition.z);
                //Debug.Log("[DEBUG] i = " + i + " MeshGO = " + meshName + ", localRotation: " + meshGO.transform.localEulerAngles);
                //Debug.Log("[DEBUG] i = " + i + " MeshGO = " + meshName + ", localScale: " + meshGO.transform.localScale);

                var meshfilter = meshGO.GetComponent<MeshFilter>();
                updateMesh.Clear();
                updateMesh.vertices = modelResource.SectionInfo[i]._vectice;
                updateMesh.uv = modelResource.SectionInfo[i]._uv;
                updateMesh.uv2 = modelResource.SectionInfo[i]._uv;
                updateMesh.colors32 = colors.ToArray();
                updateMesh.normals = modelResource.SectionInfo[i]._normal;
                updateMesh.SetIndices(modelResource.SectionInfo[i]._indice, MeshTopology.Triangles, 0);
                updateMesh.name = meshName;
                if (meshfilter != null)
                {
                    meshfilter.mesh = updateMesh;
                }
                var meshRenderer = meshGO.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    if (ImgMaterial == null)
                    {
                        Debug.Log("[DEBUG] ImgMaterial is null");
                    }
                    meshRenderer.material = ImgMaterial;
                    meshRenderer.material.mainTexture = modelResource.modelTexture[0];
                    meshRenderer.enabled = true;
                }

                if (meshName.Equals(emitterMeshName))
                {
                    Debug.Log("[DEBUG] " + meshName + " is found, set " + meshName + " active: true");
                    meshGO.SetActive(true);
                }
                else if (meshName.Equals("__CM__Battery"))
                {
                    //isBatteryIndicatorReady = false;
                    if (modelResource.isBatterySetting)
                    {
                        if (modelResource.batteryTextureList != null)
                        {
                            batteryMR = meshGO.GetComponent<MeshRenderer>();
                            Material mat = null;

                            if (preferedModelName.Contains("Right"))//(modelResource.hand == XR_Hand.Dominant)
                            {
                                Debug.Log("[DEBUG] loaded Materials / WaveBatteryMatR");
                                mat = Resources.Load("Materials/WaveBatteryMatR") as Material;
                            }
                            else
                            {
                                Debug.Log("[DEBUG] loaded Materials/WaveBatteryMatL");
                                mat = Resources.Load("Materials/WaveBatteryMatL") as Material;
                            }

                            if (mat != null)
                            {
                                batteryMR.material = mat;
                            }

                            foreach (BatteryIndicator bi in modelResource.batteryTextureList)
                            {
                                TextureInfo ti = bi.batteryTextureInfo;

                                bi.batteryTexture = new Texture2D((int)ti.width, (int)ti.height, TextureFormat.RGBA32, false);
                                bi.batteryTexture.LoadRawTextureData(ti.modelTextureData);
                                bi.batteryTexture.Apply();
                                Debug.Log(" min: " + bi.min + " max: " + bi.max + " loaded: " + bi.textureLoaded + " w: " + ti.width + " h: " + ti.height + " size: " + ti.size + " array length: " + ti.modelTextureData.Length);
                            }

                            batteryMR.material.mainTexture = modelResource.batteryTextureList[0].batteryTexture;
                            batteryMR.enabled = true;
                            //isBatteryIndicatorReady = true;
                        }
                    }
                    meshGO.SetActive(false);
                    Debug.Log("[DEBUG] " + meshName + " is found, set " + meshName + " active: false(waiting for update");

                    batteryGO = meshGO;
                }
                else if (meshName == "__CM__TouchPad_Touch")
                {
                    Debug.Log("[DEBUG] " + meshName + " is found, set " + meshName + " active: false");
                    meshGO.SetActive(false);
                }
                else
                {
                    Debug.Log("[DEBUG] set " + meshName + " active: " + modelResource.SectionInfo[i]._active);
                    meshGO.SetActive(modelResource.SectionInfo[i]._active);
                }

                yield return wfef;
            }
            //Debug.Log("[DEBUG] send " + WhichHand + " RENDER_MODEL_READY ");

            //onRenderModelReady?.Invoke(WhichHand);

            Resources.UnloadUnusedAssets();
            //mLoadingState = LoadingState.LoadingState_LOADED;
        }

        private void updateBatteryLevel()
        {
            if (batteryGO != null)
            {
                if (true)
                {
                    if ((modelResource == null) || (modelResource.batteryTextureList == null))
                        return;

                    bool found = false;
                    //WVR_DeviceType type = checkDeviceType();
                    float batteryP = Interop.WVR_GetDeviceBatteryPercentage(preferedModelName.Contains("Left") ? WVR_DeviceType.WVR_DeviceType_Controller_Left : WVR_DeviceType.WVR_DeviceType_Controller_Right);
                    if (batteryP < 0)
                    {
                        Debug.Log("[DEBUG] updateBatteryLevel BatteryPercentage is negative, return");
                        batteryGO.SetActive(false);
                        return;
                    }
                    foreach (BatteryIndicator bi in modelResource.batteryTextureList)
                    {
                        if (batteryP >= bi.min / 100 && batteryP <= bi.max / 100)
                        {
                            currentBattery = bi;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        if (batteryMR != null)
                        {
                            batteryMR.material.mainTexture = currentBattery.batteryTexture;
                            //Debug.Log("[DEBUG] updateBatteryLevel battery level to " + currentBattery.level + ", battery percent: " + batteryP);
                            batteryGO.SetActive(true);
                        }
                        else
                        {
                            Debug.Log("[DEBUG] updateBatteryLevel Can't get battery mesh renderer");
                            batteryGO.SetActive(false);
                        }
                    }
                    else
                    {
                        batteryGO.SetActive(false);
                    }
                }
                //else
                //{
                //    batteryGO.SetActive(false);
                //}
            }
        }

        private class ModelResource
        {
            public string renderModelName;
            public bool loadFromAsset;
            public bool mergeToOne;
            //public XR_Hand hand;

            public uint sectionCount;
            public FBXInfo_t[] FBXInfo;
            public MeshInfo_t[] SectionInfo;
            public bool parserReady;

            public int modelTextureCount;
            public Texture2D[] modelTexture;
            public TextureInfo[] modelTextureInfo;

            public bool isTouchSetting;
            public TouchSetting TouchSetting;

            public bool isBatterySetting;
            public List<BatteryIndicator> batteryTextureList;
        }

        private class BatteryIndicator
        {
            public int level;
            public float min;
            public float max;
            public string texturePath;
            public bool textureLoaded;
            public Texture2D batteryTexture;
            public TextureInfo batteryTextureInfo;
        }

        private class TextureInfo
        {
            public byte[] modelTextureData;
            public int width;
            public int height;
            public int stride;
            public int size;
            public int format;
        }

        private class TouchSetting
        {
            public Vector3 touchForward;
            public Vector3 touchCenter;
            public Vector3 touchRight;
            public Vector3 touchPtU;
            public Vector3 touchPtW;
            public Vector3 touchPtV;
            public float raidus;
            public float touchptHeight;
        }

        private class MeshObject
        {
            public string MeshName;
            public bool hasEffect;
            public GameObject gameObject;
            public Vector3 originPosition;
            public Material originMat;
            public Material effectMat;
        }

        private void OnLoadModelComplete(string renderModelName)
        {
            m_loadingRenderModels.Remove(renderModelName);

            if (loadedModelName == renderModelName) { return; }
            if (preferedModelName != renderModelName) { return; }
            if (!isActiveAndEnabled) { return; }
            //Debug.Log(transform.parent.parent.name + " OnLoadModelComplete " + renderModelName);

            ClearModel();

            loadedModelName = renderModelName;
        }
#endif

        public void SetDeviceIndex(uint index)
        {
            Debug.Log(transform.parent.parent.name + " SetDeviceIndex " + index);
            m_deviceIndex = index;
            LoadPreferedModel();
        }
    }
}