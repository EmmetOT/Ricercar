using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;
using Ricercar.Character;
using UnityEditor;

namespace Ricercar
{
    public class CameraController : MonoBehaviour
    {
        private enum FollowMode { TRANSFORM, ATTRACTOR, WARP_GIMBAL }

        [SerializeField]
        private FollowMode m_followMode = FollowMode.TRANSFORM;

        [SerializeField]
        [ShowIf("IsFollowingTransform")]
        private Transform m_followTransform;

        [SerializeField]
        [ShowIf("IsFollowingAttractor")]
        private SimpleRigidbodyAttractor m_followAttractor;

        [SerializeField]
        [ShowIf("IsFollowingWarpGimbal")]
        private WarpGimbal m_warpGimbal;

        [SerializeField]
        [MinValue(0f)]
        private float m_followSpeed = 1f;

        [SerializeField]
        private Camera m_camera;

        [SerializeField]
        private float m_sensitivityFloor = 0.01f; // ignore vectors whose magnitude is below this value

        private Transform m_transform;

        [SerializeField]
        private bool m_rotateWithGravity = false;

        private Vector2 m_targetRotation;

        [SerializeField]
        [MinValue(0f)]
        [ShowIf("m_rotateWithGravity")]
        private float m_rotationSpeed = 1f;

        private void Awake()
        {
            m_transform = transform;
        }

        private void LateUpdate()
        {
            if ((IsFollowingTransform() && m_followTransform == null) ||
                (IsFollowingAttractor() && m_followAttractor == null) ||
                (IsFollowingWarpGimbal() && m_warpGimbal == null))
                return;

            Vector3 pos = TargetPos;
            m_transform.position = Vector3.Lerp(m_transform.position, pos, m_followSpeed * Time.deltaTime);

            if (m_rotateWithGravity)
            {
                Vector2 gravityVector = GravityVector;

                //if (gravityVector.magnitude >= m_sensitivityFloor)
                    m_targetRotation = gravityVector.normalized;

                m_camera.transform.rotation = Quaternion.Slerp(m_camera.transform.rotation, Quaternion.LookRotation(Vector3.forward, -m_targetRotation), m_rotationSpeed * Time.deltaTime);
            }
        }

        private bool IsFollowingTransform() => m_followMode == FollowMode.TRANSFORM;
        private bool IsFollowingWarpGimbal() => m_followMode == FollowMode.WARP_GIMBAL;
        private bool IsFollowingAttractor() => m_followMode == FollowMode.ATTRACTOR;

        private Vector2 TargetPos
        {
            get
            {
                switch (m_followMode)
                {
                    case FollowMode.ATTRACTOR:
                        return m_followAttractor == null ? (Vector2)transform.position : m_followAttractor.Position;
                    case FollowMode.TRANSFORM:
                        return m_followTransform == null ? (Vector2)transform.position : (Vector2)m_followTransform.position;
                    case FollowMode.WARP_GIMBAL:
                        return m_warpGimbal == null ? (Vector2)transform.position : (Vector2)m_warpGimbal.transform.position;
                    default:
                        return transform.position;
                };
            }
        }

        private Vector2 GravityVector
        {
            get
            {
                switch (m_followMode)
                {
                    case FollowMode.ATTRACTOR:
                        return m_followAttractor == null ? Physics2D.gravity : m_followAttractor.CurrentGravity;
                    case FollowMode.WARP_GIMBAL:
                        return m_warpGimbal == null ? Physics2D.gravity : m_warpGimbal.GravityWithoutWarpInfluence;
                    default:
                        return Physics2D.gravity;
                };
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (EditorApplication.isPlaying)
                Utils.DrawArrow(TargetPos, m_targetRotation, Color.white, 1f, 1f);
        }
#endif
    }

}