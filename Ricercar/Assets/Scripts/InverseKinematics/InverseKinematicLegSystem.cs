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
        [MinValue(0f)]
        private float m_groundDistance = 1.5f;

        [SerializeField]
        private InverseKinematicLeg m_legOne;

        [SerializeField]
        private InverseKinematicLeg m_legTwo;

        private Vector2 LegDir => (m_legTwo.transform.position - m_legOne.transform.position).normalized;
        private Vector2 BaseDir => (m_legTwo.RunForwardKinematics() - m_legOne.RunForwardKinematics()).normalized;
        private Vector2 GroundOffset => -transform.up * m_groundDistance;
        public Vector2 LegOneGroundPos => (Vector2)m_legOne.transform.position + GroundOffset;
        public Vector2 LegTwoGroundPos => (Vector2)m_legTwo.transform.position + GroundOffset;

        public void Update()
        {
            m_legOne.SetTarget(LegOneGroundPos);
            m_legTwo.SetTarget(LegTwoGroundPos);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(m_legOne.transform.position, 0.2f);
            Gizmos.DrawSphere(m_legTwo.transform.position, 0.2f);
        }

    }
}