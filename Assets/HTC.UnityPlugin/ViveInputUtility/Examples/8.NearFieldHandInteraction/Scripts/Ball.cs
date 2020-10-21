//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class Ball : MonoBehaviour
    {
        public void OnGrabbed()
        {
            Detach();
        }

        private void Detach()
        {
            transform.parent = null;
        }
    }
}