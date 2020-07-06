using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.InverseKinematics;

namespace Ricercar.Character
{
    public class LanderGimbal : Gimbal
    {
        [SerializeField]
        private InverseKinematicLegSystem m_legSystem;

        protected override void OnGravityChanged()
        {
            transform.up = -m_currentGravity.normalized;
        }

        private void FixedUpdate()
        {
            m_legSystem.TryLand();
        }
    }
}