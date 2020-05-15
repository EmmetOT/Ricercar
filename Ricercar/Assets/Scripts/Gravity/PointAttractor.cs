using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class PointAttractor : Attractor
    {
        protected override Vector2 GetGravityVector(Vector2 from, out Vector2 sourcePos)
        {
            sourcePos = Rigidbody.position;
            return sourcePos - from;
        }
    }
}