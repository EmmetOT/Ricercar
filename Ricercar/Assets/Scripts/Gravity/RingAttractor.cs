using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public class RingAttractor : MonoBehaviour, IRingAttractor
    {
        [SerializeField]
        protected GravityField m_gravityField;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField;

        //[SerializeField]
        //private Vector2 m_startingForce;

        //[SerializeField]
        //private Rigidbody2D m_rigidbody;
        //public Rigidbody2D Rigidbody => m_rigidbody;

        //[SerializeField]
        //private bool m_useRigidbodyMass;

        [SerializeField]
        private float m_mass;

        [SerializeField]
        [MinValue(0f)]
        private float m_radius = 0f;
        public float Radius => m_radius;

        [SerializeField]
        [BoxGroup("Ring")]
        [MinMaxSlider(0f, 360f)]
        private Vector2 m_angleSpan = Vector2.zero;

        public float StartAngle => m_angleSpan.x - m_transform.eulerAngles.z;
        public float EndAngle => m_angleSpan.y - m_transform.eulerAngles.z;

        public Vector2 StartDirection => Utils.RotateAround(Vector3.back * StartAngle);
        public Vector2 EndDirection => Utils.RotateAround(Vector3.back * EndAngle);

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        public float Mass => /*m_useRigidbodyMass ? m_rigidbody.mass : */m_mass;
        public Vector2 Position => m_transform.position;
        public Vector2 Velocity => Vector2.zero;/*m_rigidbody.velocity;*/

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        private void Reset()
        {
            m_currentGravity = Vector2.zero;
            m_transform = transform;
            //m_rigidbody = GetComponent<Rigidbody2D>();
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
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;
            //m_rigidbody.AddForce(m_currentGravity);
        }



        //        protected override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        //        {
        //            Vector2 directionFromCentreToFrom = (Rigidbody.position - from).normalized;
        //            sourcePos = Rigidbody.position - directionFromCentreToFrom * m_radius;

        //            return sourcePos - from;
        //        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            Handles.color = Color.white;
            Handles.DrawWireArc(Position, Vector3.back, StartDirection, EndAngle - StartAngle, Radius);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(Position + StartDirection * Radius, 0.3f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Position + EndDirection * Radius, 0.3f);
        }
#endif
    }
}
