using Obi;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

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
        private Rigidbody2D m_rigidbody;

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

        private int m_currentWheel = 0;

        private ObiRopeBlueprint m_ropeBlueprint;

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
            
            for (int i = 0; i < m_wheels.Length; i++)
            {
                Debug.Log(i + ") " + m_wheels[i].Type.ToString() + ", " + m_wheels[i].ComponentCount.ToString());

                Wheel wheel = m_wheelPool.GetNew(m_wheels[i].Type);
                wheel.transform.SetParent(m_transform);
                wheel.transform.localPosition = Vector3.zero;
                wheel.Initialize(m_wheels[i].ComponentCount, m_wheels[i].ComponentProximity, m_selectedColour, m_unselectedColour, i, m_solver, m_material, m_rigidbody, m_collider);
                wheel.SetSelected(i == m_currentWheel);

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

            if (GetCurrentHeldDirection() == Vector2.zero)
            {
                float angle = Vector3.SignedAngle(Vector3.up, GetCurrentMouseAim(), Vector3.back);

                m_currentWheels[m_currentWheel].SetAim(angle);
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

            if (Input.mouseScrollDelta.y != 0f)
                m_currentWheels[m_currentWheel].OnScroll(Input.mouseScrollDelta.y * m_ropeScrollSpeed * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            Vector2 heldDirection = GetCurrentHeldDirection();

            if (heldDirection != Vector2.zero)
            {
                m_holdPrimaryFire = false;
                m_holdSecondaryFire = false;

                if (m_currentWheels[m_currentWheel].IsPrimaryFireHeld)
                    m_currentWheels[m_currentWheel].ReleaseHoldPrimaryFire();

                if (m_currentWheels[m_currentWheel].IsSecondaryFireHeld)
                    m_currentWheels[m_currentWheel].ReleaseHoldSecondaryFire();

                float angle = Vector3.SignedAngle(Vector3.down, heldDirection, Vector3.back);

                m_currentWheels[m_currentWheel].SetAim(angle);
                m_currentWheels[m_currentWheel].HoldPrimaryFire();
            }
            else
            {
                if (m_primaryFire)
                    m_currentWheels[m_currentWheel].PrimaryFire();

                if (m_secondaryFire)
                    m_currentWheels[m_currentWheel].SecondaryFire();

                if (m_holdSecondaryFire)
                    m_currentWheels[m_currentWheel].HoldSecondaryFire();
                else if (m_currentWheels[m_currentWheel].IsSecondaryFireHeld)
                    m_currentWheels[m_currentWheel].ReleaseHoldSecondaryFire();

                if (m_holdPrimaryFire)
                    m_currentWheels[m_currentWheel].HoldPrimaryFire();
                else if (m_currentWheels[m_currentWheel].IsPrimaryFireHeld)
                    m_currentWheels[m_currentWheel].ReleaseHoldPrimaryFire();
            }
            
            m_primaryFire = false;
            m_secondaryFire = false;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_ropeBlueprint);
        }

        public void IncrementCurrentWheel()
        {
            m_currentWheels[m_currentWheel].SetSelected(false);
            m_currentWheel = (m_currentWheel + 1) % m_wheels.Length;
            m_currentWheels[m_currentWheel].SetSelected(true);

        }

        public void DecrementCurrentWheel()
        {
            m_currentWheels[m_currentWheel].SetSelected(false);
            m_currentWheel--;

            if (m_currentWheel < 0)
                m_currentWheel = m_wheels.Length - 1;

            m_currentWheels[m_currentWheel].SetSelected(true);
        }

        private Vector3 GetCurrentMouseAim()
        {
            Vector3 mousePos = Utils.GetMousePos2D(m_camera);

            return (mousePos - m_transform.position).normalized;
        }

        private Vector2 GetCurrentHeldDirection()
        {
            Vector2 sum = Vector2.zero;

            sum += Input.GetKey(KeyCode.W) ? Vector2.up : Vector2.zero;
            sum += Input.GetKey(KeyCode.S) ? Vector2.down : Vector2.zero;
            sum += Input.GetKey(KeyCode.A) ? Vector2.left : Vector2.zero;
            sum += Input.GetKey(KeyCode.D) ? Vector2.right : Vector2.zero;

            return sum.normalized;
        }

        [Button("Reduce Velocity")]
        private void ReduceVelocity()
        {
            m_rigidbody.velocity = Vector2.zero;
            m_rigidbody.angularVelocity = 0f;
        }
    }
}


