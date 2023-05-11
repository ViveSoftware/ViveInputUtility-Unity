//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

#if VIU_STEAMVR_2_0_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace HTC.UnityPlugin.Vive.SteamVRExtension
{
    [Serializable]
    public class VIUSteamVRLoadJsonFileBase<T> where T : VIUSteamVRLoadJsonFileBase<T>
    {
        private static Dictionary<string, T> s_fileCache;

        [JsonIgnore]
        public string dirPath { get; set; }
        [JsonIgnore]
        public string fileName { get; set; }
        [JsonIgnore]
        public string fullPath { get { return Path.Combine(dirPath, fileName); } }
        [JsonIgnore]
        public DateTime lastWriteTime { get; private set; }

        public static bool TryLoad(string dirPath, string fileName, out T file, bool force = false)
        {
            try
            {
                var fullPath = Path.Combine(dirPath, fileName);
                if (!File.Exists(fullPath)) { file = null; return false; }

                var lastWriteTime = File.GetLastWriteTime(fullPath);

                // check cached file
                if (!force && s_fileCache != null && s_fileCache.TryGetValue(fullPath, out file))
                {
                    if (file.lastWriteTime == lastWriteTime)
                    {
                        return true;
                    }
                }

                file = JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPath));
                file.dirPath = dirPath;
                file.fileName = fileName;
                file.lastWriteTime = lastWriteTime;

                if (s_fileCache == null) { s_fileCache = new Dictionary<string, T>() { { fullPath, file } }; }
                else { s_fileCache[fullPath] = file; }

                file.OnAfterLoaded();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (s_fileCache != null) { s_fileCache.Clear(); }
                file = null;
                return false;
            }
        }

        protected virtual void OnAfterLoaded() { }

        public void Save() { Save(dirPath); }

        public void Save(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                Debug.LogWarning("dirPath is empty");
                return;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("fileName is empty");
                return;
            }

            try
            {
                OnBeforeSave(dirPath);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(Path.Combine(dirPath, fileName), json);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual void OnBeforeSave(string dirPash) { }
    }

    public interface IStringKey
    {
        string stringKey { get; }
    }

    public interface IMergable<T>
    {
        bool IsMerged(T obj);
        void Merge(T obj);
        T Copy();
    }

    [Serializable]
    public class MergableDictionary<T> : Dictionary<string, T>, IMergable<MergableDictionary<T>> where T : IMergable<T>
    {
        public event Action<T> onNewItemWhenMerge;

        public MergableDictionary() : base() { }

        public MergableDictionary(MergableDictionary<T> src) : base(src) { }

        public bool IsMerged(MergableDictionary<T> obj)
        {
            if (this == obj) { return true; }

            foreach (var pair in obj)
            {
                T srcV;
                if (!TryGetValue(pair.Key, out srcV)) { return false; }

                if (!srcV.IsMerged(pair.Value)) { return false; }
            }

            return true;
        }

        public MergableDictionary<T> Copy()
        {
            return new MergableDictionary<T>(this);
        }

        public void Merge(MergableDictionary<T> obj)
        {
            if (this == obj) { return; }

            foreach (var pair in obj)
            {
                T srcV;
                if (!TryGetValue(pair.Key, out srcV))
                {
                    srcV = pair.Value.Copy();
                    Add(pair.Key, srcV);
                    if (onNewItemWhenMerge != null) { onNewItemWhenMerge(srcV); }
                }
                else
                {
                    srcV.Merge(pair.Value);
                }
            }
        }
    }

    [Serializable]
    public class OverridableDictionary<T> : Dictionary<string, T>, IMergable<OverridableDictionary<T>> where T : IMergable<T>
    {
        public OverridableDictionary() : base() { }

        public OverridableDictionary(OverridableDictionary<T> src) : base(src) { }

        public bool IsMerged(OverridableDictionary<T> obj)
        {
            if (this == obj) { return true; }
            if (Count != obj.Count) { return false; }

            foreach (var pair in obj)
            {
                T srcV;
                if (!TryGetValue(pair.Key, out srcV)) { return false; }

                if (!srcV.IsMerged(pair.Value)) { return false; }
            }

            return true;
        }

        public OverridableDictionary<T> Copy()
        {
            return new OverridableDictionary<T>(this);
        }

        public void Merge(OverridableDictionary<T> obj)
        {
            if (this == obj) { return; }

            Clear();
            foreach (var pair in obj)
            {
                Add(pair.Key, pair.Value.Copy());
            }
        }
    }

    [Serializable]
    public class MergableList<T> : List<T>, IMergable<MergableList<T>> where T : IMergable<T>
    {
        private static List<bool> s_checkList;

        private void ResetCheckList()
        {
            if (s_checkList == null)
            {
                s_checkList = new List<bool>();
            }
            else
            {
                s_checkList.Clear();
            }

            foreach (var item in this) { s_checkList.Add(false); }
        }

        private bool FoundInCheckList(T item)
        {
            for (int i = 0, imax = s_checkList.Count; i < imax; ++i)
            {
                if (s_checkList[i]) { continue; }

                if (this[i].IsMerged(item))
                {
                    s_checkList[i] = true;
                    return true;
                }
            }
            return false;
        }

        public MergableList() : base() { }

        public MergableList(MergableList<T> src) : base(src) { }

        public bool IsMerged(MergableList<T> obj)
        {
            if (this == obj) { return true; }

            ResetCheckList();

            foreach (var item in obj)
            {
                if (!FoundInCheckList(item)) { return false; }
            }

            return true;
        }

        public MergableList<T> Copy()
        {
            return new MergableList<T>(this);
        }

        public void Merge(MergableList<T> obj)
        {
            if (this == obj) { return; }

            ResetCheckList();

            foreach (var item in obj)
            {
                if (!FoundInCheckList(item)) { Add(item.Copy()); }
            }
        }
    }

    [Serializable]
    public class OverridableList<T> : MergableList<T>, IMergable<OverridableList<T>> where T : IMergable<T>
    {
        public OverridableList() : base() { }

        public OverridableList(OverridableList<T> src) : base(src) { }

        public bool IsMerged(OverridableList<T> obj)
        {
            if (this == obj) { return true; }
            if (Count != obj.Count) { return false; }
            return base.IsMerged(obj);
        }

        public new OverridableList<T> Copy()
        {
            return new OverridableList<T>(this);
        }

        public void Merge(OverridableList<T> obj)
        {
            if (this == obj) { return; }

            Clear();
            foreach (var item in obj)
            {
                Add(item.Copy());
            }
        }
    }

    [Serializable]
    public class MergableDictionary : Dictionary<string, string>, IMergable<MergableDictionary>
    {
        public event Action<string> onNewItemWhenMerge;

        public MergableDictionary() : base() { }

        public MergableDictionary(MergableDictionary src) : base(src) { }

        public bool IsMerged(MergableDictionary obj)
        {
            if (this == obj) { return true; }

            foreach (var pair in obj)
            {
                string srcV;
                if (!TryGetValue(pair.Key, out srcV)) { return false; }

                if (srcV != pair.Value) { return false; }
            }

            return true;
        }

        public MergableDictionary Copy()
        {
            return new MergableDictionary(this);
        }

        public void Merge(MergableDictionary obj)
        {
            if (this == obj) { return; }

            foreach (var pair in obj)
            {
                string srcV;
                if (!TryGetValue(pair.Key, out srcV))
                {
                    srcV = pair.Value;
                    Add(pair.Key, srcV);
                    if (onNewItemWhenMerge != null) { onNewItemWhenMerge(srcV); }
                }
                else
                {
                    this[pair.Key] = pair.Value;
                }
            }
        }
    }

    [Serializable]
    public class OverridableDictionary : Dictionary<string, string>, IMergable<OverridableDictionary>
    {
        public OverridableDictionary() : base() { }

        public OverridableDictionary(OverridableDictionary src) : base(src) { }

        public bool IsMerged(OverridableDictionary obj)
        {
            if (this == obj) { return true; }
            if (Count != obj.Count) { return false; }

            foreach (var pair in obj)
            {
                string srcV;
                if (!TryGetValue(pair.Key, out srcV)) { return false; }

                if (srcV != pair.Value) { return false; }
            }

            return true;
        }

        public OverridableDictionary Copy()
        {
            return new OverridableDictionary(this);
        }

        public void Merge(OverridableDictionary obj)
        {
            if (this == obj) { return; }

            Clear();
            foreach (var pair in obj)
            {
                this[pair.Key] = pair.Value;
            }
        }
    }

    public static class SerializeExtension
    {
        public static MergableDictionary<T> ToMergableDictionary<T>(this List<T> list) where T : IMergable<T>, IStringKey
        {
            var result = new MergableDictionary<T>();
            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(item.stringKey))
                {
                    Debug.LogWarning("MergableDictionary key cannot be null");
                }
                else if (result.ContainsKey(item.stringKey))
                {
                    Debug.LogWarning("Duplicate key(" + item.stringKey + ") found");
                }
                else
                {
                    result.Add(item.stringKey, item);
                }
            }
            return result;
        }
    }
}
#endif