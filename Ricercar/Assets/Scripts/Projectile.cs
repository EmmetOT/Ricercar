using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ricercar.Gravity;
using NaughtyAttributes;

namespace Ricercar
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField]
        private Attractor m_attractor;

        [SerializeField]
        private Rigidbody2D m_rigidbody;

        public void Initialize(Vector2 startPosition, Vector2 startVelocity)
        {
            m_rigidbody.position = startPosition;
            //m_rigidbody.rotation = startPosition;
            m_rigidbody.velocity = startVelocity;
        }
    }
}