using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Attractor : MonoBehaviour
    {
        public const float G = 667.4f;//6.673e-11f;

        private static List<Attractor> m_attractors = new List<Attractor>();

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

        [SerializeField]
        private bool m_applyForceToSelf = true;

        [SerializeField]
        private bool m_affectsField = true;

        public Vector2 CurrentGravity => GetGravityAtPoint(this);

        [SerializeField]
        private bool m_tracePath = false;

        private List<Vector3> m_pathPoints = new List<Vector3>();

        private float m_pathTraceFrequency = 0.5f;

        private float m_pathTraceTimer = 0f;

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
            if (!m_startingForce.IsZero())
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

            Vector3 gravityForce = GetGravityAtPoint(m_rigidbody.position, Mass, ignore: this);
            m_rigidbody.AddForce(gravityForce);
        }
        
        public static Vector3 GetGravityAtPoint(Attractor attractor)
        {
            return GetGravityAtPoint(attractor.Position, attractor.Mass, attractor);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && m_tracePath)
            {
                Gizmos.color = Color.white;
                for (int i = 0; i < m_pathPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(m_pathPoints[i], m_pathPoints[i + 1]);
                }
            }

            if (!m_startingForce.IsZero())
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(m_rigidbody.position, m_rigidbody.position + m_startingForce);
            }
        }
#endif

        public static Vector3 GetGravityAtPoint(Vector2 position, float mass = 1f, Attractor ignore = null)
        {
            Vector2 result = Vector2.zero;

            for (int i = 0; i < m_attractors.Count; i++)
            {
                Attractor attractor = m_attractors[i];

                if ((ignore != null && attractor == ignore) || !attractor.m_affectsField)
                    continue;

                Rigidbody2D attractorRigidbody = attractor.Rigidbody;
                float attractorMass = attractor.Mass;

                Vector2 difference = attractorRigidbody.position - position;
                float distance = difference.magnitude;

                if (distance == 0f)
                    continue;
                
                Vector2 direction = difference.normalized;

                float forceMagnitude = G * (mass * attractorMass) / (distance * distance);
                Vector3 force = direction * forceMagnitude;

                result += direction * forceMagnitude;
            }

            return result;
        }

        [Button]
        private void PrintCurrentGravity()
        {
            Debug.Log(CurrentGravity);
        }
    }
}