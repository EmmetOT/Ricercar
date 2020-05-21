using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    /// <summary>
    /// A shell attractor is supposed to mimic the shell theorem: that as you approach the centre of a solid radially symmetric
    /// sphere (or circle in this case), the gravity around you cancels out. It turns out this scales linearly with distance from
    /// the surface to the centre.
    /// </summary>
    public class ShellAttractor : PointAttractor
    {
        [SerializeField]
        [BoxGroup("Shell")]
        [MinValue(0f)]
        private float m_radius = 1f;

//        public override Vector2 CalculateGravitationalForce(Vector2 position, bool checkNeutralizers = false)
//        {
//            Vector2 result = base.CalculateGravitationalForce(position, checkNeutralizers);

//            Vector2 centre = Rigidbody.position;

//            float sqrRadius = m_radius * m_radius;

//            float sqrMagnitude = (centre - position).sqrMagnitude;

//            // we're outside the surface so nothing fancy is required
//            if (result.IsZero() || sqrMagnitude >= sqrRadius)
//                return result;

//            // first we get the position at the surface from the direction supplied
//            Vector2 directionFromCentreToFrom = (centre - position).normalized;
//            Vector2 surfacePos = centre - directionFromCentreToFrom * m_radius;

//            Vector2 surfaceGravity = base.CalculateGravitationalForce(surfacePos, checkNeutralizers);

//            float distance = Mathf.Sqrt(sqrMagnitude);

//            return surfaceGravity * Mathf.InverseLerp(0f, m_radius, distance);
//        }


//#if UNITY_EDITOR
//        protected override void OnDrawGizmos()
//        {
//            base.OnDrawGizmos();

//            Handles.color = Color.white;
//            Handles.DrawWireDisc(Rigidbody.position, Vector3.back, m_radius);
//        }
//#endif
    }
}