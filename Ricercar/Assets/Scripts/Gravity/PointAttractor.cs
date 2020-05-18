using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class PointAttractor : Attractor
    {
        [Button]
        public void Test()
        {
            Vector2 gravityVector = GetGravityVector(Position, out _);
            Vector2 gravityForce = CalculateGravitationalForce(Position);

            Debug.Log("Vector = " + gravityVector);
            Debug.Log("Vector = " + GetGravityVector(Position + Vector2.right * 0.1f, out _));
            Debug.Log("Force = " + gravityForce);
        }

        protected override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        {
            sourcePos = Rigidbody.position;
            return sourcePos - from;
        }
    }
}