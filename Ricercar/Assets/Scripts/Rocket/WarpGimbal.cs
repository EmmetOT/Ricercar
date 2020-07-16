using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Ricercar.Gravity;
using UnityEditor;

namespace Ricercar.Character
{
    /// <summary>
    /// This component will always try to get the attached rigidbody moving at a specific velocity.
    /// </summary>
    public class WarpGimbal : Gimbal
    {
        [SerializeField]
        private Rigidbody2D m_rigidbody;

        [SerializeField]
        private float m_targetSpeed = 5f;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_antigravity = 1f;

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("How many seconds will it take to reach max speed?")]
        private float m_accelerationTime = 1f;

        [SerializeField]
        private WarpComponents m_positiveWarp;

        [SerializeField]
        private WarpComponents m_negativeWarp;

        private WarpCalculationData m_positiveWarpData;
        private WarpCalculationData m_negativeWarpData;

        private void Start()
        {
            m_positiveWarpData = new WarpCalculationData(m_positiveWarp, m_rigidbody.position, m_targetSpeed, m_accelerationTime);
            m_negativeWarpData = new WarpCalculationData(m_negativeWarp, m_rigidbody.position, -m_targetSpeed, m_accelerationTime);
        }

        private void FixedUpdate()
        {
            if (!m_isActive)
                return;

            Vector2 gravity = m_accelerationTime * m_controller.CurrentGravityWithoutWarp;

            // acceleration should be applied if we're moving or based on the antigravity value
            float shouldApplyDeceleration = Mathf.Min(1f, m_desiredMovement.magnitude + m_antigravity);
            Vector2 deceleration = shouldApplyDeceleration * (m_rigidbody.velocity + gravity);

            Quaternion rotation = Quaternion.FromToRotation(Vector2.up, -gravity.normalized);
            Vector2 acceleration = rotation * (m_desiredMovement * m_targetSpeed);

            UpdateWarp(m_positiveWarpData, acceleration, 1f);
            UpdateWarp(m_negativeWarpData, deceleration, -1f);
        }

        private void UpdateWarp(WarpCalculationData data, Vector2 acceleration, float multiplier)
        {
            data.Components.Pivot.up = data.NormalizingRotation * acceleration.normalized;

            float mass = multiplier * acceleration.magnitude * data.NormalizedMass / m_accelerationTime;
            float absMass = Mathf.Abs(mass);

            data.Components.Attractor.gameObject.SetActive(absMass > 0f);

            data.Components.Attractor.SetMass(mass);

            float normalizedMass = Mathf.InverseLerp(0f, Mathf.Abs(data.MaxMass), absMass);
            Color colour = Color.Lerp(data.Components.MinColour, data.Components.MaxColour, normalizedMass);
            data.Components.SpriteRenderer.color = colour;
        }

        protected override void OnSpaceDown()
        {
            base.OnSpaceDown();

            SetActive(false);
        }

        protected override void OnSpaceUp()
        {
            base.OnSpaceDown();

            SetActive(true);
        }

        protected override void OnSetActive(bool active)
        {
            base.OnSetActive(active);

            m_positiveWarp.SetActive(active);
            m_negativeWarp.SetActive(active);
        }

        //#if UNITY_EDITOR
        //        private void OnDrawGizmos()
        //        {
        //            if (!EditorApplication.isPlaying || !enabled || !m_isActive)
        //                return;

        //            Utils.Label((Vector2)m_transform.position + Vector2.up * 1f, m_rigidbody.velocity.magnitude.ToString(), 13, Color.white);
        //            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.3f, m_negativeAttractor.Mass.ToString(), 13, Color.red);
        //            Utils.Label((Vector2)m_transform.position + Vector2.up * 1.6f, m_positiveAttractor.Mass.ToString(), 13, Color.blue);

        //            Gizmos.color = Color.red;
        //            Gizmos.DrawLine(m_transform.position, m_transform.position + (Vector3)m_controller.Attractor.CurrentGravity.normalized * 2f);
        //        }
        //#endif

        #region Structs

        [System.Serializable]
        private struct WarpComponents
        {
            public Attractor Attractor;
            public Transform Pivot;
            public SpriteRenderer SpriteRenderer;
            public Color MinColour;
            public Color MaxColour;

            public void SetActive(bool isActive)
            {
                Attractor.SetMass(0f);

                if (!isActive)
                    Attractor.gameObject.SetActive(false);
            }
        }

        private struct WarpCalculationData
        {
            public WarpComponents Components;
            public Vector2 GravityVector;
            public Vector2 GravityDirection;
            public float NormalizedMass;
            public float MaxMass;

            // these rotations ensure the warp attractors' centres of gravity
            // are always aligned with the gimbals up rotation
            public Quaternion NormalizingRotation;

            public WarpCalculationData(WarpComponents components, Vector2 position, float targetSpeed, float acceleratonTime)
            {
                Components = components;

                GravityVector = Components.Attractor.GetAttractionFromPosition(position, 1f);

                GravityDirection = GravityVector.normalized;

                // set the positive attractors mass such that it accelerates to the target speed in the target amount of time
                NormalizedMass = 1f / GravityVector.magnitude;

                MaxMass = targetSpeed * NormalizedMass / acceleratonTime;

                NormalizingRotation = Quaternion.FromToRotation(GravityDirection, Vector2.up);
            }
        }

        #endregion
    }
}