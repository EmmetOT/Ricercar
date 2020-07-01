using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Character
{
    public class RocketController : MonoBehaviour
    {
        [SerializeField]
        private CharacterInputProcessor m_input;

        [SerializeField]
        [BoxGroup("Components")]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        [BoxGroup("Components")]
        private SimpleRigidbodyAttractor m_attractor;

        [SerializeField]
        private Gimbal[] m_gimbals;

        private Transform m_transform;
        private Camera m_camera;

        private Vector2 m_currentAim;
        private Vector2 m_currentMovement;

        private void OnEnable()
        {
            m_transform = transform;
            m_camera = Camera.main;
        }

        private void Update()
        {
            m_input.ManualUpdate();

            m_currentAim = m_input.GetAimDirection(m_transform.position, m_camera);
            m_currentMovement = m_input.MoveDirection;

            for (int i = 0; i < m_gimbals.Length; i++)
            {
                m_gimbals[i].SetAim(m_currentAim);
                m_gimbals[i].SetMovement(m_currentMovement);
            }
        }

        private void FixedUpdate()
        {
            m_input.ManualFixedUpdate();
        }
    }
}