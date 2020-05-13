using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    public class Gun : MonoBehaviour
    {

        [SerializeField]
        protected SpriteRenderer m_aimSpriteRenderer;

        private Attractor m_sourceAttractor;
        private Transform m_transform;
        private float m_distanceFromCentre;

        public void Initialize(Attractor attractor, float distanceFromCentre)
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
            
        }

        public void ManualUpdate(float deltaTime)
        {

        }

        public void Fire()
        {

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
    }
}