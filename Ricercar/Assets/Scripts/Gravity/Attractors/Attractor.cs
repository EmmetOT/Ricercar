using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace Ricercar.Gravity
{
    public abstract class Attractor : MonoBehaviour, IAttractor
    {
        private bool m_canReceiveGravity = false;

        [GravityLayer]
        public int m_layer;
        public int Layer => m_layer;

        [SerializeField]
        [HideInInspector]
        protected Transform m_transform;

        [SerializeField]
        protected GravityField m_gravityField;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField && Mass != 0f;

        public abstract float Mass { get; }

        public abstract Vector2 Position { get; }
        public abstract Vector2 Velocity { get; }

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        protected virtual IEnumerator Start()
        {
            yield return null;
            m_canReceiveGravity = true;
        }

        protected virtual void Reset()
        {
            m_transform = transform;
        }

        protected virtual void OnEnable()
        {
            m_transform = transform;
            m_gravityField.RegisterAttractor(this);
        }

        protected virtual void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        public virtual void SetGravity(Vector2 gravity)
        {
            // for some unclear reason, gravity from the previous play session can
            // "leak in" during the first frame
            if (!m_canReceiveGravity)
                return;

            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;
        }

        public virtual void SetMass(float mass) { }

        public Vector2 GetAttractionFromPosition(Vector2 pos) => GetAttractionFromPosition(pos, Mass);

        public abstract Vector2 GetAttractionFromPosition(Vector2 pos, float mass);
    }
}