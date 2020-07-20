using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.InverseKinematics;
using UnityEditor;
using System;

namespace Ricercar.Character
{
    public class LanderGimbal : Gimbal
    {
        [SerializeField]
        private bool m_tryAnimateLegs = true;

        [SerializeField]
        private LayerMask m_groundLayer;

        [SerializeField]
        private InverseKinematicLegSystem m_legSystem;

        [SerializeField]
        private Transform[] m_legOne;

        [SerializeField]
        private Transform[] m_legTwo;

        [SerializeField]
        [MinValue(0f)]
        private float m_startDistanceFromGround = 4f;

        [SerializeField]
        [MinValue(0f)]
        private float m_targetDistanceFromGround = 2f;

        [SerializeField]
        [BoxGroup("Leg Animation")]
        private AnimationCurve m_joint1OpeningCurve;

        [SerializeField]
        [BoxGroup("Leg Animation")]
        private AnimationCurve m_joint2OpeningCurve;

        [SerializeField]
        [BoxGroup("Leg Animation")]
        [Range(0f, 1f)]
        [OnValueChanged("SetLegAnimationVal")]
        private float m_legAnimationT = 0f;


        private void LateUpdate()
        {
            m_transform.up = -m_controller.CurrentGravityWithoutWarp.normalized;
            m_legSystem.TryLand();

            if (!m_tryAnimateLegs)
                return;

            float animPoint = GetPointInLandingSequence();


            m_legAnimationT = Mathf.Clamp01(animPoint);
            SetLegAnimationVal();
        }

        private void SetLegAnimationVal()
        {
            m_legSystem.LerpFromRestAngle1(m_joint1OpeningCurve.Evaluate(m_legAnimationT));
            m_legSystem.LerpFromRestAngle2(m_joint2OpeningCurve.Evaluate(m_legAnimationT));
        }

        private float GetPointInLandingSequence()
        {
            float RaycastFrom(Vector3 source)
            {
                RaycastHit2D hit = Physics2D.Raycast(source, -m_transform.up, m_startDistanceFromGround, m_groundLayer);

                return hit.collider == null ? -1f : hit.distance;
            }

            float legOneDist = RaycastFrom(m_legSystem.LegOne.transform.position);
            float legTwoDist = RaycastFrom(m_legSystem.LegTwo.transform.position);

            if (legOneDist < 0f || legTwoDist < 0f)
                return -1f;

            float avg = (legOneDist + legTwoDist) * 0.5f;

            return Mathf.InverseLerp(m_startDistanceFromGround, m_targetDistanceFromGround, avg);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!EditorApplication.isPlaying)
                return;

            m_legSystem.DrawGizmos();

            float animPoint = GetPointInLandingSequence();
            Handles.color = (animPoint > 0f) ? Color.green : Color.red;

            if (animPoint > 0f)
                Utils.Label((Vector2)m_transform.position + Vector2.up * 1f, animPoint.ToString(), 13, Color.white);

            //Vector3 legOnePos = m_legSystem.LegOne.transform.position;
            //Vector3 legTwoPos = m_legSystem.LegTwo.transform.position;

            //Handles.DrawAAPolyLine(width: 3f, legOnePos, legTwoPos);

            //Vector3 midpoint = (legOnePos + legTwoPos) * 0.5f;
            //Handles.DrawAAPolyLine(width: 3f, midpoint, midpoint - m_transform.up * m_startDistanceFromGround);

            //Handles.color = Color.white;
        }
#endif
    }
}