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
        private Projectile m_projectilePrefab;

        private static Pool<Projectile> m_projectilePool;

        private readonly List<Projectile> m_projectiles = new List<Projectile>();
        
        [SerializeField]
        protected SpriteRenderer m_aimSpriteRenderer;

        [SerializeField]
        [MinValue(0f)]
        private float m_defaultForce;

        private IAttractor m_sourceAttractor;
        private Transform m_transform;
        private float m_distanceFromCentre;

        public void Initialize(IAttractor attractor, float distanceFromCentre)
        {
            if (m_projectilePool == null)
                m_projectilePool = new Pool<Projectile>(m_projectilePrefab);

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

        public void DespawnProjectiles()
        {
            m_projectilePool.ReturnAll();
            m_projectiles.Clear();

        }

        public Vector2 Fire()
        {
            return Fire(m_defaultForce);
        }

        public Vector2 Fire(float force)
        {
            Vector3 resultForce = m_transform.up * force;

            Projectile projectile = m_projectilePool.GetNew();
            projectile.Initialize(m_transform.position + m_transform.up * 0.3f, resultForce);
            m_projectiles.Add(projectile);

            return resultForce;
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