using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

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

        protected override void OnMovementSet()
        {
            base.OnMovementSet();

            if (m_currentMovement != Vector2.zero)
                m_transform.rotation = Quaternion.FromToRotation(Vector2.up, m_currentMovement);
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
    }
}