using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace Ricercar.Gravity
{
    public interface IAttractor
    {
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        Vector2 CurrentGravity { get; }
        float Mass { get; }
        bool AffectsField { get; }

        void SetGravity(Vector2 gravity);
    }

    public interface ISimpleAttractor : IAttractor
    {
        float Radius { get; }
        float SurfaceGravityForce { get; }
    }

    public interface ILineAttractor : IAttractor
    {
        Vector2 Start { get; }
        Vector2 End { get; }
    }

    public struct AttractorData
    {
        // 11 * 4
        public const int Stride = 44;

        public float x;
        public float y;
        public int ignore;
        public float mass;
        public float radius;
        public float surfaceGravityForce;

        public int isLine;
        public float lineStartX;
        public float lineStartY;
        public float lineEndX;
        public float lineEndY;

        public AttractorData(IAttractor attractor)
        {
            Vector2 pos = attractor.Position;

            x = pos.x;
            y = pos.y;
            ignore = attractor.AffectsField ? 0 : 1;
            mass = attractor.Mass;

            if (!(attractor is ISimpleAttractor simple))
            {
                radius = 0f;
                surfaceGravityForce = 1f;
            }
            else
            {
                radius = simple.Radius;
                surfaceGravityForce = simple.SurfaceGravityForce;
            }

            if (!(attractor is ILineAttractor line))
            {
                isLine = 0;
                lineStartX = 0f;
                lineStartY = 0f;
                lineEndX = 1f;
                lineEndY = 1f;
            }
            else
            {
                isLine = 1;
                lineStartX = line.Start.x;
                lineStartY = line.Start.y;
                lineEndX = line.End.x;
                lineEndY = line.End.y;
            }
        }

        public override string ToString()
        {
            return $"x:\t{x}\ny:\t{y}\nignore:\t{ignore}\nmass:\t{mass}\nradius:\t{radius}\nsurfaceGravityForce:\t{surfaceGravityForce}\nisLine:\t{isLine}\nlineStartX:\t{lineStartX}\nlineStartY:\t{lineStartY}\nlineEndX:\t{lineEndX}\nlineEndY:\t{lineEndY}";
        }
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Attractor : MonoBehaviour, IAttractor
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

        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public Vector2 Position => transform.position;
        public Vector2 Velocity => m_rigidbody.velocity;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        private void Reset()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            CalculateSurfaceGravity();
        }

        private void OnValidate()
        {
            CalculateSurfaceGravity();
        }

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;
            m_rigidbody.AddForce(m_currentGravity);
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

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (m_isShell)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(transform.position, Vector3.back, m_radius);
            }
        }
#endif
    }
}