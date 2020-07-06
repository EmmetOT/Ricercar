using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ricercar.InverseKinematics
{
    public class InverseKinematicLeg : MonoBehaviour
    {
        [System.Serializable]
        private struct LegData
        {
            public Vector2 startPosition;
            public float minAngle;
            public float maxAngle;

            public LegData(Vector2 startPosition, float minAngle, float maxAngle)
            {
                this.startPosition = startPosition;
                this.minAngle = minAngle;
                this.maxAngle = maxAngle;
            }
        }

        public float testAngle;

        [SerializeField]
        private LegData[] m_legData;

        [SerializeField]
        [MinValue(0.1f)]
        private float m_samplingDistance = 0.1f;

        [SerializeField]
        [MinValue(0.1f)]
        private float m_learningRate = 0.1f;

        [SerializeField]
        [MinValue(0.1f)]
        private float m_distanceThreshold = 0.1f;

        [SerializeField]
        [MinValue(1)]
        private int m_iterationsPerFrame = 1;

        [SerializeField]
        [ReadOnly]
        private float m_maxTotalLength = 0f;

        private float[] m_angles;

        [SerializeField]
        private Vector2 m_target;

        public bool IsInTargetRange => ((Vector2)transform.position - m_target).sqrMagnitude < m_maxTotalLength * m_maxTotalLength;

        private void Awake()
        {
            m_angles = new float[m_legData.Length];
        }

        private void OnValidate()
        {
            m_maxTotalLength = 0f;

            if (m_angles.IsNullOrEmpty() || (!m_legData.IsNullOrEmpty() && m_legData.Length != m_angles.Length))
                m_angles = new float[m_legData.Length];

            for (int i = 0; i < m_legData.Length; i++)
            {
                m_maxTotalLength += m_legData[i].startPosition.magnitude;
            }
        }

        private void Update()
        {
            for (int i = 0; i < m_iterationsPerFrame; i++)
                RunInverseKinematics();
        }

        public void SetTarget(Vector2 target)
        {
            m_target = target;
        }

        /// <summary>
        /// Returns how far the calculated final value is from the desired position.
        /// The lower this value, the better.
        /// </summary>
        private float DistanceFromTarget()
        {
            return Vector2.Distance(RunForwardKinematics(), m_target);
        }

        private float GetPartialGradientOfAngleAtIndex(int i)
        {
            // Saves the angle,
            // it will be restored later
            float angle = m_angles[i];

            // Gradient : [F(x+SamplingDistance) - F(x)] / h
            float f_x = DistanceFromTarget();

            m_angles[i] += m_samplingDistance;
            float f_x_plus_d = DistanceFromTarget();

            float gradient = (f_x_plus_d - f_x) / m_samplingDistance;

            // restore angle
            m_angles[i] = angle;

            return gradient;
        }

        public void RunInverseKinematics()
        {
            if (DistanceFromTarget() < m_distanceThreshold)
                return;

            for (int i = m_legData.Length - 1; i >= 0; i--)
            {
                // Gradient descent
                // Update : Solution -= LearningRate * Gradient
                float gradient = GetPartialGradientOfAngleAtIndex(i);
                m_angles[i] -= m_learningRate * gradient;

                m_angles[i] = Utils.ClampAngle(m_angles[i], m_legData[i].minAngle, m_legData[i].maxAngle);

                // Early termination
                if (DistanceFromTarget() < m_distanceThreshold)
                    return;
            }
        }

        /// <summary>
        /// Compute the final destination given a series of angles.
        /// </summary>
        public Vector2 RunForwardKinematics(bool debug = false)
        {
            float angle = 0f;
            Vector2 previousPosition = transform.position;

            Gizmos.color = Color.green;

            if (debug)
                Gizmos.DrawSphere(previousPosition, 0.1f);

            for (int i = 0; i < m_legData.Length; i++)
            {
                angle += m_angles[i];
                Vector2 nextPosition = Utils.RotateAround(previousPosition + m_legData[i].startPosition, previousPosition, angle);

                if (debug)
                {
                    Gizmos.DrawLine(previousPosition, nextPosition);
                    Gizmos.DrawSphere(previousPosition, 0.1f);
                }

                previousPosition = nextPosition;
            }

            if (debug)
                Gizmos.DrawSphere(previousPosition, 0.1f);

            return previousPosition;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_target, 0.2f);

            Handles.color = IsInTargetRange ? Color.green : Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.back, m_maxTotalLength);

            Handles.color = Handles.color.SetAlpha(0.1f);
            Handles.DrawSolidDisc(transform.position, Vector3.back, m_maxTotalLength);

            if (m_legData.IsNullOrEmpty())
                return;

            RunForwardKinematics(debug: true);
        }
    }
}