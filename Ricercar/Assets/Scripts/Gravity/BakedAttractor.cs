using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class BakedAttractor : MonoBehaviour, IBakedAttractor
    {
        [SerializeField]
        private GravityMap m_gravityMap;

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
        [HideInInspector]
        private Transform m_transform;

        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public Vector2 Position => m_transform.position;
        public Vector2 Velocity => m_rigidbody.velocity;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        public Texture2D Texture => m_gravityMap.Texture;
        public Vector2 CentreOfGravity => m_gravityMap.CentreOfGravity;
        public float Rotation => m_transform.eulerAngles.z;

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;

            if (m_rigidbody != null)
                m_rigidbody.AddForce(m_currentGravity);
        }
        private void Reset()
        {
            m_transform = transform;
            m_rigidbody = GetComponent<Rigidbody2D>();
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

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            m_transform = transform;
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Position + m_gravityMap.CentreOfGravity, 0.4f);
        }
#endif
    }

    public interface IBakedAttractor : IAttractor
    {
        Texture2D Texture { get; }
        Vector2 CentreOfGravity { get; }
        float Rotation { get; }
    }
}