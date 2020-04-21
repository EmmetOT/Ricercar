using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar
{
    public class RocketWheel : Wheel
    {
        public override Type WheelType => Type.ROCKET;
        
        [SerializeField]
        private Rocket m_rocketPrefab;

        private static Pool<Rocket> m_rocketPool;

        private readonly List<Rocket> m_rockets = new List<Rocket>();

        [SerializeField]
        [MinValue(1)]
        private int m_rocketCount = 1;
        
        [SerializeField]
        [MinValue(1f)]
        private float m_stabilisationDamping = 100f;

        protected override bool CanAim => base.CanAim && !IsSecondaryFireHeld;

        public override void Initialize(int componentCount, float componentProximity, Color selectedColour, Color unselectedColour, int index, ObiSolver solver, Material material, Rigidbody2D rigidbody, ObiCollider2D parentCollider)
        {
            base.Initialize(componentCount, componentProximity, selectedColour, unselectedColour, index, solver, material, rigidbody, parentCollider);
            
            if (m_rocketPool == null)
                m_rocketPool = new Pool<Rocket>(m_rocketPrefab);

            for (int i = 0; i < m_componentCount; i++)
            {
                Rocket rocket = m_rocketPool.GetNew();
                rocket.transform.SetParent(m_transform);
                rocket.transform.Reset();
                rocket.Initialize(m_rigidbody, SourceDistance);

                m_rockets.Add(rocket);
            }
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);

            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].ManualUpdate(deltaTime);
        }

        public override void HoldPrimaryFire()
        {
            base.HoldPrimaryFire();

            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].Fire();
        }

        public override void HoldSecondaryFire()
        {
            base.HoldSecondaryFire();

            FireStabilisers();
        }

        private void FireStabilisers()
        {
            Debug.Log("Stabilising...");

            Vector2 antiGravForce = m_rigidbody.mass * Physics2D.gravity;
            Vector2 dampingForce = m_rigidbody.velocity * m_stabilisationDamping;
            Vector2 resultForce = antiGravForce + dampingForce;

            float angle = -Vector2.SignedAngle(Vector2.up, resultForce.normalized);
            float magnitude = resultForce.magnitude;

            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].SetRotation(angle + GetIndexAngle(i, m_componentProximity));

            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].Fire(magnitude);
        }

        private float GetIndexAngle(int index, float squash)
        {
            if (m_rockets.Count <= 1)
                return 0f;

            float x = m_rockets.Count;
            float inverseLerp = Mathf.InverseLerp(0f, 360f, squash);
            float result = Mathf.Lerp(0f, (180f * (x - 1)) / x, inverseLerp);

            return Mathf.Lerp(-result, result, index / (m_rockets.Count - 1f));
        }

        protected override void OnSetAim(float angle)
        {
            base.OnSetAim(angle);

            if (IsSecondaryFireHeld)
                return;
            
            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].SetRotation(angle + GetIndexAngle(i, m_componentProximity));
        }

        public override void SetColour(Color col)
        {
            base.SetColour(col);

            for (int i = 0; i < m_rockets.Count; i++)
                m_rockets[i].SetSpriteColour(col);
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);

            if (!selected)
                for (int i = 0; i < m_rockets.Count; i++)
                    m_rockets[i].Reset();
        }
    }
}