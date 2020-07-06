using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar.Character
{
    public abstract class Gimbal : MonoBehaviour
    {
        protected Vector2 m_currentAim;

        [SerializeField]
        [ReadOnly]
        protected Vector2 m_desiredMovement = Vector2.zero;
        protected Vector2 m_currentGravity;

        protected Transform m_transform;

        [SerializeField]
        protected bool m_isActive;

        protected virtual void OnEnable()
        {
            m_desiredMovement = Vector2.zero;
            m_transform = transform;
        }

        public void SetGravity(Vector2 gravity)
        {
            if (m_currentGravity == gravity)
                return;

            m_currentGravity = gravity;

            OnGravityChanged();
        }

        protected virtual void OnGravityChanged() { }

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