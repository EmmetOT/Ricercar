using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ricercar.Gravity;
using NaughtyAttributes;
using UnityEditor;

namespace Ricercar.Character
{
    /// <summary>
    /// This is the controller for a 2D character. It is strongly based on Catlike Coding's movement
    /// tutorial, but adapted for 2D and with a variable gravity direction. The input elements have also
    /// been pulled into a separate script.
    /// </summary>
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
        private float m_maxGroundAngle = 25f;

        private float m_minGroundDotProduct;

        [SerializeField]
        [Range(0f, 90f)]
        [BoxGroup("Controls")]
        private float m_maxStairsAngle = 50f;

        private float m_minStairsDotProduct;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_jumpHeight;

        [SerializeField]
        [MinValue(0)]
        [BoxGroup("Controls")]
        private int m_maxAirJumps = 0;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_maxSnapSpeed = 5f;

        [SerializeField]
        [MinValue(0f)]
        [BoxGroup("Controls")]
        private float m_groundProbeDistance = 1f;

        [SerializeField]
        [BoxGroup("Controls")]
        private LayerMask m_groundProbeLayerMask;

        [SerializeField]
        [BoxGroup("Controls")]
        private LayerMask m_stairsProbeLayerMask;

        [SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_groundContactCount;

        public bool IsGrounded => m_groundContactCount > 0;

        [SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_steepContactCount;

        public bool IsTouchingSteep => m_steepContactCount > 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentVelocity;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_desiredVelocity;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentGroundNormal;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private Vector2 m_currentSteepNormal;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_stepsSinceLastGrounded = 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_stepsSinceLastJump = 0;

        //[SerializeField]
        //[ReadOnly]
        //[BoxGroup("State")]
        private int m_jumps = 0;

        private Transform m_transform;

        public Vector2 CurrentGravity => m_attractor == null ? Physics2D.gravity : m_attractor.CurrentGravity;
        public float GravityMagnitude => CurrentGravity.magnitude;
        public Vector2 Up => m_attractor == null ? Vector2.up : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.up);
        public Vector2 Down => m_attractor == null ? Vector2.down : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.down);
        public Vector2 Left => m_attractor == null ? Vector2.left : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.left);
        public Vector2 Right => m_attractor == null ? Vector2.right : GravityField.ConvertDirectionToGravitySpace(m_attractor.CurrentGravity.normalized, Vector2.right);

        private void OnValidate()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);
        }

        private void OnEnable()
        {
            m_minGroundDotProduct = Mathf.Cos(m_maxGroundAngle * Mathf.Deg2Rad);
            m_minStairsDotProduct = Mathf.Cos(m_maxStairsAngle * Mathf.Deg2Rad);

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
            ++m_stepsSinceLastGrounded;
            ++m_stepsSinceLastJump;

            m_currentVelocity = m_rigidbody.velocity;

            if (IsGrounded || SnapToGround() || CheckSteepContacts())
            {
                m_stepsSinceLastGrounded = 0;

                if (m_stepsSinceLastJump > 1)
                    m_jumps = 0;
            }
            else
            {
                m_currentGroundNormal = Up;
            }
        }

        private void ResetState()
        {
            m_groundContactCount = 0;
            m_steepContactCount = 0;
            m_currentGroundNormal = Vector2.zero;
            m_currentSteepNormal = Vector2.zero;
        }

        private void FixedUpdate()
        {
            UpdateState();

            AdjustVelocity();

            m_input.ManualFixedUpdate();

            m_rigidbody.SetRotation(Quaternion.FromToRotation(Vector2.up, Up));
            m_rigidbody.angularVelocity = 0f;

            //m_currentVelocity += CurrentGravity * Time.fixedDeltaTime;

            m_rigidbody.velocity = m_currentVelocity;

            // this works. how to phrase it in terms of velocity?
            m_rigidbody.AddForce(CurrentGravity * Time.fixedDeltaTime);

            ResetState();
        }

        //private void FixedUpdate()
        //{
        //    UpdateState();

        //    AdjustVelocity();

        //    m_input.ManualFixedUpdate();

        //    m_rigidbody.SetRotation(Quaternion.FromToRotation(Vector2.up, Up));
        //    m_rigidbody.angularVelocity = 0f;

        //    m_currentVelocity += CurrentGravity * Time.fixedDeltaTime;

        //    m_rigidbody.velocity = m_currentVelocity;

        //    ResetState();
        //}

        private void OnJumpInput()
        {
            Vector2 jumpDirection;

            if (IsGrounded)
            {
                jumpDirection = m_currentGroundNormal;
            }
            else if (IsTouchingSteep)
            {
                jumpDirection = m_currentSteepNormal;
                m_jumps = 0;
            }
            else if (m_maxAirJumps > 0 && m_jumps <= m_maxAirJumps)
            {
                m_jumps = Mathf.Max(1, m_jumps);

                jumpDirection = m_currentGroundNormal;
            }
            else
            {
                return;
            }

            ++m_jumps;

            m_stepsSinceLastJump = 0;

            float jumpSpeed = Mathf.Sqrt(2f * GravityMagnitude * m_jumpHeight);

            // this line introduces an upward bias to jumps, which improves wall jumping
            jumpDirection = (jumpDirection + Up).normalized;

            // how much does the calculated jump direction line up with the current velocity?
            // (does not include gravity now, which might break this)
            float alignedSpeed = Vector3.Dot(m_currentVelocity, jumpDirection);

            if (alignedSpeed > 0f)
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);

            m_currentVelocity += jumpDirection * jumpSpeed * Time.fixedDeltaTime;
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
            float minDot = GetMinDot(collision.gameObject.layer);

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 normal = collision.GetContact(i).normal;
                float upDot = Vector3.Dot(Up, normal);

                if (upDot >= minDot)
                {
                    ++m_groundContactCount;
                    m_currentGroundNormal += normal;
                }
                else if (upDot > -0.01f)
                {
                    ++m_steepContactCount;
                    m_currentSteepNormal += normal;
                }
            }

            if (m_groundContactCount > 0)
                m_currentGroundNormal /= m_groundContactCount;

            if (m_steepContactCount > 0)
                m_currentSteepNormal /= m_steepContactCount;
        }

        /// <summary>
        /// Given a direction vector, project it so that it's aligned with whatever
        /// plane the character is standing on.
        /// </summary>
        private Vector2 ProjectDirectionOnPlane(Vector2 direction, Vector2 normal)
        {
            return (direction - normal * Vector2.Dot(direction, normal)).normalized;
        }

        private void AdjustVelocity()
        {
            // problem: while airborne, m_desiredVelocity.x goes to zero, so any horizontal movement is quickly removed
            // solution: prevent this velocity adjustment while the character is airborne and there is no input
            if (!IsGrounded && m_desiredVelocity.IsZero())
                return;

            Vector2 xAxis = ProjectDirectionOnPlane(Right, m_currentGroundNormal);
            float currentX = Vector2.Dot(m_currentVelocity, xAxis);

            float maxSpeedChange = (IsGrounded ? m_maxAcceleration : m_maxAirborneAcceleration) * Time.deltaTime;

            float newX = Mathf.MoveTowards(currentX, m_desiredVelocity.x, maxSpeedChange);

            m_currentVelocity += xAxis * (newX - currentX);
        }

        private bool SnapToGround()
        {
            //return false;

            if (m_stepsSinceLastGrounded > 1 || m_stepsSinceLastJump <= 2)
                return false;

            float speed = m_currentVelocity.magnitude;

            if (speed > m_maxSnapSpeed)
                return false;

            RaycastHit2D hit = Physics2D.Raycast(m_rigidbody.position, Down, m_groundProbeDistance, m_groundProbeLayerMask, -Mathf.Infinity);

            if (hit.collider == null)
                return false;

            float upDot = Vector2.Dot(Up, hit.normal);
            if (upDot < GetMinDot(hit.collider.gameObject.layer))
                return false;

            // we have just left the ground. set the velocity to snap us back down

            m_groundContactCount = 1;
            m_currentGroundNormal = hit.normal;

            float dot = Vector2.Dot(m_currentVelocity, hit.normal);

            // only do this when dot product is > 0, else it would slow us down not speed us up towards the ground
            if (dot > 0f)
                m_currentVelocity = (m_currentVelocity - m_currentGroundNormal * dot).normalized * speed;

            return true;
        }

        /// <summary>
        /// Get the appropriate minimum dot product for the given layer.
        /// </summary>
        float GetMinDot(int layer)
        {
            return ((m_stairsProbeLayerMask & (1 << layer)) == 0) ? m_minGroundDotProduct : m_minStairsDotProduct;
        }

        /// <summary>
        /// This method converts walls being touched into "virtual ground" in case th player ends up caught between walls with no ground below them.
        /// </summary>
        private bool CheckSteepContacts()
        {
            if (m_steepContactCount > 1 && Vector2.Dot(Up, m_currentSteepNormal) >= m_minGroundDotProduct)
            {
                m_groundContactCount = 1;
                m_currentGroundNormal = m_currentSteepNormal;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(m_currentVelocity.normalized * 2f));
        }
#endif
    }
}