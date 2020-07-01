using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;
using UnityEditor;

namespace Ricercar.Character
{
    public class WarpGimbal : Gimbal
    {
        [SerializeField]
        private BakedAttractor m_positiveAttractor;

        [SerializeField]
        private BakedAttractor m_negativeAttractor;

        [SerializeField]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        private float m_targetSpeed = 5f;

        [SerializeField]
        [MinValue(0f)]
        [OnValueChanged("OnWarpMassSet")]
        private float m_warpMass = 0.1f;

        [SerializeField]
        [MinValue(0f)]
        private float m_rotationSpeed = 100f;

        private void Update()
        {
            m_transform.up = Vector2.Lerp(m_transform.up, m_currentMovement, m_rotationSpeed * Time.deltaTime).normalized;
        }

        private void FixedUpdate()
        {
            if (Vector2.Dot(m_transform.up, m_rigidbody.velocity) >= m_targetSpeed)
                SetMass(0f);
            else
                SetMass(m_warpMass);
        }

        public void SetMass(float mass)
        {
            m_positiveAttractor.SetMass(mass);
            m_negativeAttractor.SetMass(-mass);
        }

        private void OnWarpMassSet()
        {
            SetMass(m_warpMass);
        }

        private void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.right * 5f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_currentMovement * 5f);
        }
    }
}