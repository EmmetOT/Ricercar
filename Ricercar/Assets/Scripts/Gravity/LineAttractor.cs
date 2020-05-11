using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class LineAttractor : Attractor
    {
        [SerializeField]
        private Transform m_startPoint;

        [SerializeField]
        private Transform m_endPoint;

        [BoxGroup("Dead Zone")]
        [SerializeField]
        [MinValue(0f)]
        private float m_deadZoneMagnitude = 0.1f;

        [BoxGroup("Dead Zone")]
        [SerializeField]
        [MinValue(0f)]
        private float m_deadZoneSteepness = 1000f;

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (m_startPoint == null || m_endPoint == null)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_startPoint.position, m_endPoint.position);
        }
#endif

        public override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        {
            sourcePos = Utils.ProjectPointOnLineSegment(m_startPoint.position, m_endPoint.position, from);
            Vector2 result = sourcePos - from;

            float deadzoneDist = result.magnitude - m_deadZoneMagnitude;

            if (deadzoneDist <= 0f)
                return Vector2.Lerp(result, result.normalized * m_deadZoneSteepness, Mathf.InverseLerp(0f, -m_deadZoneMagnitude, deadzoneDist));

            return result;
        }
    }
}