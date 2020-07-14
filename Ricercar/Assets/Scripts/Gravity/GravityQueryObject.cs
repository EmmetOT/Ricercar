using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ricercar.Gravity
{
    /// <summary>
    /// A gravity query object can be created to track gravity at a particular location,
    /// without affecting it. 
    /// </summary>
    public class GravityQueryObject : IAttractor
    {
        private GravityField m_field;

        private int m_layer;
        public int Layer => m_layer;

        private Transform m_transform;

        public Vector2 Position => m_transform == null ? Vector2.zero : (Vector2)m_transform.position;

        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;
        public float Mass => 1f;
        public bool AffectsField => false;

        public GravityQueryObject(GravityField field, int layer, Transform transform = null)
        {
            m_field = field;
            m_layer = layer;
            m_transform = transform;

            m_field.RegisterAttractor(this);
        }

        ~GravityQueryObject()
        {
            m_field.DeregisterAttractor(this);
        }

        public void SetGravity(Vector2 gravity)
        {
            m_currentGravity = gravity;
        }

        public void SetMass(float mass)
        {
            
        }

        public Vector2 GetAttractionFromPosition(Vector2 pos)
        {
            return Vector2.zero;
        }

        public Vector2 GetAttractionFromPosition(Vector2 pos, float otherMass)
        {
            return Vector2.zero;
        }
    }
}