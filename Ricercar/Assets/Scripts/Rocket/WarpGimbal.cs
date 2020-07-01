using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;
using UnityEditor;

namespace Ricercar.Character
{
    /// <summary>
    /// This component will always try to get the attached rigidbody moving at a specific velocity.
    /// </summary>
    public class WarpGimbal : Gimbal
    {
        [SerializeField]
        private Transform m_positiveWarpPivot;

        [SerializeField]
        private Transform m_negativeWarpPivot;

        [SerializeField]
        private NonRigidbodyAttractor m_positiveAttractor;

        [SerializeField]
        private NonRigidbodyAttractor m_negativeAttractor;

        [SerializeField]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        private float m_targetSpeed = 5f;

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("How many seconds will it take to reach max speed?")]
        private float m_accelerationTime = 1f;

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("How many seconds will it take to reach stop when the hitting the breaks?")]
        private float m_stopTime = 1f;

        [SerializeField]
        [MinValue(0f)]
        [OnValueChanged("OnWarpMassSet")]
        private float m_warpMass = 0.1f;
        
        [SerializeField]
        [MinValue(0f)]
        private float m_rotationSpeed = 100f;

        [ReadOnly]
        [SerializeField]
        private float m_normalizedPositiveMass;

        [ReadOnly]
        [SerializeField]
        private float m_normalizedNegativeMass;

        [BoxGroup("Visuals")]
        [SerializeField]
        private SpriteRenderer m_positiveSprite;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_minPositiveSpriteColour;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_maxPositiveSpriteColour;
        
        private bool m_blocked = false;
        private bool m_stopping = false;

        private void Start()
        {
            // set the positive attractors mass such that it accelerates to the target speed in the target amount of time
            m_normalizedPositiveMass = 1f / m_positiveAttractor.GetGravityFromMass(m_rigidbody.position, 1f).magnitude;
            m_normalizedNegativeMass = 1f / m_negativeAttractor.GetGravityFromMass(m_rigidbody.position, 1f).magnitude;

            Debug.Log("Setting mass to " + (m_normalizedPositiveMass));

            m_positiveAttractor.SetMass(m_targetSpeed * m_normalizedPositiveMass / m_accelerationTime);
        }

        private void Update()
        {
            // current situation: trying to determine what mass to give the positive attractor and where to position it so that i can turn back and forth
            // on one axis

            Vector2 desiredMovement = new Vector2(0f, -1f);

            Debug.Log("Desired Movement = " + m_desiredMovement);

            Vector2 acceleration = (m_desiredMovement * m_targetSpeed) - m_rigidbody.velocity;

            //m_positiveAttractor.gameObject.SetActive(acceleration.magnitude != 0f);

            m_positiveWarpPivot.up = acceleration.normalized;
            //m_negativeWarpPivot.up = -acceleration.normalized;

            float positiveMass = 0.5f * acceleration.magnitude * m_normalizedPositiveMass / m_accelerationTime;
            //float negativeMass = -0.5f * acceleration.magnitude * m_normalizedNegativeMass / m_accelerationTime;

            m_positiveAttractor.SetMass(positiveMass);
            //m_negativeAttractor.SetMass(negativeMass);

            m_positiveSprite.color = Color.Lerp(m_minPositiveSpriteColour, m_maxPositiveSpriteColour, Mathf.InverseLerp(0f, m_normalizedPositiveMass * m_targetSpeed, positiveMass));
        }

        public void Stop()
        {
            m_positiveAttractor.gameObject.SetActive(false);
            m_positiveWarpPivot.up = Vector2.up;
            m_blocked = false;
        }

        public void StopStop()
        {
            m_negativeAttractor.gameObject.SetActive(false);
            m_negativeWarpPivot.up = Vector2.up;
            m_stopping = false;
        }

        private void FixedUpdate()
        {
            //if (Vector2.Dot(m_transform.up, m_rigidbody.velocity) >= m_targetSpeed)
            //    SetMass(0f);
            //else
                //SetMass(m_warpMass);
        }

        public void SetMass(float mass)
        {
            m_positiveAttractor.SetMass(mass);
            m_negativeAttractor.SetMass(-mass);
        }

        // called by inspector
        private void OnWarpMassSet()
        {
            SetMass(m_warpMass);
        }

        private void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying)
                return;

            //Vector2 attractorGrav = m_rigidbody.mass * m_positiveAttractor.GetGravityFrom(m_rigidbody.position);

            Utils.Label((Vector2)m_transform.position + Vector2.up * 1f, m_rigidbody.velocity.magnitude.ToString(), 13, Color.white);
            //Utils.Label((Vector2)m_transform.position + Vector2.up * 1.4f, (m_normalizedPositiveMass / m_normalizedPositiveMass.magnitude).ToString(), 13, Color.green);
            //Utils.Label((Vector2)m_transform.position + Vector2.up * 1.2f, attractorGrav.ToString(), 13, Color.red);

            // draw current velocity as cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_rigidbody.velocity.normalized * 2f);

            // draw desired velocity as magenta
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_desiredMovement.normalized * 2f);
        }
    }
}