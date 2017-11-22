using HTC.UnityPlugin.Utility;
using System.Collections.Generic;
using UnityEngine;

public class PlayGroundManager : MonoBehaviour
{
    private static List<Draggable> draggablesCache = new List<Draggable>();
    private Dictionary<int, RigidPose> poseTable = new Dictionary<int, RigidPose>();

    private void Awake()
    {
        draggablesCache.Clear();
        GetComponentsInChildren(draggablesCache);
        for (int i = 0, imax = draggablesCache.Count; i < imax; ++i)
        {
            var dt = draggablesCache[i].transform;
            poseTable[dt.GetInstanceID()] = new RigidPose(dt);
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
            RigidPose pose;
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
