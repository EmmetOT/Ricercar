using Obi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

namespace Ricercar
{
    public class RopeWheel : Wheel
    {
        public override Type WheelType => Type.ROPE;

        [SerializeField]
        private RopeLauncher m_ropeLauncherPrefab;

        [SerializeField]
        [MinValue(0f)]
        [MaxValue(180f)]
        private float m_ropeSelectionAngularTolerance = 30f;

        private static Pool<RopeLauncher> m_ropeLauncherPool;
        
        private readonly List<RopeLauncher> m_ropeLaunchers = new List<RopeLauncher>();

        private int m_currentRopeIndex = 0;

        protected override bool CanAim
        {
            get
            {
                for (int i = m_ropeLaunchers.Count - 1; i >= 0; --i)
                    if (!m_ropeLaunchers[i].IsActive)
                        return true;

                return false;
            }
        }

        public override void Initialize(int componentCount, float componentProximity, Color selectedColour, Color unselectedColour, int index, ObiSolver solver, Material material, IAttractor attractor, ObiCollider2D parentCollider)
        {
            base.Initialize(componentCount, componentProximity, selectedColour, unselectedColour, index, solver, material, attractor, parentCollider);

            m_currentRopeIndex = 0;

            if (m_ropeLauncherPool == null)
                m_ropeLauncherPool = new Pool<RopeLauncher>(m_ropeLauncherPrefab);

            for (int i = 0; i < m_componentCount; i++)
            {
                RopeLauncher ropeLauncher = m_ropeLauncherPool.GetNew();
                ropeLauncher.transform.SetParent(m_transform);
                ropeLauncher.transform.Reset();
                ropeLauncher.Initialize(m_attractor, m_solver, SourceDistance, m_raycastFilter, m_material);

                m_ropeLaunchers.Add(ropeLauncher);
            }
        }

        public override void PrimaryFire() => LaunchNextRope();

        public override void SecondaryFire() => DetachCurrentAimedRope();

        public override void OnScroll(float delta) => ChangeRopesLength(delta);

        public override void SetColour(Color col)
        {
            base.SetColour(col);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].SetColour(col);
        }

        //private float GetIndexAngle(int index, float squash)
        //{
        //    if (m_ropeLaunchers.Count <= 1)
        //        return 0f;

        //    float x = m_ropeLaunchers.Count;
        //    float inverseLerp = Mathf.InverseLerp(0f, 360f, squash);
        //    float result = Mathf.Lerp(0f, (180f * (x - 1)) / x, inverseLerp);

        //    return Mathf.Lerp(-result, result, index / (m_ropeLaunchers.Count - 1f));
        //}

        protected override void OnSetAim(float angle)
        {
            base.OnSetAim(angle);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                if (!m_ropeLaunchers[i].IsActive)
                    m_ropeLaunchers[i].SetRotation(angle);// angle + GetIndexAngle(i, m_componentProximity));

            int aimedRope = FindAimedRope(angle);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
            {
                if (m_ropeLaunchers[i].IsActive)
                    m_ropeLaunchers[i].SetColour(aimedRope == i ? Color.red : (m_isSelected ? m_selectedColour : m_unselectedColour));
            }
        }

        /// <summary>
        /// Find the index of the rope being aimed at, within a 'cone of tolerance.'
        /// </summary>
        private int FindAimedRope(float angle)
        {
            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                if (m_ropeLaunchers[i].IsActive)
                    if (Utils.IsInSpan(m_ropeLaunchers[i].Angle, angle - m_ropeSelectionAngularTolerance * 0.5f, angle + m_ropeSelectionAngularTolerance * 0.5f))
                        return i;

            return -1;
        }

        public override void ManualUpdate(float deltaTime)
        {
            base.ManualUpdate(deltaTime);

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                m_ropeLaunchers[i].ManualUpdate(deltaTime);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            DetachAllRopes();
        }

        public void LaunchNextRope()
        {
            LaunchRope(m_currentRopeIndex);

            m_currentRopeIndex = GetNextAvailableRope(m_currentRopeIndex);
        }

        /// <summary>
        /// Return the next free rope after the given index. Leave index
        /// empty to start at 0.
        /// </summary>
        private int GetNextAvailableRope(int from = -1)
        {
            ++from;

            for (int i = 0; i < m_ropeLaunchers.Count; i++)
            {
                int index = (from + i) % m_ropeLaunchers.Count;

                if (!m_ropeLaunchers[index].IsActive)
                    return index;
            }

            return -1;
        }

        public void LaunchAllRopes()
        {
            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                LaunchRope(i);
        }

        public void LaunchRope(int ropeIndex)
        {
            if (m_ropeLaunchers.IsOutOfRange(ropeIndex) || m_ropeLaunchers[ropeIndex].IsActive)
                return;

            StartCoroutine(m_ropeLaunchers[ropeIndex].Cr_Launch());
        }

        public void DetachAllRopes()
        {
            for (int i = 0; i < m_ropeLaunchers.Count; i++)
                DetachRope(i);
        }

        public void DetachCurrentAimedRope()
        {
            int aimedRope = FindAimedRope(CurrentAim);

            if (aimedRope != -1)
                DetachRope(aimedRope);
        }

        public void DetachRope(int ropeIndex)
        {
            if (m_ropeLaunchers.IsOutOfRange(ropeIndex) || !m_ropeLaunchers[ropeIndex].IsActive)
                return;

            m_ropeLaunchers[ropeIndex].DetachRope();
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
