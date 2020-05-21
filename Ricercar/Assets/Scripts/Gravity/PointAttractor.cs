using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class PointAttractor : Attractor
    {
        private void OnEnable()
        {
            m_gravityField.RegisterAttractor(this);
        }

        private void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        //protected override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        //{
        //    sourcePos = Rigidbody.position;
        //    return sourcePos - from;
        //}
    }
}