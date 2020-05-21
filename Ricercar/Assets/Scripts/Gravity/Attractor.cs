using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public interface IAttractor
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        float Mass { get; }
        bool AffectsField { get; }

        void SetGravity(Vector2 gravity);
    }

    public struct AttractorData
    {
        public const int Stride = 16;

        public float x;
        public float y;
        public int ignore;
        public float mass;

        public AttractorData(IAttractor attractor)
        {
            Vector2 pos = attractor.Position;

            x = pos.x;
            y = pos.y;
            ignore = attractor.AffectsField ? 0 : 1;
            mass = attractor.Mass;
        }
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Attractor : MonoBehaviour, IAttractor
    {
        //[SerializeField]
        //protected bool m_isStatic = false;
        //public bool IsStatic => m_isStatic;

        [SerializeField]
        protected GravityField m_gravityField;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField;

        [SerializeField]
        private Vector2 m_startingForce;

        [SerializeField]
        private Rigidbody2D m_rigidbody;
        public Rigidbody2D Rigidbody => m_rigidbody;

        [SerializeField]
        private bool m_useRigidbodyMass;

        [SerializeField]
        [HideIf("m_useRigidbodyMass")]
        private float m_mass;

        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public Vector2 Position => m_rigidbody.position;
        public Vector2 Velocity => m_rigidbody.velocity;

        public Vector2 CurrentGravity
        {
            get;
            private set;
        }

        private void Reset()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
        }

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            CurrentGravity = gravity;
            m_rigidbody.AddForce(CurrentGravity);
        }

        //        /// <summary>
        //        /// Returns the gravity vector from the given point to this attractor. Also returns the point on the attractor which the
        //        /// given point is being pulled towards.
        //        /// </summary>
        //        protected abstract Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos);

        //        /// <summary>
        //        /// Quick method to get just the gravity from the given position to this attractor.
        //        /// </summary>
        //        public virtual Vector2 CalculateGravitationalForce(Vector2 position, bool checkNeutralizers = false)
        //        {
        //            if (!enabled)
        //                return Vector2.zero;

        //            Vector2 gravity = GetGravityVector(position, out Vector2 gravitySource);

        //            if (checkNeutralizers && Neutralizer.IsNeutralized(position, gravitySource))
        //                return Vector2.zero;

        //            float sqrMagnitude = gravity.sqrMagnitude;

        //            if (sqrMagnitude == 0f)
        //                return Vector2.zero;

        //            Vector2 direction = gravity.normalized;

        //            float forceMagnitude = GravityField.G * Mass / sqrMagnitude;

        //            return direction * forceMagnitude;
        //        }

        //#if UNITY_EDITOR
        //        protected virtual void OnDrawGizmos()
        //        {
        //            if (Application.isPlaying && m_tracePath)
        //            {
        //                Gizmos.color = Color.white;
        //                for (int i = 0; i < m_pathPoints.Count - 1; i++)
        //                {
        //                    Gizmos.DrawLine(m_pathPoints[i], m_pathPoints[i + 1]);
        //                }
        //            }

        //            if (!Application.isPlaying && !m_startingForce.IsZero())
        //            {
        //                Gizmos.color = Color.white;
        //                Gizmos.DrawLine(m_rigidbody.position, m_rigidbody.position + m_startingForce);
        //            }
        //        }
        //#endif
    }
}