using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    [RequireComponent(typeof(Collider2D))]
    public class Neutralizer : MonoBehaviour
    {
        [SerializeField]
        private Collider2D m_collider;

        [SerializeField]
        private bool m_isVolume = false;

        [SerializeField]
        [HideIf("m_isVolume")]
        private bool m_isOneWay = false;

        [SerializeField]
        [ShowIf("IsOneWay")]
        private Vector2 m_oneWayVector = Vector2.up;

        public bool IsOneWay => !m_isVolume && m_isOneWay;

        public Vector2 OneWayVector => transform.rotation * m_oneWayVector;

        private static List<Neutralizer> m_neutralizers = new List<Neutralizer>();

        private static RaycastHit2D[] m_results = new RaycastHit2D[100];

        private static ContactFilter2D m_contactFilter;

        private void Awake()
        {
            m_contactFilter.useLayerMask = false;
        }

        private void OnValidate()
        {
            if (!m_isVolume && m_isOneWay && !m_oneWayVector.IsZero())
                m_oneWayVector = m_oneWayVector.normalized;
        }

        private void OnEnable()
        {
            m_neutralizers.Add(this);
        }

        private void OnDisable()
        {
            m_neutralizers.Remove(this);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (m_isVolume || !m_isOneWay || OneWayVector.IsZero())
                return;

            Gizmos.color = Color.cyan;
            Utils.DrawArrow(transform.position, OneWayVector, Color.cyan, 1f, 1f);
        }
#endif

        public static bool IsNeutralized(Vector2 from, Vector2 to)
        {
            for (int i = 0; i < m_neutralizers.Count; i++)
            {
                Neutralizer neutralizer = m_neutralizers[i];

                if (!neutralizer.enabled)
                    continue;

                bool fromOverlapped = neutralizer.m_collider.OverlapPoint(from);
                bool toOverlapped = neutralizer.m_collider.OverlapPoint(to);

                if (neutralizer.m_isVolume)
                {
                    if (fromOverlapped != toOverlapped)
                        return true;
                }
                else
                {
                    if (fromOverlapped || toOverlapped)
                        return true;
                }
            }


            Vector2 direction = (to - from).normalized;
            int hits = Physics2D.Raycast(from, direction, m_contactFilter, m_results, Vector2.Distance(from, to));

            for (int i = 0; i < hits; i++)
            {
                if (m_results[i].collider.TryGetComponent(out Neutralizer other) && other.enabled)
                {
                    if (!other.m_isOneWay || other.OneWayVector.IsZero())
                        return true;

                    float dot = Vector2.Dot(other.OneWayVector, direction);

                    if (dot < 0f)
                        return true;
                }
            }


            return false;
        }
    }
}