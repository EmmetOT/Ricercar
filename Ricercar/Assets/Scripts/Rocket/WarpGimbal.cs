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
        [MinValue(0f)]
        [OnValueChanged("OnWarpMassSet")]
        private float m_warpMass = 0.1f;

        protected override void OnMovementSet()
        {
            base.OnMovementSet();

            m_transform.rotation = Quaternion.FromToRotation(Vector2.up, m_currentMovement);
        }

        public void SetMass(float mass)
        {
            m_warpMass = mass;

            m_positiveAttractor.SetMass(mass);
            m_negativeAttractor.SetMass(-mass);
        }

        private void OnWarpMassSet()
        {
            SetMass(m_warpMass);
        }
    }
}