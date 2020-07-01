using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Character
{
    [Serializable]
    public class CharacterInputProcessor
    {
        [Flags]
        public enum MovementAxis
        {
            HORIZONTAL = 1 << 0, 
            VERTICAL = 1 << 1
        }

        private bool HasVerticalMovement => (m_movementAxis & MovementAxis.VERTICAL) != 0;
        private bool HasHorizontalMovement => (m_movementAxis & MovementAxis.HORIZONTAL) != 0;

        [EnumFlags]
        [SerializeField]
        private MovementAxis m_movementAxis;

        [SerializeField]
        private KeyCode m_left;

        [SerializeField]
        private KeyCode m_right;

        [SerializeField]
        private KeyCode m_up;

        [SerializeField]
        private KeyCode m_down;

        [SerializeField]
        private KeyCode m_jump;

        [SerializeField]
        private bool m_restOnLastMoveDirection = false;

        private Vector2 m_moveDirection = Vector2.zero;
        public Vector2 MoveDirection => m_moveDirection;

        private bool m_jumpFlag = false;

        public void ManualUpdate()
        {
            Vector2 moveDirection = Vector2.zero;

            if (HasHorizontalMovement)
            {
                moveDirection += Input.GetKey(m_left) ? Vector2.left : Vector2.zero;
                moveDirection += Input.GetKey(m_right) ? Vector2.right : Vector2.zero;
            }

            if (HasVerticalMovement)
            {
                moveDirection += Input.GetKey(m_up) ? Vector2.up : Vector2.zero;
                moveDirection += Input.GetKey(m_down) ? Vector2.down : Vector2.zero;
            }

            //if (m_restOnLastMoveDirection || !moveDirection.IsZero())
            m_moveDirection = moveDirection;

            m_jumpFlag |= Input.GetKeyDown(m_jump);
        }

        public Vector2 GetAimDirection(Vector3 source, Camera camera)
        {
            return (Utils.GetMousePos2D(camera) - source).normalized;
        }

        public void ManualFixedUpdate()
        {
            if (m_jumpFlag)
            {
                OnJumpInput?.Invoke();
                m_jumpFlag = false;
            }
        }

        public Action OnJumpInput;
    }
}