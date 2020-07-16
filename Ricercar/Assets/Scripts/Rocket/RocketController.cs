using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Character
{
    public class RocketController : MonoBehaviour
    {
        [SerializeField]
        private WarpGimbal m_warpGimbal;

        [SerializeField]
        [GravityLayer]
        private int m_defaultLayer;

        private GravityQueryObject m_gravityQuery;

        public Vector2 CurrentGravityWithoutWarp => m_gravityQuery.CurrentGravity;

        [SerializeField]
        [ReadOnly]
        private bool m_canEnter = false;

        [SerializeField]
        private bool m_hasPilot = false;

        [SerializeField]
        [ReadOnly]
        private CharacterController m_characterController; // temp until we have a proper character script

        [SerializeField]
        private CharacterInputProcessor m_input;

        [SerializeField]
        [BoxGroup("Components")]
        private SimpleRigidbodyAttractor m_attractor;
        public Attractor Attractor => m_attractor;

        [SerializeField]
        private Gimbal[] m_gimbals;

        private Transform m_transform;
        private Camera m_camera;

        private Vector2 m_currentAim;
        private Vector2 m_currentMovement;

        private void Start()
        {
            m_transform = transform;
            m_gravityQuery = new GravityQueryObject(Attractor.GravityField, m_defaultLayer, m_transform);
        }

        private void OnEnable()
        {
            m_transform = transform;
            m_camera = Camera.main;

            m_input.OnSpaceDown += OnSpaceDown;
            m_input.OnSpaceUp += OnSpaceUp;
        }

        private void OnDisable()
        {
            m_input.OnSpaceDown -= OnSpaceDown;
            m_input.OnSpaceUp -= OnSpaceUp;
        }

        private void OnDrawGizmos()
        {
            if (EditorApplication.isPlaying)
                Utils.DrawArrow(m_transform.position, CurrentGravityWithoutWarp, Color.cyan, 0.2f, 0.4f);
        }

        public void SetHasPilot(bool hasPilot)
        {
            m_hasPilot = hasPilot;

            if (m_hasPilot && m_characterController != null)
            {
                Debug.Log("Doing the thing.");

                m_characterController.transform.SetParent(m_transform);
                m_characterController.transform.localPosition = Vector2.zero;
                m_characterController.transform.localRotation = Quaternion.identity;
                m_characterController.gameObject.SetActive(false);

                for (int i = 0; i < m_gimbals.Length; i++)
                {
                    m_gimbals[i].SetActive(true);
                }
            }
        }

        private void Update()
        {
            m_input.ManualUpdate();

            if (!m_hasPilot)
                return;

            m_currentAim = m_input.GetAimDirection(m_transform.position, m_camera);
            m_currentMovement = m_input.MoveDirection;

            for (int i = 0; i < m_gimbals.Length; i++)
            {
                m_gimbals[i].SetAim(m_currentAim);
                m_gimbals[i].SetMovement(m_currentMovement);
            }
        }

        private void FixedUpdate()
        {
            m_input.ManualFixedUpdate();

            //for (int i = 0; i < m_gimbals.Length; i++)
            //    m_gimbals[i].SetGravity(m_warpGimbal.GravityWithoutWarpInfluence);
        }

        private void OnSpaceDown()
        {
            if (m_hasPilot)
            {
                for (int i = 0; i < m_gimbals.Length; i++)
                    m_gimbals[i].SetSpaceDown();
            }
        }

        private void OnSpaceUp()
        {
            if (m_hasPilot)
            {
                for (int i = 0; i < m_gimbals.Length; i++)
                    m_gimbals[i].SetSpaceUp();
            }
            else if (m_canEnter)
            {
                Debug.Log("Setting has pilot to true");
                SetHasPilot(true);
            }
        }

        public void SetCanEnter(bool canEnter, CharacterController characterController = null)
        {
            m_canEnter = canEnter;
            m_characterController = characterController;
        }
    }
}