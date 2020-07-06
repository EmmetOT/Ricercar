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
        private const float MIN_MASS_ABSOLUTE_VALUE = 0.000001f;

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
        
        [ReadOnly]
        [SerializeField]
        private float m_normalizedPositiveMass;

        [ReadOnly]
        [SerializeField]
        private float m_normalizedNegativeMass;

        [SerializeField]
        [ReadOnly]
        private float m_maxPositiveMass;

        [SerializeField]
        [ReadOnly]
        private float m_maxNegativeMass;


        [ReadOnly]
        [SerializeField]
        private float m_currentNormalizedPositiveMass;

        [ReadOnly]
        [SerializeField]
        private float m_currentNormalizedNegativeMass;


        [BoxGroup("Visuals")]
        [SerializeField]
        private SpriteRenderer m_positiveSprite;

        [BoxGroup("Visuals")]
        [SerializeField]
        private SpriteRenderer m_negativeSprite;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_minPositiveSpriteColour;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_maxPositiveSpriteColour;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_minNegativeSpriteColour;

        [BoxGroup("Visuals")]
        [SerializeField]
        private Color m_maxNegativeSpriteColour;

        private void Start()
        {
            // set the positive attractors mass such that it accelerates to the target speed in the target amount of time
            m_normalizedPositiveMass = 1f / m_positiveAttractor.GetGravityFromMass(m_rigidbody.position, 1f).magnitude;
            m_normalizedNegativeMass = 1f / m_negativeAttractor.GetGravityFromMass(m_rigidbody.position, 1f).magnitude;

            m_maxPositiveMass = m_targetSpeed * m_normalizedPositiveMass / m_accelerationTime;
            m_maxNegativeMass = -m_targetSpeed * m_normalizedNegativeMass / m_accelerationTime;
        }

        protected override void OnSetActive(bool active)
        {
            base.OnSetActive(active);

            if (!active)
            {
                m_positiveAttractor.SetMass(0f);
                m_negativeAttractor.SetMass(0f);

                m_positiveAttractor.gameObject.SetActive(false);
                m_negativeAttractor.gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            if (!m_isActive)
                return;

            // how do i differentiate 'intentional movement' and braking from 'unintentional movement' such as falling?

            (Vector2 positiveAcceleration, Vector2 negativeAcceleration) = CalculateWarpMasses(m_desiredMovement * m_targetSpeed, m_rigidbody.velocity);

            m_positiveWarpPivot.up = positiveAcceleration.normalized;
            m_negativeWarpPivot.up = -negativeAcceleration.normalized;

            float positiveMass = positiveAcceleration.magnitude * m_normalizedPositiveMass / m_accelerationTime;
            float negativeMass = -negativeAcceleration.magnitude * m_normalizedNegativeMass / m_accelerationTime;
            
            m_positiveAttractor.gameObject.SetActive(Mathf.Abs(positiveMass) > MIN_MASS_ABSOLUTE_VALUE);
            m_negativeAttractor.gameObject.SetActive(Mathf.Abs(negativeMass) > MIN_MASS_ABSOLUTE_VALUE);
            
            m_positiveAttractor.SetMass(positiveMass);
            m_negativeAttractor.SetMass(negativeMass);

            m_currentNormalizedPositiveMass = Mathf.InverseLerp(0f, m_maxPositiveMass, positiveMass);
            m_currentNormalizedNegativeMass = Mathf.InverseLerp(0f, -m_maxNegativeMass, -negativeMass);

            m_positiveSprite.color = Color.Lerp(m_minPositiveSpriteColour, m_maxPositiveSpriteColour, m_currentNormalizedPositiveMass);
            m_negativeSprite.color = Color.Lerp(m_minNegativeSpriteColour, m_maxNegativeSpriteColour, m_currentNormalizedNegativeMass);
        }

        protected override void OnSpaceDown()
        {
            base.OnSpaceDown();

            SetActive(false);
        }

        protected override void OnSpaceUp()
        {
            base.OnSpaceDown();

            SetActive(true);
        }

        /// <summary>
        /// Returns a tuple of two vectors, the 'positive acceleration' and the 'negative acceleration.'
        /// </summary>
        private (Vector2, Vector2) CalculateWarpMasses(Vector2 newVelocity, Vector2 currentVelocity)
        {
            // this quaternion converts vectors into the current 'velocity space', i.e. a space where the current velocity is always "up"
            Quaternion velocityTransform = Quaternion.FromToRotation(currentVelocity, Vector2.up);
            Quaternion inverseVelocityTransform = Quaternion.Inverse(velocityTransform);
            
            Vector2 transformedCurrentVelocity = velocityTransform * currentVelocity;
            Vector2 transformedNewVelocity = velocityTransform * newVelocity;
            
            Vector2 acceleration = transformedNewVelocity - transformedCurrentVelocity;
            Vector2 positiveAcceleration = new Vector2(Mathf.Max(0f, acceleration.x), Mathf.Max(0f, acceleration.y));
            Vector2 negativeAcceleration = new Vector2(Mathf.Max(0f, -acceleration.x), Mathf.Max(0f, -acceleration.y));

            Vector2 untransformedPositiveAcceleration = inverseVelocityTransform * positiveAcceleration;
            Vector2 untransformedNegativeAcceleration = inverseVelocityTransform * negativeAcceleration;
            
            return (untransformedPositiveAcceleration, untransformedNegativeAcceleration);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying || !enabled)
                return;
            
            Utils.Label((Vector2)m_transform.position + Vector2.up * 1f, m_rigidbody.velocity.magnitude.ToString(), 13, Color.white);
            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.2f, m_negativeAttractor.Mass.ToString(), 13, Color.red);
            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.4f, m_positiveAttractor.Mass.ToString(), 13, Color.blue);

            // draw current velocity as cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_rigidbody.velocity.normalized * 2f);

            // draw desired velocity as magenta
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_desiredMovement.normalized * 2f);
        }
#endif
    }
}