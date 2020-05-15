using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Attractor : MonoBehaviour
    {
        [SerializeField]
        protected bool m_isStatic = false;
        public bool IsStatic => m_isStatic;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsFields => m_affectsField;

        [SerializeField]
        [HideIf("m_static")]
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

        [SerializeField]
        private bool m_tracePath = false;

        private List<Vector3> m_pathPoints = new List<Vector3>();

        private float m_pathTraceFrequency = 0.5f;

        private float m_pathTraceTimer = 0f;

        private void Reset()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            GravityField.AddAttractor(this);
        }

        private void OnDisable()
        {
            GravityField.RemoveAttractor(this);
        }

        private void Start()
        {
            if (!m_startingForce.IsZero() && !m_isStatic)
                m_rigidbody.AddForce(m_startingForce);

            if (m_tracePath)
            {
                m_pathTraceTimer = m_pathTraceFrequency;
            }
        }

        private void FixedUpdate()
        {
            if (m_tracePath)
            {
                m_pathTraceTimer -= Time.fixedDeltaTime;

                if (m_pathTraceTimer <= 0f)
                {
                    m_pathTraceTimer = m_pathTraceFrequency;

                    m_pathPoints.Add(m_rigidbody.position);
                }
            }

            if (!m_applyForceToSelf)
                return;

            Vector2 gravityForce = GravityField.GetGravity(m_rigidbody.position, this) * Mass;

            m_rigidbody.AddForce(gravityForce);
        }

        /// <summary>
        /// Returns the gravity vector from the given point to this attractor. Also returns the point on the attractor which the
        /// given point is being pulled towards.
        /// </summary>
        protected abstract Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos);

        /// <summary>
        /// Quick method to get just the gravity from the given position to this attractor.
        /// </summary>
        public virtual Vector2 CalculateGravitationalForce(Vector2 position, bool checkNeutralizers = false)
        {
            if (!enabled)
                return Vector2.zero;

            Vector2 gravity = GetGravityVector(position, out Vector2 gravitySource);

            if (checkNeutralizers && Neutralizer.IsNeutralized(position, gravitySource))
                return Vector2.zero;

            float sqrMagnitude = gravity.sqrMagnitude;

            if (sqrMagnitude == 0f)
                return Vector2.zero;

            Vector2 direction = gravity.normalized;

            float forceMagnitude = GravityField.G * Mass / sqrMagnitude;

            return direction * forceMagnitude;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (Application.isPlaying && m_tracePath)
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < m_pathPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(m_pathPoints[i], m_pathPoints[i + 1]);
                }
            }

            if (!Application.isPlaying && !m_startingForce.IsZero())
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(m_rigidbody.position, m_rigidbody.position + m_startingForce);
            }
        }
#endif
    }
}