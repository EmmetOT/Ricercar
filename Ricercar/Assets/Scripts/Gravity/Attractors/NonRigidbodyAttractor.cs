using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public class NonRigidbodyAttractor : Attractor
    {
        [SerializeField]
        private float m_mass = 1f;
        public override float Mass => m_mass;

        [SerializeField]
        private bool m_drawGizmos = true;

        public override Vector2 Position => m_transform.position;


        private Vector2 m_velocity = Vector2.zero;
        public override Vector2 Velocity => m_velocity;

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

        protected override void Reset()
        {
            base.Reset();

            CalculateSurfaceGravity();
        }

        private void OnValidate()
        {
            CalculateSurfaceGravity();
        }

        public override void SetMass(float mass)
        {
            base.SetMass(mass);

            m_mass = mass;
        }

        public void AddVelocity(Vector2 velocity)
        {
            m_velocity += velocity;
        }

        public void SetPosition(Vector2 position)
        {
            m_transform.position = position;
        }

        private void FixedUpdate()
        {
            if (!ApplyForceToSelf)
                return;

            m_velocity += (CurrentGravity * Time.deltaTime) / m_mass;
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

        public override Vector2 GetAttractionFromPosition(Vector2 pos, float mass)
        {
            // todo: implement the shell part of this
            Vector2 displacement = ((Vector2)m_transform.position - pos);
            float sqrDist = displacement.sqrMagnitude;

            return displacement.normalized * (GravityField.G * mass / sqrDist);
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