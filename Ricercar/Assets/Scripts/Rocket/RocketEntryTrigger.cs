using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Character
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class RocketEntryTrigger : MonoBehaviour
    {
        [SerializeField]
        private RocketController m_rocketController;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out CharacterController characterController))
            {
                m_rocketController.SetCanEnter(true, characterController);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out CharacterController _))
            {
                m_rocketController.SetCanEnter(false);
            }
        }
    }
}