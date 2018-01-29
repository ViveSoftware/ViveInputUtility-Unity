//========= Copyright 2016-2018, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.Vive
{
    public interface IViveRoleComponent
    {
        ViveRoleProperty viveRole { get; }
    }

    /// <summary>
    /// This component sync its role to all child component that is an IViveRoleComponent
    /// </summary>
    public class ViveRoleSetter : MonoBehaviour
    {
        private static List<IViveRoleComponent> s_comps = new List<IViveRoleComponent>();

        [SerializeField]
        private ViveRoleProperty m_viveRole = ViveRoleProperty.New(HandRole.RightHand);

        public ViveRoleProperty viveRole { get { return m_viveRole; } }
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            // get role from first found component
            var comp = GetComponentInChildren<IViveRoleComponent>();
            if (comp != null)
            {
                m_viveRole.Set(comp.viveRole);
            }
        }

        protected virtual void OnValidate()
        {
            UpdateChildrenViveRole();
        }
#endif
        protected virtual void Awake()
        {
            m_viveRole.onRoleChanged += UpdateChildrenViveRole;
        }

        public void UpdateChildrenViveRole()
        {
            GetComponentsInChildren(true, s_comps);
            for (int i = 0; i < s_comps.Count; ++i)
            {
                s_comps[i].viveRole.Set(m_viveRole);
            }
            s_comps.Clear();
        }

        protected virtual void OnDestroy()
        {
            m_viveRole.onRoleChanged -= UpdateChildrenViveRole;
        }
    }
}