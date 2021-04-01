//========= Copyright 2016-2021, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class ItemSpawner : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] private GameObject m_itemPrefab;

#pragma warning restore 0649

        private GameObject m_spawnedItem;

        private void Awake()
        {
            Spawn();
        }

        private void OnTriggerExit(Collider collider)
        {
            Transform trans = m_spawnedItem.transform;
            do
            {
                if (trans.gameObject == m_spawnedItem)
                {
                    Spawn();
                    break;
                }

                trans = trans.parent;
            } while (trans);
        }

        private void Spawn()
        {
#if UNITY_5_4_OR_NEWER
            GameObject obj = Instantiate(m_itemPrefab, transform);
#else
            GameObject obj = Instantiate(m_itemPrefab);
            obj.transform.SetParent(transform, false);
#endif
            m_spawnedItem = obj;
        }
    }
}