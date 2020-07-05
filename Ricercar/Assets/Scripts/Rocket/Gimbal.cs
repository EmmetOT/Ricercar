﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar.Character
{
    public abstract class Gimbal : MonoBehaviour
    {
        protected Vector2 m_currentAim;
        protected Vector2 m_desiredMovement;
        protected Transform m_transform;

        [SerializeField]
        protected bool m_isActive;

        protected virtual void OnEnable()
        {
            m_transform = transform;
        }

        public void SetAim(Vector2 aim)
        {
            if (m_currentAim == aim)
                return;

            m_currentAim = aim;
            OnAimSet();
        }

        protected virtual void OnAimSet() { }

        public void SetMovement(Vector2 movement)
        {
            if (m_desiredMovement == movement)
                return;

            m_desiredMovement = movement;
            OnMovementSet();
        }

        protected virtual void OnMovementSet() { }

        public void SetSpaceDown()
        {
            OnSpaceDown();
        }

        public void SetSpaceUp()
        {
            OnSpaceUp();
        }

        protected virtual void OnSpaceDown() { }
        protected virtual void OnSpaceUp() { }


        public void SetActive(bool active)
        {
            m_isActive = active;
            OnSetActive(m_isActive);
        }

        protected virtual void OnSetActive(bool active) { }
    }
}