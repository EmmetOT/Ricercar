using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class BakedAttractor : MonoBehaviour, IBakedAttractor
    {
        [SerializeField]
        private GravityMap m_gravityMap;

        [SerializeField]
        protected GravityField m_gravityField;

        [SerializeField]
        private bool m_applyForceToSelf = true;
        public bool ApplyForceToSelf => m_applyForceToSelf;

        [SerializeField]
        private bool m_affectsField = true;
        public bool AffectsField => m_affectsField;

        [SerializeField]
        [OnValueChanged("OnScaleChanged")]
        [MinValue(0.01f)]
        private float m_scale = 1f;
        public float Scale => m_scale;

        [SerializeField]
        private Vector2 m_startingForce;

        [SerializeField]
        private Rigidbody2D m_rigidbody;
        public Rigidbody2D Rigidbody => m_rigidbody;

        [SerializeField]
        private bool m_useRigidbodyMass;

        [SerializeField]
        [HideIf("m_useRigidbodyMass")]
        private float m_mass;

        [SerializeField]
        private ExtrapolationSource m_extrapolationSource = ExtrapolationSource.CENTRE_OF_GRAVITY;
        public ExtrapolationSource ExtrapolationSource => m_extrapolationSource;

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        public float Mass => m_useRigidbodyMass ? m_rigidbody.mass : m_mass;
        public Vector2 Position => m_transform.position;
        public Vector2 Velocity => m_rigidbody.velocity;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_currentGravity;
        public Vector2 CurrentGravity => m_currentGravity;

        public Texture2D Texture => m_gravityMap.Texture;
        public Vector2 CentreOfGravity => transform.TransformPoint(m_gravityMap.TextureSpaceToWorldSpace(m_gravityMap.CentreOfGravity));
        public float Rotation => m_transform.eulerAngles.z;
        public float Size => m_gravityMap.Size;

        public void SetGravity(Vector2 gravity)
        {
            if (!m_applyForceToSelf)
                return;

            m_currentGravity = gravity;

            if (m_rigidbody != null)
                m_rigidbody.AddForce(m_currentGravity * Time.fixedDeltaTime);
        }

        private void Reset()
        {
            m_transform = transform;
            m_rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            m_transform = transform;
            m_gravityField.RegisterAttractor(this);
        }

        private void OnDisable()
        {
            m_gravityField.DeregisterAttractor(this);
        }

        private void OnScaleChanged()
        {
            m_transform.localScale = Vector3.one * m_scale;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 translate = transform.localToWorldMatrix.ExtractTranslation();
            Quaternion rotation = transform.localToWorldMatrix.ExtractRotation();

            Matrix4x4 matrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(translate, rotation, Vector3.one * Scale);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_gravityMap.LocalBottomLeftCorner, m_gravityMap.LocalBottomRightCorner);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_gravityMap.LocalBottomLeftCorner, m_gravityMap.LocalTopLeftCorner);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_gravityMap.LocalTopLeftCorner, m_gravityMap.LocalTopRightCorner);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_gravityMap.LocalTopRightCorner, m_gravityMap.LocalBottomRightCorner);

            Gizmos.matrix = Matrix4x4.TRS(translate, rotation, Vector3.one);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(m_gravityMap.TextureSpaceToWorldSpace(m_gravityMap.CentreOfGravity), 5f);

            Gizmos.matrix = matrix;
        }
#endif
    }

    public enum ExtrapolationSource
    {
        POSITION,
        CENTRE_OF_GRAVITY
    }

    public interface IBakedAttractor : IAttractor
    {
        Texture2D Texture { get; }
        Vector2 CentreOfGravity { get; }
        float Rotation { get; }
        float Size { get; }
        float Scale { get; }
        ExtrapolationSource ExtrapolationSource { get; }
    }
}