using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Obi;
using Ricercar.Gravity;

namespace Ricercar
{
    public abstract class Wheel : MonoBehaviour
    {
        [System.Serializable]
        public struct Data
        {
            [SerializeField]
            private Type m_type;
            public Type Type => m_type;

            [SerializeField]
            private int m_componentCount;
            public int ComponentCount => Mathf.Max(1, m_componentCount);

            [SerializeField]
            private float m_componentProximity;
            public float ComponentProximity => Mathf.Clamp(m_componentProximity, 0f, 360f);

            [SerializeField]
            private Control m_control;
            public Control Control => m_control;

            [SerializeField]
            private bool m_alwaysActive;
            public bool AlwaysActive => m_alwaysActive;
        }

        public enum Type
        {
            ROPE,
            ROCKET,
            GUN
        }

        public enum Control
        { 
            MOUSE,
            KEYBOARD
        }

        public abstract Type WheelType { get; }

        [SerializeField]
        protected Sprite[] m_wheelSprites;

        //[SerializeField]
        //protected Sprite m_aimSprite;

        [SerializeField]
        protected SpriteRenderer m_wheelSpriteRenderer;

        protected Transform m_transform;
        protected Color m_selectedColour;
        protected Color m_unselectedColour;

        [SerializeField]
        [ReadOnly]
        protected int m_index;

        [SerializeField]
        [ReadOnly]
        protected int m_componentCount;

        [SerializeField]
        [ReadOnly]
        protected float m_componentProximity;

        [SerializeField]
        protected LayerMask m_environmentLayerMask;

        protected ObiSolver m_solver;

        protected Material m_material;

        protected IAttractor m_attractor;

        protected ObiCollider2D m_parentCollider;

        protected ContactFilter2D m_raycastFilter;
        protected readonly RaycastHit2D[] m_raycastHits = new RaycastHit2D[1];
        protected int m_currentHitCount = 0;
        
        protected bool m_isSelected = false;

        protected float SourceDistance => 0.555f + 0.08f * m_index;

        public float CurrentAim { get; private set; }

        protected virtual bool CanAim => true;

        private bool m_isPrimaryFireHeld = false;
        public bool IsPrimaryFireHeld => m_isPrimaryFireHeld;

        private bool m_isSecondaryFireHeld = false;
        public bool IsSecondaryFireHeld => m_isSecondaryFireHeld;


        public virtual void PrimaryFire() { }
        public virtual void SecondaryFire() { }
        public virtual void HoldPrimaryFire() { m_isPrimaryFireHeld = true; }
        public virtual void HoldSecondaryFire() { m_isSecondaryFireHeld = true; }
        public virtual void ReleaseHoldPrimaryFire() { m_isPrimaryFireHeld = false; }
        public virtual void ReleaseHoldSecondaryFire() { m_isSecondaryFireHeld = false; }

        public virtual void OnScroll(float delta) { }

        public virtual void Initialize(int componentCount, float componentProximity, Color selectedColour, Color unselectedColour, int index, ObiSolver solver, Material material, IAttractor attractor, ObiCollider2D parentCollider)
        {
            CurrentAim = 0f;

            m_index = index;
            m_componentCount = componentCount;
            m_componentProximity = componentProximity;

            m_transform = transform;

            m_solver = solver;

            m_material = material;

            m_selectedColour = selectedColour;
            m_unselectedColour = unselectedColour;

            m_wheelSpriteRenderer.sprite = m_wheelSprites[index];
            m_attractor = attractor;
            m_parentCollider = parentCollider;
            
            m_raycastFilter = new ContactFilter2D
            {
                useTriggers = false
            };

            m_raycastFilter.SetLayerMask(m_environmentLayerMask);

            SetSelected(false);
            
            OnSetAim(0f);
        }

        public virtual void SetSelected(bool selected)
        {
            m_isSelected = selected;

            SetColour(m_isSelected ? m_selectedColour : m_unselectedColour);
        }

        public virtual void SetColour(Color col)
        {
            m_material.color = col;
        }

        public virtual void ManualUpdate(float deltaTime)
        {
        }

        protected virtual void OnDestroy()
        {

        }

        public void SetAim(float angle)
        {
            if (!CanAim)
                return;

            CurrentAim = angle;

            OnSetAim(CurrentAim);
        }

        protected virtual void OnSetAim(float angle)
        {

        }
    }
}