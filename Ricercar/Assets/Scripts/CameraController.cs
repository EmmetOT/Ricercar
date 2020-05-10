using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private Transform m_followTransform;

        [SerializeField]
        private float m_followSpeed = 1f;

        [SerializeField]
        private Camera m_camera;

        private Transform m_transform;

        [SerializeField]
        private bool m_rotateWithGravity = false;

        private void Awake()
        {
            m_transform = transform;
        }

        private void LateUpdate()
        {
            if (m_followTransform == null)
                return;

            m_transform.position = Vector3.Lerp(m_transform.position, m_followTransform.position, m_followSpeed * Time.deltaTime);

            if (m_rotateWithGravity)
            {
                Vector2 gravity = Attractor.GetGravityAtPoint(m_transform.position);
                m_camera.transform.rotation = Quaternion.LookRotation(Vector3.forward, -gravity.normalized);
            }
        }
    }

}