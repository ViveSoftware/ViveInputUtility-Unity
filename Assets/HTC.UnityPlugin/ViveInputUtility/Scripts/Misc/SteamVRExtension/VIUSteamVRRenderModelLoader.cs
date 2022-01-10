//========= Copyright 2016-2022, HTC Corporation. All rights reserved. ===========

#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if VIU_STEAMVR && UNITY_STANDALONE
using Valve.VR;
#endif

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    public static class VIUSteamVRRenderModelLoader
    {
        public class RenderModel
        {
            public string name;
            public string[] childCompNames;
            public string[] childModelNames;
            public int childCount;
            public Dictionary<int, Texture2D> textures;

            public bool TryCreateMaterialForTexture(int textureID, Shader shader, out Material material)
            {
                Texture2D texture;
                if (textures != null && textures.TryGetValue(textureID, out texture))
                {
                    material = new Material(shader)
                    {
                        mainTexture = texture,
                    };
                    return true;
                }
                else
                {
                    material = default(Material);
                    return false;
                }
            }
        }

        public class Model
        {
            public Mesh mesh;
            public int textureID;
        }

        private class LoadRenderModelJob
        {
            private struct PtrPack
            {
                public IntPtr model;
                public IntPtr texture;
                public IntPtr textureD3D11;
            }

            private static int s_nextJobID;
            private int m_jobID;
            private int m_startFrame;
            private string m_name;
            private Action<string> m_callback;
            private bool m_isDone;
            private RenderModel m_unreadyRM;
            private PtrPack m_loadedPtr;
            private PtrPack[] m_loadedChildPtrs;

            public int jobID { get { return m_jobID; } }

            public LoadRenderModelJob(string name, Action<string> callback)
            {
                m_name = name;
                m_callback = callback;
                m_jobID = s_nextJobID++;
            }

            private void DoComplete()
            {
                m_isDone = true;
                if (m_callback != null)
                {
                    m_callback(m_name);
                    m_callback = null;
                }
            }

#if VIU_STEAMVR && UNITY_STANDALONE
            private static readonly bool s_verbose = false;
            // Should not do job after interrupted
            public void InterruptAndComplete()
            {
                var vrRenderModels = OpenVR.RenderModels;
                if (vrRenderModels == null) { DoComplete(); return; }

                if (m_loadedPtr.texture != IntPtr.Zero) { vrRenderModels.FreeTexture(m_loadedPtr.texture); }
                if (m_loadedPtr.model != IntPtr.Zero) { vrRenderModels.FreeRenderModel(m_loadedPtr.model); }

                foreach (var ptrPack in m_loadedChildPtrs)
                {
                    if (ptrPack.texture != IntPtr.Zero) { vrRenderModels.FreeTexture(ptrPack.texture); }
                    if (ptrPack.model != IntPtr.Zero) { vrRenderModels.FreeRenderModel(ptrPack.model); }
                }

                DoComplete();
            }

            // return true if is done
            public bool DoJob()
            {
                if (m_isDone) { return true; }

                if (!s_renderModelsCache.ContainsKey(m_name))
                {
                    var vrRenderModels = OpenVR.RenderModels;
                    if (vrRenderModels == null) { DoComplete(); return true; }

                    if (m_unreadyRM == null)
                    {
                        var childCount = (int)vrRenderModels.GetComponentCount(m_name);
                        if (childCount > 0)
                        {
                            var childCompNames = new string[childCount];
                            var childModelNames = new string[childCount];
                            var strBuilder = new StringBuilder(16);

                            for (int iChild = 0; iChild < childCount; ++iChild)
                            {
                                var strCap = vrRenderModels.GetComponentName(m_name, (uint)iChild, null, 0);
                                if (strCap == 0) { continue; }
                                strBuilder.Length = 0;
                                strBuilder.EnsureCapacity((int)strCap);
                                if (vrRenderModels.GetComponentName(m_name, (uint)iChild, strBuilder, strCap) == 0) { continue; }
                                childCompNames[iChild] = strBuilder.ToString();
                                if (s_verbose) { Debug.Log("[" + m_jobID + "]+0 GetComponentName " + m_name + "[" + iChild + "]=" + childCompNames[iChild]); }

                                strCap = vrRenderModels.GetComponentRenderModelName(m_name, childCompNames[iChild], null, 0);
                                if (strCap == 0) { continue; }
                                strBuilder.Length = 0;
                                strBuilder.EnsureCapacity((int)strCap);
                                if (vrRenderModels.GetComponentRenderModelName(m_name, childCompNames[iChild], strBuilder, strCap) == 0) { continue; }
                                childModelNames[iChild] = strBuilder.ToString();
                                if (s_verbose) { Debug.Log("[" + m_jobID + "]+0 GetComponentRenderModelName " + m_name + "[" + childCompNames[iChild] + "]=" + System.IO.Path.GetFileName(childModelNames[iChild])); }
                            }

                            m_unreadyRM = new RenderModel()
                            {
                                name = m_name,
                                childCompNames = childCompNames,
                                childModelNames = childModelNames,
                                childCount = childCount,
                                textures = new Dictionary<int, Texture2D>(),
                            };

                            m_loadedChildPtrs = new PtrPack[childCount];
                        }
                        else
                        {
                            m_unreadyRM = new RenderModel()
                            {
                                name = m_name,
                                textures = new Dictionary<int, Texture2D>(),
                            };
                        }

                        m_startFrame = Time.frameCount;
                    }

                    if (m_unreadyRM.childCount == 0)
                    {
                        if (!DoLoadModelJob(vrRenderModels, m_name, m_unreadyRM.textures, ref m_loadedPtr.model, ref m_loadedPtr.texture, ref m_loadedPtr.textureD3D11))
                        {
                            return false;
                        }

                        if (m_loadedPtr.texture != IntPtr.Zero) { vrRenderModels.FreeTexture(m_loadedPtr.texture); }
                        if (m_loadedPtr.model != IntPtr.Zero) { vrRenderModels.FreeRenderModel(m_loadedPtr.model); }
                    }
                    else
                    {
                        var loadChildModelsDone = true;
                        for (int i = 0, imax = m_unreadyRM.childCount; i < imax; ++i)
                        {
                            loadChildModelsDone = DoLoadModelJob(vrRenderModels, m_unreadyRM.childModelNames[i], m_unreadyRM.textures, ref m_loadedChildPtrs[i].model, ref m_loadedChildPtrs[i].texture, ref m_loadedChildPtrs[i].textureD3D11) && loadChildModelsDone;
                        }

                        if (!loadChildModelsDone) { return false; }

                        foreach (var ptrPack in m_loadedChildPtrs)
                        {
                            if (ptrPack.texture != IntPtr.Zero) { vrRenderModels.FreeTexture(ptrPack.texture); }
                            if (ptrPack.model != IntPtr.Zero) { vrRenderModels.FreeRenderModel(ptrPack.model); }
                        }
                    }

                    s_renderModelsCache.Add(m_name, m_unreadyRM);
                }

                DoComplete();
                return true;
            }

            // return true if is done
            private bool DoLoadModelJob(CVRRenderModels vrRenderModels, string modelName, Dictionary<int, Texture2D> texturesCache, ref IntPtr modelPtr, ref IntPtr texturePtr, ref IntPtr d3d11TexturePtr)
            {
                if (string.IsNullOrEmpty(modelName)) { return true; }

                EVRRenderModelError error;
                Model model;
                if (!s_modelsCache.TryGetValue(modelName, out model))
                {
                    switch (error = vrRenderModels.LoadRenderModel_Async(modelName, ref modelPtr))
                    {
                        default:
                            Debug.LogError("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadRenderModel_Async failed! " + System.IO.Path.GetFileName(modelName) + " EVRRenderModelError=" + error);
                            return true;
                        case EVRRenderModelError.Loading:
                            if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadRenderModel_Async loading... " + System.IO.Path.GetFileName(modelName)); }
                            return false;
                        case EVRRenderModelError.None:
                            if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadRenderModel_Async succeed! " + System.IO.Path.GetFileName(modelName)); }
                            RenderModel_t modelData = MarshalRenderModel(modelPtr);

                            var vertices = new Vector3[modelData.unVertexCount];
                            var normals = new Vector3[modelData.unVertexCount];
                            var uv = new Vector2[modelData.unVertexCount];

                            Type type = typeof(RenderModel_Vertex_t);
                            for (int iVert = 0; iVert < modelData.unVertexCount; iVert++)
                            {
                                var ptr = new IntPtr(modelData.rVertexData.ToInt64() + iVert * Marshal.SizeOf(type));
                                var vert = (RenderModel_Vertex_t)Marshal.PtrToStructure(ptr, type);

                                vertices[iVert] = new Vector3(vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2);
                                normals[iVert] = new Vector3(vert.vNormal.v0, vert.vNormal.v1, -vert.vNormal.v2);
                                uv[iVert] = new Vector2(vert.rfTextureCoord0, vert.rfTextureCoord1);
                            }

                            var indexCount = (int)modelData.unTriangleCount * 3;
                            var indices = new short[indexCount];
                            Marshal.Copy(modelData.rIndexData, indices, 0, indices.Length);

                            var triangles = new int[indexCount];
                            for (int iTri = 0; iTri < modelData.unTriangleCount; iTri++)
                            {
                                triangles[iTri * 3 + 0] = indices[iTri * 3 + 2];
                                triangles[iTri * 3 + 1] = indices[iTri * 3 + 1];
                                triangles[iTri * 3 + 2] = indices[iTri * 3 + 0];
                            }

                            model = new Model()
                            {
                                textureID = modelData.diffuseTextureId,
                                mesh = new Mesh()
                                {
                                    hideFlags = HideFlags.HideAndDontSave,
                                    vertices = vertices,
                                    normals = normals,
                                    uv = uv,
                                    triangles = triangles,
                                },
                            };

                            s_modelsCache.Add(modelName, model);
                            break;
                    }
                }

                Texture2D texture;
                if (!texturesCache.TryGetValue(model.textureID, out texture))
                {
                    switch (error = vrRenderModels.LoadTexture_Async(model.textureID, ref texturePtr))
                    {
                        default:
                            Debug.LogError("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadTexture_Async failed! " + System.IO.Path.GetFileName(modelName) + "[" + model.textureID + "] EVRRenderModelError=" + error);
                            return true;
                        case EVRRenderModelError.Loading:
                            if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadTexture_Async loading... " + System.IO.Path.GetFileName(modelName) + "[" + model.textureID + "]"); }
                            return false;
                        case EVRRenderModelError.None:
                            if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadTexture_Async succeed! " + System.IO.Path.GetFileName(modelName) + "[" + model.textureID + "]"); }
                            var textureMap = MarshalRenderModelTextureMap(texturePtr);
                            texture = new Texture2D(textureMap.unWidth, textureMap.unHeight, TextureFormat.RGBA32, false);

                            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
                            {
                                texture.Apply();
                                d3d11TexturePtr = texture.GetNativeTexturePtr();
                            }
                            else
                            {
                                var textureMapData = new byte[textureMap.unWidth * textureMap.unHeight * 4]; // RGBA
                                Marshal.Copy(textureMap.rubTextureMapData, textureMapData, 0, textureMapData.Length);

                                var colors = new Color32[textureMap.unWidth * textureMap.unHeight];
                                int iColor = 0;
                                for (int iHeight = 0; iHeight < textureMap.unHeight; iHeight++)
                                {
                                    for (int iWidth = 0; iWidth < textureMap.unWidth; iWidth++)
                                    {
                                        var r = textureMapData[iColor++];
                                        var g = textureMapData[iColor++];
                                        var b = textureMapData[iColor++];
                                        var a = textureMapData[iColor++];
                                        colors[iHeight * textureMap.unWidth + iWidth] = new Color32(r, g, b, a);
                                    }
                                }

                                texture.SetPixels32(colors);
                                texture.Apply();
                            }

                            texturesCache.Add(model.textureID, texture);
                            break;
                    }
                }

                if (d3d11TexturePtr != IntPtr.Zero)
                {
                    while (true)
                    {
                        switch (error = vrRenderModels.LoadIntoTextureD3D11_Async(model.textureID, d3d11TexturePtr))
                        {
                            default:
                                Debug.LogError("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadIntoTextureD3D11_Async failed! " + System.IO.Path.GetFileName(modelName) + " EVRRenderModelError=" + error);
                                d3d11TexturePtr = IntPtr.Zero;
                                return true;
                            case EVRRenderModelError.Loading:
                                if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadIntoTextureD3D11_Async loading... " + System.IO.Path.GetFileName(modelName) + "[" + model.textureID + "]"); }
                                break;
                            case EVRRenderModelError.None:
                                if (s_verbose) { Debug.Log("[" + m_jobID + "]+" + (Time.frameCount - m_startFrame) + " LoadIntoTextureD3D11_Async succeed! " + System.IO.Path.GetFileName(modelName)); }
                                d3d11TexturePtr = IntPtr.Zero;
                                return true;
                        }
                        // FIXME: LoadIntoTextureD3D11_Async blocks main thread? Crashes when not calling it in while loop
#if !UNITY_METRO
                        System.Threading.Thread.Sleep(1);
#endif
                    }
                }

                return true;
            }

            /// <summary>
            /// Helper function to handle the inconvenient fact that the packing for RenderModel_t is 
            /// different on Linux/OSX (4) than it is on Windows (8)
            /// </summary>
            /// <param name="pRenderModel">native pointer to the RenderModel_t</param>
            /// <returns></returns>
            private static RenderModel_t MarshalRenderModel(IntPtr pRenderModel)
            {
                if ((Environment.OSVersion.Platform == PlatformID.MacOSX) ||
                    (Environment.OSVersion.Platform == PlatformID.Unix))
                {
                    var packedModel = (RenderModel_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t_Packed));
                    var model = new RenderModel_t();
                    packedModel.Unpack(ref model);
                    return model;
                }
                else
                {
                    return (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));
                }
            }

            /// <summary>
            /// Helper function to handle the inconvenient fact that the packing for RenderModel_TextureMap_t is 
            /// different on Linux/OSX (4) than it is on Windows (8)
            /// </summary>
            /// <param name="pTextureMap">native pointer to the RenderModel_TextureMap_t</param>
            /// <returns></returns>
            private static RenderModel_TextureMap_t MarshalRenderModelTextureMap(IntPtr pTextureMap)
            {
                if ((Environment.OSVersion.Platform == PlatformID.MacOSX) ||
                    (Environment.OSVersion.Platform == PlatformID.Unix))
                {
                    var packedModel = (RenderModel_TextureMap_t_Packed)Marshal.PtrToStructure(pTextureMap, typeof(RenderModel_TextureMap_t_Packed));
                    var model = new RenderModel_TextureMap_t();
                    packedModel.Unpack(ref model);
                    return model;
                }
                else
                {
                    return (RenderModel_TextureMap_t)Marshal.PtrToStructure(pTextureMap, typeof(RenderModel_TextureMap_t));
                }
            }
#else
            public void InterruptAndComplete()
            {
                DoComplete();
            }

            public bool DoJob()
            {
                if (m_isDone) { return true; }

                DoComplete();
                return true;
            }
#endif
        }


        private static Dictionary<string, RenderModel> s_renderModelsCache = new Dictionary<string, RenderModel>();
        private static Dictionary<string, Model> s_modelsCache = new Dictionary<string, Model>();

        public static Dictionary<string, RenderModel> renderModelsCache { get { return s_renderModelsCache; } }
        public static Dictionary<string, Model> modelsCache { get { return s_modelsCache; } }

        public static void ClearCache()
        {
            s_renderModelsCache.Clear();
            s_modelsCache.Clear();
        }

        // NOTICE: Avoid calling Load after applicaion quit, this function will create worker gameobject
        public static void Load(string name, Action<string> onComplete)
        {
            WorkerBehaviour.EnqueueJob(new LoadRenderModelJob(name, onComplete));
        }

        #region Worker Behaviour
        private class WorkerBehaviour : MonoBehaviour
        {
            private static WorkerBehaviour s_worker;
            private static Queue<LoadRenderModelJob> s_jobQueue;

            private Coroutine m_coroutine;

            private bool isWorking
            {
                get { return m_coroutine != null; }
                set
                {
                    if (isWorking == value) { return; }
                    if (value) { m_coroutine = StartCoroutine(WorkingCoroutine()); }
                    else { StopCoroutine(m_coroutine); m_coroutine = null; }
                }
            }

            public static void EnqueueJob(LoadRenderModelJob job)
            {
                if (s_worker == null)
                {
                    var workerObj = new GameObject(typeof(VIUSteamVRRenderModelLoader).Name + "." + typeof(WorkerBehaviour).Name)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    DontDestroyOnLoad(workerObj);
                    s_worker = workerObj.AddComponent<WorkerBehaviour>();
                }

                if (s_jobQueue == null)
                {
                    s_jobQueue = new Queue<LoadRenderModelJob>();
                }

                s_jobQueue.Enqueue(job);

                s_worker.isWorking = true;
            }

            private void OnDestroy()
            {
                isWorking = false;

                while (s_jobQueue.Count > 0)
                {
                    s_jobQueue.Dequeue().InterruptAndComplete();
                }
            }

            private IEnumerator WorkingCoroutine()
            {
                while (s_jobQueue.Count > 0)
                {
                    if (s_jobQueue.Peek().DoJob())
                    {
                        s_jobQueue.Dequeue();
                    }
                    else
                    {
                        yield return null;
                    }
                }

                isWorking = false;
            }
        }
        #endregion
    }
}