using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.UIElements;
using System;

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
        private float m_joint1RestAngle;

        [SerializeField]
        private float m_joint2RestAngle;

        private float m_joint1TargetAngle;
        private float m_joint2TargetAngle;

        public float CurrentJoint1Angle => Mathf.LerpAngle(m_joint1RestAngle, m_joint1TargetAngle, m_restTargetInterpolate);
        public float CurrentJoint2Angle => Mathf.LerpAngle(m_joint2RestAngle, m_joint2TargetAngle, m_restTargetInterpolate);
        public float TargetJoint1Angle => m_joint1TargetAngle;
        public float TargetJoint2Angle => m_joint2TargetAngle;

        public float this[int i]
        {
            get
            {
                if (i == 0)
                    return TargetJoint1Angle;
                else if (i == 1)
                    return TargetJoint2Angle;
                
                throw new IndexOutOfRangeException();
            }
        }

        [SerializeField]
        [Range(0f, 1f)]
        private float m_restTargetInterpolate = 0f;

        [SerializeField]
        private RotationConstraint m_rotationConstraint;



        private enum RotationConstraint
        {
            ANTICLOCKWISE,
            CLOCKWISE
        }

        public float MaxLength => m_joint1Length + m_joint2Length;

        private Vector2 m_target;

        public void SetTarget(Vector2 target)
        {
            m_target = target;
        }

        public void SetRestTargetLerp(float t)
        {
            m_restTargetInterpolate = Mathf.Clamp01(t);
        }

        [Button]
        private void FlipAngles()
        {
            m_joint1TargetAngle = -(m_joint1TargetAngle - 180f);
            m_joint2TargetAngle *= -1f;
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
                m_joint1TargetAngle = atan;
                m_joint2TargetAngle = 0f;
            }
            else
            {
                float cosAngle0 = ((distanceToTarget * distanceToTarget) + (m_joint1Length * m_joint1Length) - (m_joint2Length * m_joint2Length)) / (2 * distanceToTarget * m_joint1Length);
                float angle0 = Mathf.Acos(cosAngle0) * Mathf.Rad2Deg;

                float cosAngle1 = ((m_joint2Length * m_joint2Length) + (m_joint1Length * m_joint1Length) - (distanceToTarget * distanceToTarget)) / (2 * m_joint2Length * m_joint1Length);
                float angle1 = Mathf.Acos(cosAngle1) * Mathf.Rad2Deg;

                // So they work in Unity reference frame
                m_joint1TargetAngle = atan - angle0;
                m_joint2TargetAngle = 180f - angle1;
            }

            if (m_rotationConstraint == RotationConstraint.CLOCKWISE)
                FlipAngles();
        }

        /// <summary>
        /// Compute the final destination given a series of angles.
        /// </summary>
        public Vector2 RunForwardKinematics(float angle1, float angle2, bool debug = false)
        {
            float angleOffset = m_rotationConstraint == RotationConstraint.ANTICLOCKWISE ? -transform.eulerAngles.z : transform.eulerAngles.z;
            float angle = 0f;
            Vector2 previousPosition = transform.position;

            if (debug)
                Gizmos.DrawSphere(previousPosition, 0.1f);

            void CalculateJoint(float angleIncrement, float jointLength)
            {
                angle += angleIncrement;
                Vector2 nextPosition = Utils.RotateAround(previousPosition + (Vector2)transform.right * jointLength, previousPosition, angle + angleOffset);

                if (debug)
                {
                    Gizmos.DrawLine(previousPosition, nextPosition);
                    Gizmos.DrawSphere(previousPosition, 0.1f);
                }

                previousPosition = nextPosition;
            }

            CalculateJoint(angle1, m_joint1Length);
            CalculateJoint(angle2, m_joint2Length);

            if (debug)
                Gizmos.DrawSphere(previousPosition, 0.1f);

            return previousPosition;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_target, 0.2f);

            Handles.color = (MaxLength >= Vector2.Distance(transform.position, m_target)) ? Color.green : Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.back, MaxLength);

            Handles.color = Handles.color.SetAlpha(0.1f);
            Handles.DrawSolidDisc(transform.position, Vector3.back, MaxLength);

            Gizmos.color = Color.red;
            RunForwardKinematics(CurrentJoint1Angle, CurrentJoint2Angle, debug: true);

            if (EditorApplication.isPlaying)
            {
                Gizmos.color = Color.green;
                RunForwardKinematics(m_joint1TargetAngle, m_joint2TargetAngle, debug: true);
            }
        }
#endif
    }
}