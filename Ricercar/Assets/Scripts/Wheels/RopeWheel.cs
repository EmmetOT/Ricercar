﻿using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar
{
    public class RopeWheel : Wheel
    {
        public override Type WheelType => Type.ROPE;

        [SerializeField]
        private RopeLauncher m_ropeLauncherPrefab;

        private static Pool<RopeLauncher> m_ropeLauncherPool;
        
        private readonly List<RopeLauncher> m_ropeLaunchers = new List<RopeLauncher>();
        
        protected override bool CanAim
        {
            get
            {
                for (int i = m_ropeLaunchers.Count - 1; i >= 0; --i)
                    if (m_ropeLaunchers[i].IsActive)
                        return false;

                return true;
            }
        }

        public override void Initialize(int componentCount, float componentProximity, Color selectedColour, Color unselectedColour, int index, ObiSolver solver, Material material, Rigidbody2D rigidbody, ObiCollider2D parentCollider)
        {
            base.Initialize(componentCount, componentProximity, selectedColour, unselectedColour, index, solver, material, rigidbody, parentCollider);

            if (m_ropeLauncherPool == null)
                m_ropeLauncherPool = new Pool<RopeLauncher>(m_ropeLauncherPrefab);

            Debug.Log("Creating a rope wheel with: " + m_componentCount);

            for (int i = 0; i < m_componentCount; i++)
            {
                RopeLauncher ropeLauncher = m_ropeLauncherPool.GetNew();
                ropeLauncher.transform.SetParent(m_transform);
                ropeLauncher.transform.Reset();
                ropeLauncher.Initialize(m_rigidbody, m_solver, SourceDistance, m_raycastFilter, m_material);

                m_ropeLaunchers.Add(ropeLauncher);
            }
        }

        public override void SetColour(Color col)
        {
            base.SetColour(col);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].SetSpriteColour(col);
        }

        private float GetIndexAngle(int index, float squash)
        {
            if (m_ropeLaunchers.Count <= 1)
                return 0f;

            float x = m_ropeLaunchers.Count;
            float inverseLerp = Mathf.InverseLerp(0f, 360f, squash);
            float result = Mathf.Lerp(0f, (180f * (x - 1)) / x, inverseLerp);

            return Mathf.Lerp(-result, result, index / (m_ropeLaunchers.Count - 1f));
        }

        protected override void OnSetAim(float angle)
        {
            base.OnSetAim(angle);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].SetRotation(angle + GetIndexAngle(i, m_componentProximity));
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);

            if (!m_ropeLaunchers[0].IsActive)
                return;

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].ManualUpdate(deltaTime);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DetachRopes();
        }

        public override void PrimaryFire() => LaunchRopes();

        public override void SecondaryFire() => DetachRopes();

        public override void OnScroll(float delta) => ChangeRopesLength(delta);

        public void LaunchRopes()
        {
            if (m_ropeLaunchers[0].IsActive)
                return;

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                StartCoroutine(m_ropeLaunchers[i].Cr_Launch());
        }

        public void DetachRopes()
        {
            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].DetachRope();
        }

        public void ChangeRopesLength(float delta)
        {
            if (!m_ropeLaunchers[0].IsActive)
                return;

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].AddToRopeLength(delta);
        }
    }
}
