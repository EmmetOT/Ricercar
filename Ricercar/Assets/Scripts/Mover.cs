using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar
{
    [RequireComponent(typeof(Timer))]
    public class Mover : MonoBehaviour
    {
        private Vector3 m_from;
        private Vector3 m_to;

        [SerializeField]
        private Vector3 m_offset;

        [SerializeField]
        private float m_period = 1f;

        private Timer m_timer;
        private Transform m_transform;

        private void Reset()
        {
            m_transform = transform;
            m_to = m_transform.localPosition;
            m_from = m_transform.localPosition;
            m_offset = Vector3.zero;
        }

        private void Start()
        {
            m_timer = GetComponent<Timer>();
            m_transform = transform;

            m_from = m_transform.localPosition;
            m_to = m_from + m_offset;

            m_timer.StartRepeatingSine(0, SetPos, m_period, 0f, 1f);
        }

        private void SetPos(float t)
        {
            m_transform.localPosition = Vector3.Lerp(m_from, m_to, t);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.localPosition, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.localPosition + m_offset, 0.3f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.localPosition, transform.localPosition + m_offset);
        }
#endif
    }
}