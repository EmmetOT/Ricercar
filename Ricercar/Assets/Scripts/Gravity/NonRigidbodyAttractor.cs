using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class NonRigidbodyAttractor : MonoBehaviour, IAttractor
    {
        [SerializeField]
        private GravityField m_gravityField;

        [SerializeField]
        private float m_mass = 1f;
        public float Mass => m_mass;

        [SerializeField]
        private bool m_applyForceToSelf = true;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField;

        [SerializeField]
        private Transform m_transform;

        public Vector2 Position => m_transform.position;

        private Vector2 m_currentGravity = Vector2.zero;

        private Vector2 m_velocity = Vector2.zero;
        public Vector2 Velocity => m_velocity;

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
        private float m_surfaceGravityForce = Mathf.Infinity;
        public float SurfaceGravityForce => (m_isShell && m_radius > 0f) ? m_surfaceGravityForce : 1f;

        private void Reset()
        {
            m_transform = transform;
            CalculateSurfaceGravity();
        }

        private void OnValidate()
        {
            CalculateSurfaceGravity();
        }

        public void Awake()
        {
            m_transform = transform;
        }

        public void OnEnable()
        {
            if (m_gravityField == null)
                m_gravityField = FindObjectOfType<GravityField>();

            m_gravityField.RegisterAttractor(this);
        }

        public void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;
        }

        public void AddVelocity(Vector2 velocity)
        {
            m_velocity += velocity;
        }

        public void SetPosition(Vector2 position)
        {
            m_transform.position = position;
        }

        private void Update()
        {
            if (!m_applyForceToSelf)
                return;

            m_velocity += m_currentGravity * Time.deltaTime;
            m_transform.position += (Vector3)m_velocity * Time.deltaTime;
        }

        [Button]
        public void CalculateSurfaceGravity()
        {
            if (!m_isShell)
            {
                m_surfaceGravityForce = Mathf.Infinity;
                return;
            }

            m_surfaceGravityForce = GravityField.G * m_mass / (m_radius * m_radius);
        }
    }
}