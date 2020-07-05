using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Character
{
    [RequireComponent(typeof(Timer))]
    public class RocketLegAnimator : MonoBehaviour
    {
        [SerializeField]
        private Timer m_timer;

        [SerializeField]
        private Transform m_legOne;
        
        [SerializeField]
        private Transform m_legTwo;

        private Quaternion m_legOneStartRotation;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Part One")]
        private float m_partOneTime;

        [SerializeField]
        [BoxGroup("Part One")]
        private float m_partOneDisplacement;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Part Two")]
        private float m_partTwoTime;

        [SerializeField]
        [BoxGroup("Part Two")]
        private float m_partTwoRotation;

        private Quaternion m_legOnePartTwoRotation;

        private Vector2 m_legOneStartPos;
        private Vector2 m_legOneEndPos;

        private void Start()
        {
            m_legOneStartRotation = m_legOne.transform.localRotation;
            m_legOnePartTwoRotation = Quaternion.Euler(m_legOneStartRotation.eulerAngles + Vector3.forward * m_partTwoRotation);

            m_legOneStartPos = m_legOne.transform.localPosition;
            m_legOneEndPos = m_legOne.transform.localPosition + m_legOne.transform.up * m_partOneDisplacement;
        }

        private int m_currentStep = -1;

        [Button]
        private void Play()
        {
            AdvanceStep();
        }

        private void DisplaceLegOne(float t)
        {
            m_legOne.transform.localPosition = Vector2.Lerp(m_legOneStartPos, m_legOneEndPos, t);
        }

        private void DisplaceRotateLegOne(float t)
        {
            m_legOne.transform.localRotation = Quaternion.Slerp(m_legOneStartRotation, m_legOnePartTwoRotation, t);
        }

        private void AdvanceStep()
        {
            ++m_currentStep;

            switch (m_currentStep)
            {
                case 0:
                    m_timer.StartLerpTimer(0, DisplaceLegOne, m_partOneTime, AdvanceStep);
                    break;
                case 1:
                    m_timer.StartLerpTimer(0, DisplaceRotateLegOne, m_partTwoTime, AdvanceStep);
                    break;
            }

        }
    }
}
