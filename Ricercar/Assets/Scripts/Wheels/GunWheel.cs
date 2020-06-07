using System.Collections;
using System.Collections.Generic;
//using Obi;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    public class GunWheel : Wheel
    {
        public override Type WheelType => Type.GUN;
        
        [SerializeField]
        private Gun m_gunPrefab;

        private static Pool<Gun> m_gunPool;

        private readonly List<Gun> m_guns = new List<Gun>();

        [SerializeField]
        [MinValue(1)]
        private int m_gunCount = 1;
        
        protected override bool CanAim => base.CanAim && !IsSecondaryFireHeld;

        public override void Initialize(int componentCount, float componentProximity, Color selectedColour, Color unselectedColour, int index/*, ObiSolver solver*/, Material material, IAttractor attractor/*, ObiCollider2D parentCollider*/)
        {
            base.Initialize(componentCount, componentProximity, selectedColour, unselectedColour, index/*, solver*/, material, attractor/*, parentCollider*/);
            
            if (m_gunPool == null)
                m_gunPool = new Pool<Gun>(m_gunPrefab);

            for (int i = 0; i < m_componentCount; i++)
            {
                Gun gun = m_gunPool.GetNew();
                gun.transform.SetParent(m_transform);
                gun.transform.Reset();
                gun.Initialize(m_attractor, SourceDistance);

                m_guns.Add(gun);
            }
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);

            for (int i = 0; i < m_guns.Count; i++)
                m_guns[i].ManualUpdate(deltaTime);
        }

        public override void HoldPrimaryFire()
        {
            base.HoldPrimaryFire();
            
            Vector2 sumForce = Vector2.zero;

            for (int i = 0; i < m_guns.Count; i++)
                sumForce += m_guns[i].Fire();

            //m_attractor.Rigidbody.AddForce(-sumForce);
        }

        public override void SecondaryFire()
        {
            base.SecondaryFire();

            for (int i = 0; i < m_guns.Count; i++)
                m_guns[i].DespawnProjectiles();
        }

        private float GetIndexAngle(int index, float squash)
        {
            if (m_guns.Count <= 1)
                return 0f;

            float x = m_guns.Count;
            float inverseLerp = Mathf.InverseLerp(0f, 360f, squash);
            float result = Mathf.Lerp(0f, (180f * (x - 1)) / x, inverseLerp);

            return Mathf.Lerp(-result, result, index / (m_guns.Count - 1f));
        }

        protected override void OnSetAim(float angle)
        {
            base.OnSetAim(angle);

            if (IsSecondaryFireHeld)
                return;
            
            for (int i = 0; i < m_guns.Count; i++)
                m_guns[i].SetRotation(angle + GetIndexAngle(i, m_componentProximity));
        }

        public override void SetColour(Color col)
        {
            base.SetColour(col);

            for (int i = 0; i < m_guns.Count; i++)
                m_guns[i].SetSpriteColour(col);
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);

            if (!selected)
                for (int i = 0; i < m_guns.Count; i++)
                    m_guns[i].Reset();
        }
    }
}