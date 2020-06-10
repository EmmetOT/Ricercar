using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ricercar.Gravity;
using NaughtyAttributes;
using UnityEditor;

namespace Ricercar.Character
{
    public class CharacterController : MonoBehaviour
    {
        [SerializeField]
        private CharacterInputProcessor m_input;

        [SerializeField]
        [BoxGroup("Components")]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        [BoxGroup("Components")]
        private SimpleRigidbodyAttractor m_attractor;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_speed;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_maxAcceleration;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_maxAirborneAcceleration;

        [SerializeField]
        [Range(0f, 90f)]
        [BoxGroup("Controls")]
        private float m_maxGroundAngle;

        private float m_minGroundDotProduct;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_jumpHeight;

        [SerializeField]
        [MinValue(0)]
        [BoxGroup("Controls")]
        private int m_maxAirJumps = 0;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private bool m_isGrounded;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private Vector2 m_currentVelocity;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private Vector2 m_desiredVelocity;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private Vector2 m_currentContactNormal;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private int m_jumps = 0;

        private Transform m_transform;

        public Vector2 Up => m_attractor == null ? Vector2.up : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.up);
        public Vector2 Down => m_attractor == null ? Vector2.down : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.down);
        public Vector2 Left => m_attractor == null ? Vector2.left : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.left);
        public Vector2 Right => m_attractor == null ? Vector2.right : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.right);

        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
        }

        private void OnEnable()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);

            m_transform = transform;

            m_input.OnJumpInput += OnJumpInput;
        }

        private void OnDisable()
        {
            m_input.OnJumpInput -= OnJumpInput;
        }

        private void Update()
        {
            m_input.ManualUpdate();
            m_desiredVelocity = m_input.MoveDirection * m_speed;
        }

        private void UpdateState()
        {
            m_currentVelocity = m_rigidbody.velocity;

            if (m_isGrounded)
            {
                m_jumps = 0;
            }
            else
            {
                // TODO: generalize for any gravity direction
                m_currentContactNormal = Vector2.up;
            }
        }

        private void ResetState()
        {
            m_isGrounded = false;
            m_currentContactNormal = Vector2.zero;
        }

        private void FixedUpdate()
        {
            UpdateState();

            // note that all the below takes place in "normal gravity space"
            // and that transformations will need to be added from and then back to the 
            // distorted gravity
            // (adapted from catlike coding)
            // TODO: generalize for any gravity direction

            //float maxSpeedChange = (m_isGrounded ? m_maxAcceleration : m_maxAirborneAcceleration) * Time.fixedDeltaTime;
            //m_currentVelocity.x = Mathf.MoveTowards(m_currentVelocity.x, m_desiredVelocity.x, maxSpeedChange);

            //if (!m_isGrounded)
            //{
            //    Debug.Log("Speed change = " + maxSpeedChange);
            //}

            AdjustVelocityOriginal();

            m_input.ManualFixedUpdate();

            m_rigidbody.velocity = m_currentVelocity;

            ResetState();
        }

        private void OnJumpInput()
        {
            if (m_isGrounded || m_jumps < m_maxAirJumps)
            {
                ++m_jumps;

                // TODO: generalize for any gravity direction
                float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * m_jumpHeight);

                float alignedSpeed = Vector3.Dot(m_currentVelocity, m_currentContactNormal);

                if (alignedSpeed > 0f)
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

                m_currentVelocity += m_currentContactNormal * jumpSpeed;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            EvaluateCollision(collision);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            EvaluateCollision(collision);
        }

        private void EvaluateCollision(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                // TODO: generalize for any gravity direction
                Vector2 normal = collision.GetContact(i).normal;
                if (normal.y >= m_minGroundDotProduct)
                {
                    m_isGrounded = true;
                    m_currentContactNormal += normal;
                }
            }

            if (collision.contactCount > 0)
                m_currentContactNormal /= collision.contactCount;

            if (collision.contactCount > 1)
                Debug.Log("Greater than 1!");
        }

        /// <summary>
        /// Given a direction vector, project it so that it's aligned with whatever
        /// plane the character is standing on.
        /// </summary>
        private Vector2 ProjectOnContactPlane(Vector2 vector)
        {
            return vector - m_currentContactNormal * Vector2.Dot(vector, m_currentContactNormal);
        }

        private void AdjustVelocity()
        {
            // TODO: generalize for any gravity direction

            float maxSpeedChange = (m_isGrounded ? m_maxAcceleration : m_maxAirborneAcceleration) * Time.deltaTime;

            // gives the new "right" direction on the current ground
            Vector2 xAxis = ProjectOnContactPlane(Vector2.right).normalized;
            Vector2 yAxis = ProjectOnContactPlane(Vector2.up).normalized;

            Vector2 desiredVelocity = Quaternion.FromToRotation(Vector2.right, xAxis) * m_desiredVelocity;

            float currentX = Vector3.Dot(m_currentVelocity, xAxis);
            float currentY = Vector3.Dot(m_currentVelocity, yAxis);

            float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
            float newY = Mathf.MoveTowards(currentY, desiredVelocity.y, maxSpeedChange);

            //m_currentVelocity = Vector2.MoveTowards(m_currentVelocity, desiredVelocity, maxSpeedChange);

            m_currentVelocity += xAxis * (newX - currentX) + yAxis * (newY - currentY);
        }

        private void AdjustVelocityOriginal()
        {
            Vector2 xAxis = ProjectOnContactPlane(Vector2.right).normalized;
            float currentX = Vector2.Dot(m_currentVelocity, xAxis);

            float maxSpeedChange = (m_isGrounded ? m_maxAcceleration : m_maxAirborneAcceleration) * Time.deltaTime;

            // problem: while airborne, desiredVelocity.x goes to zero, so any horizontal movement is quickly removed
            float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, maxSpeedChange);

            m_currentVelocity += xAxis * (newX - currentX);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(m_currentVelocity.normalized * 2f));
        }
    }
}