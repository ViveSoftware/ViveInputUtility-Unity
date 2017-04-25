using System.Collections.Generic;
using UnityEngine;

public class PlayGroundManager : MonoBehaviour
{
    public struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;

        public Pose(Transform t) { pos = t.position; rot = t.rotation; }
    }

    private static List<Draggable> draggablesCache = new List<Draggable>();
    private Dictionary<int, Pose> poseTable = new Dictionary<int, Pose>();

    private void Awake()
    {
        draggablesCache.Clear();
        GetComponentsInChildren(draggablesCache);
        for (int i = 0, imax = draggablesCache.Count; i < imax; ++i)
        {
            var dt = draggablesCache[i].transform;
            poseTable[dt.GetInstanceID()] = new Pose(dt);
        }
        draggablesCache.Clear();
    }

    public void ResetPositions()
    {
        draggablesCache.Clear();
        GetComponentsInChildren(draggablesCache);
        for (int i = 0, imax = draggablesCache.Count; i < imax; ++i)
        {
            var dt = draggablesCache[i].transform;
            Pose pose;
            if (poseTable.TryGetValue(dt.GetInstanceID(), out pose))
            {
                dt.position = pose.pos;
                dt.rotation = pose.rot;

                var rb = dt.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        draggablesCache.Clear();
    }
}
