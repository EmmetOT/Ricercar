using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    /// <summary>
    /// A gravity grid stores a discrete grid of gravity samples. We use bilinear sampling
    /// to make the grid continuous. Grids can be tiled to make a continuous gravity field.
    /// </summary>
    public class GravityField : MonoBehaviour
    {
        private static readonly List<GravityField> m_allFields = new List<GravityField>();

        private static readonly List<Attractor> m_allAttractors = new List<Attractor>();

        private static readonly List<Attractor> m_staticAttractors = new List<Attractor>();

        public const float G = 667.4f;

        [SerializeField]
        [MinValue(1)]
        [UnityEngine.Serialization.FormerlySerializedAs("m_resolution")]
        private int m_gravityResolution = 2048;

        [SerializeField]
        [MinValue(1)]
        private int m_textureResolution = 128;

        [SerializeField]
        [MinValue(0f)]
        private float m_size = 1f;

        [SerializeField]
        [OnValueChanged("OnDisplayDataChanged")]
        private bool m_displayData = true;

        [SerializeField]
        private RawImage m_rawImage;

        [SerializeField]
        private GravityFieldTextureCreator m_textureCreator;

        [SerializeField]
        private GravityData m_data;

        #region Unity Callbacks

        private void OnEnable()
        {
            m_allFields.Add(this);
        }

        private void OnDisable()
        {
            m_allFields.Remove(this);
        }

        #endregion

        [Button]
        public void BakeAll()
        {
            FindAllAttractors();

            GravityField[] fields = FindObjectsOfType<GravityField>();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].BakeGravity();
                fields[i].GenerateTexture();
            }
        }

        [Button]
        public void Bake()
        {
            FindAllAttractors();

            BakeGravity();
            GenerateTexture();
        }

        [Button]
        public void FindAllAttractors()
        {
            Attractor[] attractors = FindObjectsOfType<Attractor>();

            m_allAttractors.Clear();
            m_staticAttractors.Clear();

            for (int i = 0; i < attractors.Length; i++)
            {
                AddAttractor(attractors[i]);
            }
        }

        [Button]
        public void ToggleDisplays()
        {
            bool display = !m_displayData;

            GravityField[] fields = FindObjectsOfType<GravityField>();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetDisplayData(display);
            }
        }

        public void BakeGravity()
        {
            if (m_data == null)
                m_data = GravityData.Create(this);

            m_data.Bake(transform.position, m_size, m_gravityResolution, m_staticAttractors.ToArray());
        }

        #region Static Methods

        /// <summary>
        /// Add an attractor to the gravity field system.
        /// </summary>
        public static void AddAttractor(Attractor attractor)
        {
            if (attractor.IsStatic && !m_staticAttractors.Contains(attractor))
                m_staticAttractors.Add(attractor);

            if (!m_allAttractors.Contains(attractor))
                m_allAttractors.Add(attractor);
        }

        /// <summary>
        /// Remove an attractor from the gravity field system. This won't do much
        /// if you remove a static attractor.
        /// </summary>
        public static void RemoveAttractor(Attractor attractor)
        {
            if (attractor.IsStatic)
                m_staticAttractors.Remove(attractor);

            m_allAttractors.Remove(attractor);
        }

        /// <summary>
        /// Gets the total gravity at the given position. Optionally can ignore given attractors.
        /// </summary>
        public static Vector2 GetGravity(Vector2 position, params Attractor[] ignore)
        {
            Vector2 gravityForce = Vector3.zero;

            gravityForce += GetStaticGravity(position);
            gravityForce += GetDynamicGravity(position, ignore);

            return gravityForce;
        }

        /// <summary>
        /// Get the gravity for the given attractor, both static and dynamic.
        /// </summary>
        public static Vector2 GetGravity(Attractor attractor)
        {
            return GetGravity(attractor.Position, attractor);
        }

        /// <summary>
        /// Returns only the "baked" gravity of objects which don't actively influence the gravity field.
        /// </summary>
        public static Vector2 GetStaticGravity(Vector2 worldPos)
        {
            for (int i = 0; i < m_allFields.Count; i++)
            {
                if (m_allFields[i].m_data.Contains(worldPos))
                {
                    return m_allFields[i].m_data.SampleGravityAt(worldPos);
                }
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Returns the dynamic gravity at the given position. Dynamic here means unbaked, it's more expensive. It's literally
        /// calculating the gravitational attraction to every other attractor!
        /// </summary>
        public static Vector2 GetDynamicGravity(Vector2 position, params Attractor[] ignore)
        {
            Vector2 result = Vector2.zero;

            for (int i = 0; i < m_allAttractors.Count; i++)
            {
                Attractor attractor = m_allAttractors[i];

                if (attractor.IsStatic)
                    continue;

                if (!attractor.AffectsFields)
                    continue;

                if (!ignore.IsNullOrEmpty())
                {
                    bool found = false;
                    for (int j = 0; j < ignore.Length; j++)
                    {
                        if (ignore[j] == attractor)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;
                }

                result += attractor.CalculateGravitationalForce(position);
            }

            return result;
        }

        #endregion

        #region Texture

        /// <summary>
        /// Create a square texture representing the static gravitational field, with the given size.
        /// </summary>
        private void GenerateTexture()
        {
            m_rawImage.texture = m_data.CreateTexture(m_textureCreator, m_textureResolution);


#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(m_rawImage.texture);
            EditorUtility.SetDirty(m_rawImage);
#endif
        }

        #endregion

        #region Editor Stuff

        public void SetDisplayData(bool isDisplaying)
        {
            m_displayData = isDisplaying;

            OnDisplayDataChanged();
        }

        private void OnDisplayDataChanged()
        {
            m_rawImage.gameObject.SetActive(m_displayData);
        }

        #endregion
    }
}