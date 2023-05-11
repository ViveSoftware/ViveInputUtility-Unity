//========= Copyright 2016-2023, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class Ball : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] private float Lifetime = 10.0f;

#pragma warning restore 0649

        public void OnGrabbed()
        {
            Detach();
        }

        private void Detach()
        {
            transform.parent = null;
            Destroy(gameObject, Lifetime);
        }
    }
}