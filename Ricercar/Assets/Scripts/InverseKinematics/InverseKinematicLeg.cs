using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.InverseKinematics
{
    /// <summary>
    /// A simple 2-joint inverse kinematic leg. Will try to get the joints of the leg rotated
    /// so that the end of the limb touches the target.
    /// </summary>
    public class InverseKinematicLeg : MonoBehaviour
    {
        [SerializeField]
        private float m_joint1Length;

        [SerializeField]
        private float m_joint2Length;

        [SerializeField]
        private RotationConstraint m_rotationConstraint;

        private enum RotationConstraint
        {
            ANTICLOCKWISE,
            CLOCKWISE
        }

        public float MaxLength => m_joint1Length + m_joint2Length;

        [SerializeField]
        private float m_joint1Angle;
        [SerializeField]
        private float m_joint2Angle;


        private Vector2 m_target;

        public void SetTarget(Vector2 target)
        {
            m_target = target;
        }

        private void FlipAngles()
        {
            m_joint2Angle = -m_joint2Angle;
            m_joint1Angle -= m_joint2Angle;
        }

        /// <summary>
        /// An analytic solution exists for the case where there are only two joints. This function takes
        /// the positions of the two joints, the effector position, and the target, and returns a tuple
        /// containing the two angles by which the joints must be rotated for the effector to match the target.
        /// </summary>
        [Button("Run Inverse Kinematics")]
        public void RunInverseKinematics()
        {
            // displacement from joint one to target
            Vector2 diff = m_target - (Vector2)transform.position;

            // the atan input here is flipped from the tutorial. it's wrong, but it works!
            float atan = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            float distanceToTarget = diff.magnitude;

            // Is the target reachable?
            // If not, we stretch as far as possible
            if (distanceToTarget > MaxLength)
            {
                m_joint1Angle = atan;
                m_joint2Angle = 0f;
            }
            else
            {
                float cosAngle0 = ((distanceToTarget * distanceToTarget) + (m_joint1Length * m_joint1Length) - (m_joint2Length * m_joint2Length)) / (2 * distanceToTarget * m_joint1Length);
                float angle0 = Mathf.Acos(cosAngle0) * Mathf.Rad2Deg;

                float cosAngle1 = ((m_joint2Length * m_joint2Length) + (m_joint1Length * m_joint1Length) - (distanceToTarget * distanceToTarget)) / (2 * m_joint2Length * m_joint1Length);
                float angle1 = Mathf.Acos(cosAngle1) * Mathf.Rad2Deg;

                // So they work in Unity reference frame
                m_joint1Angle = atan - angle0;
                m_joint2Angle = 180f - angle1;
            }

            if (m_rotationConstraint == RotationConstraint.CLOCKWISE)
                FlipAngles();
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

            void CalculateJoint(float angleIncrement, float jointLength)
            {
                angle += angleIncrement;
                Vector2 nextPosition = Utils.RotateAround(previousPosition + Vector2.right * jointLength, previousPosition, angle);

                if (debug)
                {
                    Gizmos.DrawLine(previousPosition, nextPosition);
                    Gizmos.DrawSphere(previousPosition, 0.1f);
                }

                previousPosition = nextPosition;
            }

            CalculateJoint(m_joint1Angle, m_joint1Length);
            CalculateJoint(m_joint2Angle, m_joint2Length);

            if (debug)
                Gizmos.DrawSphere(previousPosition, 0.1f);

            return previousPosition;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_target, 0.2f);

            Handles.color = (MaxLength >= Vector2.Distance((Vector2)transform.position, m_target)) ? Color.green : Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.back, MaxLength);

            Handles.color = Handles.color.SetAlpha(0.1f);
            Handles.DrawSolidDisc(transform.position, Vector3.back, MaxLength);

            RunForwardKinematics(debug: true);
        }
#endif
    }
}