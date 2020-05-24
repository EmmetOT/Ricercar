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
        private NonRigidbodyAttractor m_attractor;

        public void Initialize(Vector2 startPosition, Vector2 startVelocity)
        {
            m_attractor.SetPosition(startPosition);
            m_attractor.AddVelocity(startVelocity);
        }
    }
}