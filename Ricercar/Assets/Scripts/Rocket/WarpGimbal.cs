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
        private const float MIN_MASS_ABSOLUTE_VALUE = 0f;//0.001f;

        [SerializeField]
        private SimpleRigidbodyAttractor m_attractor;

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

        // the gravity vectors towards the two attractors if theyre positioned
        // at their resting position with a mass of 1
        private Vector2 m_positiveAttractorGravityVector;
        private Vector2 m_negativeAttractorGravityVector;

        private float m_normalizedPositiveMass;

        private float m_normalizedNegativeMass;

        private float m_maxPositiveMass;

        private float m_maxNegativeMass;

        private float m_currentNormalizedPositiveMass;

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
            m_positiveAttractorGravityVector = m_positiveAttractor.GetGravityFromMass(m_rigidbody.position, 1f);
            m_negativeAttractorGravityVector = m_negativeAttractor.GetGravityFromMass(m_rigidbody.position, 1f);

            // set the positive attractors mass such that it accelerates to the target speed in the target amount of time
            m_normalizedPositiveMass = 1f / m_positiveAttractorGravityVector.magnitude;
            m_normalizedNegativeMass = 1f / m_negativeAttractorGravityVector.magnitude;

            m_maxPositiveMass = m_targetSpeed * m_normalizedPositiveMass / m_accelerationTime;
            m_maxNegativeMass = -m_targetSpeed * m_normalizedNegativeMass / m_accelerationTime;

            if (!m_rigidbody.velocity.IsZero())
            {
                Debug.Log("Velocity is not zero at start!");
                Debug.Break();
            }
        }

        protected override void OnSetActive(bool active)
        {
            base.OnSetActive(active);

            m_positiveAttractor.SetMass(0f);
            m_negativeAttractor.SetMass(0f);

            if (!m_isActive)
            {
                m_positiveAttractor.gameObject.SetActive(false);
                m_negativeAttractor.gameObject.SetActive(false);
            }
        }

        public Vector2 GetCurrentWarpInfluence()
        {
            Vector2 positiveInfluence = m_positiveAttractor.Mass * Utils.RotateAround(m_positiveAttractorGravityVector, Vector2.zero, m_positiveWarpPivot.eulerAngles.z);
            Vector2 negativeInfluence = m_negativeAttractor.Mass * Utils.RotateAround(m_negativeAttractorGravityVector, Vector2.zero, m_negativeWarpPivot.eulerAngles.z);

            return positiveInfluence + negativeInfluence;
        }

        private Vector2 m_gravityWithoutWarpInfluence;

        /// <summary>
        /// Because the warp gimbal affects gravity, this vector returns the gravity if the warp gimbal wasn't there.
        /// </summary>
        public Vector2 GravityWithoutWarpInfluence => m_gravityWithoutWarpInfluence;

        private void FixedUpdate()
        {
            if (!m_isActive)
                return;

            m_gravityWithoutWarpInfluence = m_accelerationTime * (m_attractor.CurrentGravity - GetCurrentWarpInfluence()) / m_attractor.Mass;

            Quaternion rotation = Quaternion.FromToRotation(Vector2.up, -m_gravityWithoutWarpInfluence.normalized);

            Vector2 desiredVelocity = rotation * (m_desiredMovement * m_targetSpeed);

            // what is this 0.2 value??? why does it work???
            Vector2 currentVelocity = m_rigidbody.velocity + m_gravityWithoutWarpInfluence;

            m_positiveWarpPivot.up = desiredVelocity.normalized;
            m_negativeWarpPivot.up = currentVelocity.normalized;

            float positiveMass = desiredVelocity.magnitude * m_normalizedPositiveMass / m_accelerationTime;
            float negativeMass = -currentVelocity.magnitude * m_normalizedNegativeMass / m_accelerationTime;

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

        ///// <summary>
        ///// Returns a tuple of two vectors, the 'positive acceleration' and the 'negative acceleration.'
        ///
        /// todo: come back to this and do some dot product stuff instead of the naive positive/negative split i did before
        /// <summary>
        /// 
        /// </summary>
        ///// </summary>
        //private (Vector2, Vector2) CalculateWarpMasses(Vector2 newVelocity, Vector2 currentVelocity)
        //{
        //    // this quaternion converts local velocity vectors into the current 'velocity space', i.e. a space where the current velocity is always "up"
        //    Quaternion velocityTransform = Quaternion.FromToRotation(currentVelocity, Vector2.up);
        //    Quaternion inverseVelocityTransform = Quaternion.Inverse(velocityTransform);

        //    Vector2 transformedCurrentVelocity = velocityTransform * currentVelocity;
        //    Vector2 transformedNewVelocity = velocityTransform * newVelocity;

        //    // desired change in speed
        //    Vector2 acceleration = transformedNewVelocity - transformedCurrentVelocity;

        //    // split the acceleration into positive (the vector we want to speed towards) and negative (the vector by which to reduce speed)
        //    //Vector2 positiveAcceleration = new Vector2(Mathf.Max(0f, acceleration.x), Mathf.Max(0f, acceleration.y));
        //    //Vector2 negativeAcceleration = new Vector2(Mathf.Max(0f, -acceleration.x), Mathf.Max(0f, -acceleration.y));

        //    //// transform the result back into local space
        //    //Vector2 untransformedPositiveAcceleration = inverseVelocityTransform * positiveAcceleration;
        //    //Vector2 untransformedNegativeAcceleration = inverseVelocityTransform * negativeAcceleration;

        //    acceleration = inverseVelocityTransform * acceleration;

        //    return (newVelocity, currentVelocity);
        //}

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!EditorApplication.isPlaying || !enabled || !m_isActive)
                return;

            //Gizmos.matrix = Matrix4x4.identity;

            Utils.Label((Vector2)m_transform.position + Vector2.up * 1f, m_rigidbody.velocity.magnitude.ToString(), 13, Color.white);
            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.3f, m_negativeAttractor.Mass.ToString(), 13, Color.red);
            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.6f, m_positiveAttractor.Mass.ToString(), 13, Color.blue);

            // draw current velocity as cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)GravityWithoutWarpInfluence.normalized * 2f);

            //// draw desired velocity as magenta
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_desiredMovement.normalized * 2f);

            //// draw current velocity as cyan
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_negativeAcceleration.normalized * 2f);

            //// draw desired velocity as magenta
            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_positiveAcceleration.normalized * 2f);
        }
#endif
    }
}