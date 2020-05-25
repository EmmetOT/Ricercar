using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ricercar.Gravity;

namespace Ricercar
{
    public class Rocket : MonoBehaviour
    {
        private const float JET_SPRITE_OFFSET = 0.05f;

        [SerializeField]
        protected SpriteRenderer m_aimSpriteRenderer;

        [SerializeField]
        private SpriteRenderer m_jetSpriteRenderer;

        [SerializeField]
        [MinValue(0f)]
        private float m_defaultForce;

        [SerializeField]
        [MinValue(0f)]
        private float m_minForceJetSpriteScale = 0.1f;

        [SerializeField]
        [MinValue(0f)]
        private float m_maxForceJetSpriteScale = 1f;

        [SerializeField]
        [MinValue(0f)]
        private float m_minForce = 30f;

        [SerializeField]
        [MinValue(0f)]
        private float m_maxForce = 800f;

        private IAttractor m_sourceAttractor;
        private Transform m_transform;
        private float m_distanceFromCentre;

        private bool m_showJetFlag = false;

        public Vector3 GetSourcePosition()
        {
            return m_transform.position + m_transform.up * m_distanceFromCentre;
        }

        public void Initialize(IAttractor attractor, float distanceFromCentre)
        {
            Reset();

            m_transform = transform;
            m_sourceAttractor = attractor;
            m_distanceFromCentre = distanceFromCentre;

            gameObject.SetActive(true);

            SetRotation(0f);
        }

        public void Reset()
        {
            m_jetSpriteRenderer.enabled = false;
            m_showJetFlag = false;
        }

        public void SetSpriteColour(Color col)
        {
            m_aimSpriteRenderer.color = col;
        }

        public void SetRotation(float angle)
        {
            if (m_transform == null)
                m_transform = transform;

            Vector3 eulerAngles = Vector3.back * angle;

            m_transform.localPosition = Utils.RotateAround(Vector3.up * m_distanceFromCentre, Vector3.zero, eulerAngles);
            m_transform.localEulerAngles = eulerAngles;
        }

        public void ManualUpdate(float deltaTime)
        {
            if (m_showJetFlag)
                m_showJetFlag = false;
            else if (m_jetSpriteRenderer.enabled)
                m_jetSpriteRenderer.enabled = false;
        }

        public void Fire()
        {
            Fire(m_defaultForce);
        }

        public void Fire(float force)
        {
            //m_sourceAttractor.Rigidbody.AddForceAtPosition(m_transform.up * -force, GetSourcePosition(), ForceMode2D.Force);

            m_jetSpriteRenderer.enabled = true;
            m_showJetFlag = true;

            SetSpriteScale(Mathf.Lerp(m_minForceJetSpriteScale, m_maxForceJetSpriteScale, Mathf.InverseLerp(m_minForce, m_maxForce, force)));
        }

        private void SetSpriteScale(float scale)
        {
            m_jetSpriteRenderer.transform.localScale = Vector3.one * scale;
            m_jetSpriteRenderer.transform.localPosition = m_jetSpriteRenderer.transform.localPosition.SetY(scale + JET_SPRITE_OFFSET);
        }
    }
}