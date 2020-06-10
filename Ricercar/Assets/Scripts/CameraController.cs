using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    public class CameraController : MonoBehaviour
    {
        private enum FollowMode { TRANSFORM, ATTRACTOR }

        [SerializeField]
        private FollowMode m_followMode = FollowMode.TRANSFORM;

        [SerializeField]
        [ShowIf("IsFollowingTransform")]
        private Transform m_followTransform;

        [SerializeField]
        [HideIf("IsFollowingTransform")]
        private SimpleRigidbodyAttractor m_followAttractor;

        [SerializeField]
        [MinValue(0f)]
        private float m_followSpeed = 1f;

        [SerializeField]
        private Camera m_camera;

        private Transform m_transform;

        [SerializeField]
        private bool m_rotateWithGravity = false;

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
            if ((m_followMode == FollowMode.TRANSFORM && m_followTransform == null) || (m_followMode == FollowMode.ATTRACTOR && m_followAttractor == null))
                return;

            Vector3 pos = m_followMode == FollowMode.TRANSFORM ? m_followTransform.position : m_followAttractor.transform.position;

            m_transform.position = Vector3.Lerp(m_transform.position, pos, m_followSpeed * Time.deltaTime);

            if (m_rotateWithGravity)
            {
                Vector2 gravity = m_followMode == FollowMode.TRANSFORM ? Physics2D.gravity : m_followAttractor.CurrentGravity;

                if (gravity.IsZero())
                    gravity = Vector2.down;

                m_camera.transform.rotation = Quaternion.Slerp(m_camera.transform.rotation, Quaternion.LookRotation(Vector3.forward, -gravity.normalized), m_rotationSpeed * Time.deltaTime);
            }
        }

        private bool IsFollowingTransform() => m_followMode == FollowMode.TRANSFORM;
    }

}