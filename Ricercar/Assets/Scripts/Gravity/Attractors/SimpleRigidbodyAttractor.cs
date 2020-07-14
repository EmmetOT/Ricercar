using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public class SimpleRigidbodyAttractor : Attractor, ISimpleAttractor
    {
        [GravityLayer]
        public int derp;

        [SerializeField]
        private bool m_useRigidbodyPosition = false;

        [SerializeField]
        private bool m_drawGizmos = true;

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

        public override float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public override Vector2 Position => m_useRigidbodyPosition ? m_rigidbody.position : (Vector2)m_transform.position;
        public override Vector2 Velocity => m_rigidbody.velocity;

        protected override void Reset()
        {
            base.Reset();

            m_rigidbody = GetComponent<Rigidbody2D>();
            CalculateSurfaceGravity();
        }

        private void OnValidate()
        {
            CalculateSurfaceGravity();
        }

        public override void SetGravity(Vector2 gravity)
        {
            base.SetGravity(gravity);

            m_rigidbody.AddForce(CurrentGravity);
        }

        public override void SetMass(float mass)
        {
            base.SetMass(mass);

            m_mass = mass;

            if (m_useRigidbodyMass)
                m_rigidbody.mass = Mathf.Abs(m_mass);
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