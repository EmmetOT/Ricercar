using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Gravity
{
    // todo associate these structs
    // todo remove the line attractor stuff

    public struct AttractorData
    {
        // 7 * 4
        public const int Stride = 28;

        public Vector2 position;
        public int ignore;
        public float mass;
        public float radius;
        public float surfaceGravityForce;
        public int layer;

        //public int isLine;
        //public float lineStartX;
        //public float lineStartY;
        //public float lineEndX;
        //public float lineEndY;

        public AttractorData(IAttractor attractor)
        {
            position = attractor.Position;
            ignore = attractor.AffectsField ? 0 : 1;
            mass = attractor.Mass;
            layer = attractor.Layer;

            if (!(attractor is ISimpleAttractor simple))
            {
                radius = 0f;
                surfaceGravityForce = 1f;
            }
            else
            {
                radius = simple.Radius;
                surfaceGravityForce = simple.SurfaceGravityForce;
            }

            //if (!(attractor is ILineAttractor line))
            //{
                //isLine = 0;
                //lineStartX = 0f;
                //lineStartY = 0f;
                //lineEndX = 1f;
                //lineEndY = 1f;
            //}
            //else
            //{
            //    isLine = 1;
            //    lineStartX = line.Start.x;
            //    lineStartY = line.Start.y;
            //    lineEndX = line.End.x;
            //    lineEndY = line.End.y;
            //}
        }

        //public override string ToString()
        //{
        //    return $"x:\t{x}\ny:\t{y}\nignore:\t{ignore}\nmass:\t{mass}\nradius:\t{radius}\nsurfaceGravityForce:\t{surfaceGravityForce}\nisLine:\t{isLine}\nlineStartX:\t{lineStartX}\nlineStartY:\t{lineStartY}\nlineEndX:\t{lineEndX}\nlineEndY:\t{lineEndY}";
        //}
    }

    /// <summary>
    /// Information about a baked attractor to be sent to the GPU.
    /// </summary>
    public struct BakedAttractorData
    {
        // 11 * 4
        public const int Stride = 44;

        public Vector2 position;
        public int ignore;
        public float mass;
        public Vector2 centreOfGravity;
        public float rotation;
        public float size;
        public float scale;
        public int textureIndex;
        public int layer;

        public BakedAttractorData(BakedAttractor attractor)
        {
            position = attractor.Position;
            ignore = attractor.AffectsField ? 0 : 1;
            mass = attractor.Mass;
            centreOfGravity = attractor.ExtrapolationSource == ExtrapolationSource.CENTRE_OF_GRAVITY ? attractor.CentreOfGravity : attractor.Position;
            rotation = attractor.Rotation;
            size = attractor.Size;
            scale = attractor.Scale;
            textureIndex = -1;
            layer = attractor.Layer;
        }

        public void SetTextureIndex(int index)
        {
            textureIndex = index;
        }
    }
}