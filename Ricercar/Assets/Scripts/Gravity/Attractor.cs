using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Attractor : MonoBehaviour
    {
        public const float G = 667.4f;

        private static List<Attractor> m_attractors = new List<Attractor>();


        [SerializeField]
        protected bool m_isStatic = false;
        public bool IsStatic => m_isStatic;


        //[SerializeField]
        //private LayerMask m_affectLayers;

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
        private bool m_applyForceToSelf = true;

        [SerializeField]
        private bool m_affectsField = true;

        public Vector2 CurrentGravity => GetGravity(this);

        [SerializeField]
        private bool m_tracePath = false;

        private List<Vector3> m_pathPoints = new List<Vector3>();

        private float m_pathTraceFrequency = 0.5f;

        private float m_pathTraceTimer = 0f;

        /// <summary>
        /// Returns the gravity vector from the given point to this attractor. Also returns the point on the attractor which the
        /// given point is being pulled towards.
        /// </summary>
        public abstract Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos);

        private void OnEnable()
        {
            m_attractors.Add(this);
        }

        private void OnDisable()
        {
            m_attractors.Remove(this);
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

            Vector2 gravityForce = GetGravity(m_rigidbody.position, ignore: this) * Mass;

            m_rigidbody.AddForce(gravityForce);
        }

        public static Vector2 GetGravity(Vector2 position, Attractor ignore = null)
        {
            Vector2 gravityForce = Vector3.zero;

            gravityForce += GravityField.GetGravity(position);
            gravityForce += GetDynamicGravity(position, ignore: ignore);

            return gravityForce;
        }
        
        public static Vector2 GetGravity(Attractor attractor)
        {
            return GetGravity(attractor.Position, attractor);
        }

        public static Vector2 GetDynamicGravity(Vector2 position, Attractor ignore = null)
        {
            Vector2 result = Vector2.zero;

            for (int i = 0; i < m_attractors.Count; i++)
            {
                Attractor attractor = m_attractors[i];

                if (attractor.IsStatic)
                    continue;

                if ((ignore != null && attractor == ignore) || !attractor.m_affectsField)
                    continue;
                
                float attractorMass = attractor.Mass;

                Vector2 difference = attractor.GetGravityVector(position, out Vector2 gravitySource);

                if (Neutralizer.IsNeutralized(position, gravitySource))
                    continue;

                float sqrMagnitude = difference.sqrMagnitude;

                if (sqrMagnitude == 0f)
                    continue;

                Vector2 direction = difference.normalized;

                float forceMagnitude = (G * attractorMass) / sqrMagnitude;

                result += direction * forceMagnitude;
            }

            return result;
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

        /// <summary>
        /// Quick method to get just the gravity from the given position to this attractor.
        /// </summary>
        public Vector2 GetGravity(Vector2 position, bool checkNeutralizers = false)
        {
            Vector2 difference = GetGravityVector(position, out Vector2 gravitySource);
            
            if (checkNeutralizers && Neutralizer.IsNeutralized(position, gravitySource))
                return Vector2.zero;
            
            float sqrMagnitude = difference.sqrMagnitude;

            if (sqrMagnitude == 0f)
                return Vector2.zero;

            Vector2 direction = difference.normalized;
            
            float forceMagnitude = G * Mass / sqrMagnitude;

            return direction * forceMagnitude;
        }

        [Button]
        private void PrintCurrentGravity()
        {
            Debug.Log(CurrentGravity);
        }
    }
}