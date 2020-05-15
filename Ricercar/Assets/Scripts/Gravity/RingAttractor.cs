using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public class RingAttractor : Attractor
    {
        [SerializeField]
        [BoxGroup("Ring")]
        [MinValue(0f)]
        private float m_radius = 1f;

        protected override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        {
            Vector2 directionFromCentreToFrom = (Rigidbody.position - from).normalized;
            sourcePos = Rigidbody.position - directionFromCentreToFrom * m_radius;

            return sourcePos - from;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            Handles.color = Color.white;
            Handles.DrawWireDisc(Rigidbody.position, Vector3.back, m_radius);
        }
#endif
    }
}
