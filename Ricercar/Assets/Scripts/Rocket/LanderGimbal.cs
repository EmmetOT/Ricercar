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

        private void LateUpdate()
        {
            m_transform.up = -m_controller.CurrentGravityWithoutWarp.normalized;
            m_legSystem.TryLand();
        }
    }
}