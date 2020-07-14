using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Gravity
{
    public interface IAttractor
    {
        int Layer { get; }
        Vector2 Position { get; }
        Vector2 Velocity { get; }
        Vector2 CurrentGravity { get; }
        float Mass { get; }
        bool AffectsField { get; }

        void SetGravity(Vector2 gravity);
        void SetMass(float mass);


        Vector2 GetAttractionFromPosition(Vector2 pos);
        Vector2 GetAttractionFromPosition(Vector2 pos, float otherMass);

    }

    // todo: remove these?

    public interface ISimpleAttractor : IAttractor
    {
        float Radius { get; }
        float SurfaceGravityForce { get; }
    }

    //public interface ILineAttractor : IAttractor
    //{
    //    Vector2 Start { get; }
    //    Vector2 End { get; }
    //}

}