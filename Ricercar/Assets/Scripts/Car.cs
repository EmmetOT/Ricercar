using Obi;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using System.Linq;
using Ricercar.Gravity;

namespace Ricercar
{
    public class Car : MonoBehaviour
    {
        [SerializeField]
        private ObiSolver m_solver;

        [SerializeField]
        private Wheel[] m_wheelPrefabs;

        private static MultiPool<Wheel.Type, Wheel> m_wheelPool;

        [SerializeField]
        private Wheel.Data[] m_wheels;

        private readonly List<Wheel> m_currentWheels = new List<Wheel>();

        [SerializeField]
        private Attractor m_attractor;

        [SerializeField]
        private ObiCollider2D m_collider;

        [SerializeField]
        private Material m_material;

        [SerializeField]
        private float m_ropeScrollSpeed = 2f;

        [SerializeField]
        private Color m_selectedColour;

        [SerializeField]
        private Color m_unselectedColour;

        private Transform m_transform;
        private Camera m_camera;

        private int m_currentWheelIndex = 0;

        private ObiRopeBlueprint m_ropeBlueprint;

        /// <summary>
        /// Enumerate all wheels that are at the active wheel index or are set to 'always active.'
        /// </summary>
        public IEnumerable<Wheel> ActiveWheels
        {
            get
            {
                for (int i = 0; i < m_currentWheels.Count; i++)
                    if (i == m_currentWheelIndex || m_wheels[i].AlwaysActive)
                        yield return m_currentWheels[i];
            }
        }

        private void Awake()
        {
            if (m_wheelPool == null)
            {
                m_wheelPool = new MultiPool<Wheel.Type, Wheel>();

                for (int i = 0; i < m_wheelPrefabs.Length; i++)
                {
                    m_wheelPool.CreatePool(m_wheelPrefabs[i].WheelType, m_wheelPrefabs[i]);
                }
            }

            m_transform = transform;
            m_camera = Camera.main;

            m_currentWheelIndex = 0;

            if (m_wheels.Length > 1)
            {
                do
                {
                    m_currentWheelIndex = m_currentWheelIndex.Increment(m_wheels);
                } while (m_wheels[m_currentWheelIndex].AlwaysActive && m_currentWheelIndex != 0);
            }

            for (int i = 0; i < m_wheels.Length; i++)
            {
                Wheel wheel = m_wheelPool.GetNew(m_wheels[i].Type);
                wheel.transform.SetParent(m_transform);
                wheel.transform.localPosition = Vector3.zero;
                wheel.Initialize(m_wheels[i].ComponentCount, m_wheels[i].ComponentProximity, m_selectedColour, m_unselectedColour, i, m_solver, m_material, m_attractor, m_collider);
                wheel.SetSelected(i == m_currentWheelIndex);

                m_currentWheels.Add(wheel);
            }
        }

        private bool m_primaryFire = false;
        private bool m_secondaryFire = false;
        private bool m_holdPrimaryFire = false;
        private bool m_holdSecondaryFire = false;

        private void Update()
        {
            for (int i = 0; i < m_currentWheels.Count; i++)
                m_currentWheels[i].ManualUpdate(Time.deltaTime);

            Vector2 keyboardAim = GetCurrentKeyboardAim();
            Vector2 mouseAim = GetCurrentMouseAim();

            float keyboardAngle = -Vector2.SignedAngle(Vector2.down, keyboardAim);
            float mouseAngle = -Vector2.SignedAngle(Vector2.up, mouseAim);

            for (int i = 0; i < m_currentWheels.Count; i++)
            {
                if (i != m_currentWheelIndex && !m_wheels[i].AlwaysActive)
                    continue;

                Wheel wheel = m_currentWheels[i];

                wheel.SetAim(m_wheels[i].Control == Wheel.Control.KEYBOARD ? keyboardAngle : mouseAngle);

                if (m_wheels[i].Control == Wheel.Control.MOUSE)
                    wheel.OnScroll(Input.mouseScrollDelta.y * m_ropeScrollSpeed * Time.deltaTime);
            }

            if (Input.GetMouseButtonUp(0))
                m_primaryFire = true;
            else if (Input.GetMouseButtonUp(1))
                m_secondaryFire = true;

            m_holdPrimaryFire = Input.GetMouseButton(0);
            m_holdSecondaryFire = Input.GetMouseButton(1);

            if (Input.GetKeyUp(KeyCode.Q))
                DecrementCurrentWheel();
            else if (Input.GetKeyUp(KeyCode.E))
                IncrementCurrentWheel();
        }

        private void FixedUpdate()
        {
            Vector2 keyboardAim = GetCurrentKeyboardAim();

            for (int i = 0; i < m_currentWheels.Count; i++)
            {
                if (i != m_currentWheelIndex && !m_wheels[i].AlwaysActive)
                    continue;

                Wheel wheel = m_currentWheels[i];

                if (m_wheels[i].Control == Wheel.Control.KEYBOARD)
                {
                    m_holdPrimaryFire = !keyboardAim.IsZero();
                    m_holdSecondaryFire = Input.GetKey(KeyCode.Space);
                }
                else if (m_wheels[i].Control == Wheel.Control.MOUSE)
                {
                    if (m_primaryFire)
                        wheel.PrimaryFire();

                    if (m_secondaryFire)
                        wheel.SecondaryFire();
                }

                if (m_holdSecondaryFire)
                    wheel.HoldSecondaryFire();
                else if (wheel.IsSecondaryFireHeld)
                    wheel.ReleaseHoldSecondaryFire();

                if (m_holdPrimaryFire)
                    wheel.HoldPrimaryFire();
                else if (wheel.IsPrimaryFireHeld)
                    wheel.ReleaseHoldPrimaryFire();
            }

            m_holdPrimaryFire = false;
            m_holdSecondaryFire = false;

            m_primaryFire = false;
            m_secondaryFire = false;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_ropeBlueprint);
        }

        public void IncrementCurrentWheel()
        {
            m_currentWheels[m_currentWheelIndex].SetSelected(false);

            int startingPoint = m_currentWheelIndex;

            do
            {
                m_currentWheelIndex = m_currentWheelIndex.Increment(m_currentWheels);
            } while (m_wheels[m_currentWheelIndex].AlwaysActive && m_currentWheels.Count > 1 && m_currentWheelIndex != startingPoint);

            m_currentWheels[m_currentWheelIndex].SetSelected(true);
        }

        public void DecrementCurrentWheel()
        {
            m_currentWheels[m_currentWheelIndex].SetSelected(false);

            int startingPoint = m_currentWheelIndex;

            do
            {
                m_currentWheelIndex = m_currentWheelIndex.Decrement(m_currentWheels);
            } while (m_wheels[m_currentWheelIndex].AlwaysActive && m_currentWheels.Count > 1 && m_currentWheelIndex != startingPoint);

            m_currentWheels[m_currentWheelIndex].SetSelected(true);
        }

        private Vector2 GetCurrentMouseAim()
        {
            Vector2 mousePos = Utils.GetMousePos2D(m_camera);

            return (mousePos - (Vector2)m_transform.position.SetZ(0f)).normalized;
        }

        private Vector2 GetCurrentKeyboardAim()
        {
            Vector2 sum = Vector2.zero;

            Vector2 currentDown = Attractor.GetGravityAtPoint(m_attractor).normalized;

            if (currentDown.IsZero())
                currentDown = Vector2.down;

            Vector2 currentRight = Vector3.Cross(currentDown, Vector3.back);

            sum += Input.GetKey(KeyCode.W) ? -currentDown : Vector2.zero;
            sum += Input.GetKey(KeyCode.S) ? currentDown : Vector2.zero;
            sum += Input.GetKey(KeyCode.A) ? -currentRight : Vector2.zero;
            sum += Input.GetKey(KeyCode.D) ? currentRight : Vector2.zero;

            return sum.normalized;
        }

        [Button("Reduce Velocity")]
        private void ReduceVelocity()
        {
            m_attractor.Rigidbody.velocity = Vector2.zero;
            m_attractor.Rigidbody.angularVelocity = 0f;
        }
    }
}


