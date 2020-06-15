using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public class SimpleRigidbodyAttractor : MonoBehaviour, ISimpleAttractor
    {
        [SerializeField]
        protected GravityField m_gravityField;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField;

        [SerializeField]
        private bool m_useRigidbodyPosition = false;

        [SerializeField]
        private bool m_drawGizmos = true;

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

        [SerializeField]
        private bool m_isShell = false;

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_isShell")]
        private float m_radius = 0f;
        public float Radius => m_isShell ? m_radius : 0f;

        [SerializeField]
        [ReadOnly]
        [ShowIf("m_isShell")]
        private float m_surfaceGravityForce = 1f;
        public float SurfaceGravityForce => (m_isShell && m_radius > 0f) ? m_surfaceGravityForce : 1f;

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public Vector2 Position => m_useRigidbodyPosition ? m_rigidbody.position : (Vector2)m_transform.position;
        public Vector2 Velocity => m_rigidbody.velocity;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        private void Reset()
        {
            m_transform = transform;
            m_rigidbody = GetComponent<Rigidbody2D>();
            CalculateSurfaceGravity();
        }

        private void OnValidate()
        {
            CalculateSurfaceGravity();
        }

        private void OnEnable()
        {
            m_transform = transform;
            m_gravityField.RegisterAttractor(this);
        }

        private void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        public void SetGravity(Vector2 gravity)
        {
            m_currentGravity = gravity;

            if (!m_applyForceToSelf)
                return;

            m_rigidbody.AddForce(m_currentGravity * m_gravityField.GravityDeltaTime);
        }

        [Button("Calculate Surface Gravity")]
        public void CalculateSurfaceGravity()
        {
            if (!m_isShell)
            {
                m_surfaceGravityForce = Mathf.Infinity;
                return;
            }

            m_surfaceGravityForce = GravityField.G * m_mass / (m_radius * m_radius);
        }

        [Button("Print GPU Data")]
        public void PrintGPUData()
        {
            Debug.Log(new AttractorData(this));
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (!m_drawGizmos)
                return;

            if (m_isShell)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(transform.position, Vector3.back, m_radius);
            }

            if (!CurrentGravity.IsZero())
                Utils.DrawArrow(Position, CurrentGravity.normalized, Color.white, CurrentGravity.magnitude * 0.5f, 3f);
        }
#endif
    }
}