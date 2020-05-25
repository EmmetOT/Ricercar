using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class LineAttractor : MonoBehaviour, ILineAttractor
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

        [SerializeField]
        [BoxGroup("Line")]
        private Transform m_startPoint;

        [SerializeField]
        [BoxGroup("Line")]
        private Transform m_endPoint;

        public Vector2 Start => m_startPoint.position;
        public Vector2 End => m_endPoint.position;
        public Vector2 Position => m_transform.position;
        public Vector2 Velocity => m_rigidbody.velocity;
        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;
            m_rigidbody.AddForce(m_currentGravity);
        }

        private void Reset()
        {
            m_rigidbody = GetComponent<Rigidbody2D>();
            m_transform = transform;
        }

        private void Awake()
        {
            m_transform = transform;
        }

        public void OnEnable()
        {
            m_gravityField.RegisterAttractor(this);
        }

        public void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        [Button("Print GPU Data")]
        public void PrintGPUData()
        {
            Debug.Log(new AttractorData(this));
        }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_startPoint.position, 0.2f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(m_endPoint.position, 0.2f);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(m_transform.position, 0.1f);

            Gizmos.DrawLine(m_startPoint.position, m_endPoint.position);
        }
#endif
    }
}