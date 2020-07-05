using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Character
{
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        public const string MOVEMENT_PARAM = "Run";

        [SerializeField]
        private Animator m_animator;

        [SerializeField]
        private CharacterController m_controller;
        
        private int m_currentMovementParam = 0;

        private void Update()
        {
            int movementParam = 0;

            if (m_controller.IsMovingLeft)
                movementParam = -1;
            else if (m_controller.IsMovingRight)
                movementParam = 1;
            else
                movementParam = 0;

            if (m_currentMovementParam != movementParam)
            {
                m_currentMovementParam = movementParam;
                UpdateAnimatorMovementParam();
            }
        }

        private void UpdateAnimatorMovementParam()
        {
            m_animator.SetInteger(MOVEMENT_PARAM, m_currentMovementParam);
        }
    }

}