using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Character
{
    [System.Serializable]
    public class CharacterInputProcessor
    {
        [SerializeField]
        private KeyCode m_left;

        [SerializeField]
        private KeyCode m_right;

        [SerializeField]
        private KeyCode m_jump;

        private Vector2 m_moveDirection = Vector2.zero;
        public Vector2 MoveDirection => m_moveDirection;

        private bool m_jumpFlag = false;

        public void ManualUpdate()
        {
            m_moveDirection = Vector2.zero;
            m_moveDirection += Input.GetKey(m_left) ? Vector2.left : Vector2.zero;
            m_moveDirection += Input.GetKey(m_right) ? Vector2.right : Vector2.zero;

            m_jumpFlag |= Input.GetKeyDown(m_jump);
        }

        public void ManualFixedUpdate()
        {
            //if (!m_moveDirection.IsZero())
            //{
            //    OnMoveInput?.Invoke(m_moveDirection);
            //    m_moveDirection = Vector2.zero;
            //}

            if (m_jumpFlag)
            {
                OnJumpInput?.Invoke();
                m_jumpFlag = false;
            }
        }

        public System.Action OnJumpInput;
        public System.Action<Vector2> OnMoveInput;
    }
}