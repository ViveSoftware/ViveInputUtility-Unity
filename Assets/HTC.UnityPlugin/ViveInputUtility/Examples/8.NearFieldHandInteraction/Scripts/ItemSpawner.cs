//========= Copyright 2016-2020, HTC Corporation. All rights reserved. ===========

using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject m_itemPrefab;

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
            GameObject obj = Instantiate(m_itemPrefab, transform);
            m_spawnedItem = obj;
        }
    }
}