using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using NaughtyAttributes;

namespace Ricercar.InverseKinematics
{
    public class InverseKinematicLegSystem : MonoBehaviour
    {
        [SerializeField]
        private InverseKinematicLeg m_legOne;

        [SerializeField]
        private InverseKinematicLeg m_legTwo;

        private Vector2 m_legOneGroundTarget;
        private Vector2 m_legTwoGroundTarget;

        private bool m_hasGroundTarget = false;
        public bool HasGroundTarget => m_hasGroundTarget;

        public void TryLand()
        {
            RaycastForGroundTargets();

            if (!m_hasGroundTarget)
                return;

            m_legOne.SetTarget(m_legOneGroundTarget);
            m_legTwo.SetTarget(m_legTwoGroundTarget);

            m_legOne.RunInverseKinematics();
            m_legTwo.RunInverseKinematics();
        }

        private void RaycastForGroundTargets()
        {
            RaycastHit2D leftHit = Physics2D.Raycast(m_legOne.transform.position, -transform.up, m_legOne.MaxLength);
            RaycastHit2D rightHit = Physics2D.Raycast(m_legTwo.transform.position, -transform.up, m_legTwo.MaxLength);

            m_hasGroundTarget = leftHit.collider != null && rightHit.collider != null;

            m_legOneGroundTarget = leftHit.point;
            m_legTwoGroundTarget = rightHit.point;
        }

        private void OnDrawGizmos()
        {
            if (!m_hasGroundTarget)
                return;

            Gizmos.color = Color.red;

            Gizmos.DrawSphere(m_legOneGroundTarget, 0.2f);
            Gizmos.DrawSphere(m_legTwoGroundTarget, 0.2f);
        }

    }
}