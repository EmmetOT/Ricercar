using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.CompilerServices;

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
        private const int THREAD_GROUP_SQRT = 16;

        private static readonly List<GravityField> m_allFields = new List<GravityField>();

        private static readonly List<Attractor> m_allAttractors = new List<Attractor>();

        //private static readonly List<Attractor> m_staticAttractors = new List<Attractor>();

        public const float G = 667.4f;

        [SerializeField]
        private ComputeShader m_gravityFieldComputeShader;

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
        private Image m_image;

        [SerializeField]
        private GravityFieldTextureCreator m_textureCreator;

        [SerializeField]
        private GravityData m_data;

        private RenderTexture m_renderTexture;

        #region Unity Callbacks

        private void Start()
        {
            m_renderTexture = new RenderTexture(m_textureResolution, m_textureResolution, 0);
            m_rawImage.texture = m_renderTexture;
        }

        private void OnEnable()
        {
            m_fieldOutputBuffer = new ComputeBuffer(THREAD_GROUP_SQRT * THREAD_GROUP_SQRT * THREAD_GROUP_SQRT * THREAD_GROUP_SQRT, 8);

            m_computeFullFieldKernel = m_gravityFieldComputeShader.FindKernel("ComputeFullField");
            m_computePointForcesKernel = m_gravityFieldComputeShader.FindKernel("ComputePointForces");

            m_forcesOutputBuffer = new ComputeBuffer(2, 8);

            m_allFields.Add(this);

            FindAllAttractors();
        }

        private void OnDisable()
        {
            if (m_pointInputBuffer != null)
                m_pointInputBuffer.Dispose();

            if (m_forcesOutputBuffer != null)
                m_forcesOutputBuffer.Dispose();

            if (m_ignorePointsInputBuffer != null)
                m_ignorePointsInputBuffer.Dispose();

            m_fieldOutputBuffer.Dispose();

            m_allFields.Remove(this);
        }

        //Vector2[] resultingGravityField = new Vector2[16 * 16 * 16 * 16];

        private readonly List<Vector3> m_staticPoints = new List<Vector3>();

        ComputeBuffer m_pointInputBuffer;
        ComputeBuffer m_ignorePointsInputBuffer;
        ComputeBuffer m_fieldOutputBuffer;
        ComputeBuffer m_forcesOutputBuffer;
        int m_computeFullFieldKernel;
        int m_computePointForcesKernel;

        private Vector2[] m_pointForces = new Vector2[2];
        private int[] m_ignorePoints = new int[2];

        private void Update()
        {
            CalculateFieldFromComputeShader(m_allAttractors);
            m_data.BlitInto(m_fieldOutputBuffer, m_textureCreator, m_renderTexture);

        }

        private void FixedUpdate()
        {
            if (m_pointForces == null)
            {
                Debug.Log("Point forces is null!");
                return;
            }

            if (m_pointInputBuffer == null)
                return;

            // send the data to the kernel of the compute shader meant for the gravity force between point attractors
            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointAttractors", m_pointInputBuffer);
            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "IgnorePoints", m_ignorePointsInputBuffer);
            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointForces", m_forcesOutputBuffer);

            m_gravityFieldComputeShader.Dispatch(m_computePointForcesKernel, m_allAttractors.Count, 1, 1);
            m_forcesOutputBuffer.GetData(m_pointForces);

            for (int i = 0; i < m_pointForces.Length; i++)
            {
                m_allAttractors[i].AddForce(m_pointForces[i]);
            }
        }

        #endregion

        private void FillAttractorBuffers(IList<Attractor> attractors)
        {
            if (m_pointInputBuffer == null || attractors.Count != m_staticPoints.Count)
            {
                if (m_pointInputBuffer != null)
                    m_pointInputBuffer.Dispose();

                m_pointInputBuffer = new ComputeBuffer(attractors.Count, 12);
            }

            if (m_forcesOutputBuffer == null || m_ignorePointsInputBuffer == null || attractors.Count != m_staticPoints.Count)
            {
                if (m_forcesOutputBuffer != null)
                    m_forcesOutputBuffer.Dispose(); 
                
                if (m_ignorePointsInputBuffer != null)
                    m_ignorePointsInputBuffer.Dispose();

                m_pointForces = new Vector2[attractors.Count];
                m_ignorePoints = new int[attractors.Count];

                m_forcesOutputBuffer = new ComputeBuffer(attractors.Count, 8);
                m_ignorePointsInputBuffer = new ComputeBuffer(attractors.Count, 4);

                for (int i = 0; i < m_ignorePoints.Length; i++)
                {
                    m_ignorePoints[i] = attractors[i].AffectsFields ? 0 : 1;
                }
            }

            m_staticPoints.Clear();

            for (int i = 0; i < attractors.Count; i++)
            {
                PointAttractor attractor = attractors[i] as PointAttractor;

                if (attractor == null)
                    continue;

                m_staticPoints.Add(new Vector3(attractor.Position.x, attractor.Position.y, attractor.Mass));
            }

            m_pointInputBuffer.SetData(m_staticPoints);
            m_ignorePointsInputBuffer.SetData(m_ignorePoints);
        }

        public void CalculateFieldFromComputeShader(IList<Attractor> attractors)
        {
            FillAttractorBuffers(attractors);

            // send the data to the kernel of the compute shader meant for calculating the gravity field of points
            m_gravityFieldComputeShader.SetBuffer(m_computeFullFieldKernel, "PointAttractors", m_pointInputBuffer);
            m_gravityFieldComputeShader.SetBuffer(m_computeFullFieldKernel, "GravityField", m_fieldOutputBuffer);
            m_gravityFieldComputeShader.SetBuffer(m_computeFullFieldKernel, "IgnorePoints", m_ignorePointsInputBuffer);

            m_gravityFieldComputeShader.SetInt("PointCount", m_staticPoints.Count);
            m_gravityFieldComputeShader.SetVector("BottomLeft", m_data.GetBottomLeft());
            m_gravityFieldComputeShader.SetVector("TopRight", m_data.GetTopRight());

            m_gravityFieldComputeShader.Dispatch(m_computeFullFieldKernel, THREAD_GROUP_SQRT, THREAD_GROUP_SQRT, 1);
        }

        [Button]
        public void UpdateTexture()
        {
            m_fieldOutputBuffer = new ComputeBuffer(THREAD_GROUP_SQRT * THREAD_GROUP_SQRT * THREAD_GROUP_SQRT * THREAD_GROUP_SQRT, 8);

            CalculateFieldFromComputeShader(FindObjectsOfType<PointAttractor>());

            m_rawImage.texture = m_data.GenerateTexture2D(m_fieldOutputBuffer, m_textureCreator);

            m_pointInputBuffer.Dispose();
            m_ignorePointsInputBuffer.Dispose();
            m_fieldOutputBuffer.Dispose();

#if UNITY_EDITOR
            EditorUtility.SetDirty(m_rawImage);
#endif
        }

        [Button]
        public void UpdateAll()
        {
            GravityField[] fields = FindObjectsOfType<GravityField>();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].UpdateTexture();
            }
        }

        [Button]
        public void FindAllAttractors()
        {
            Attractor[] attractors = FindObjectsOfType<Attractor>();

            m_allAttractors.Clear();
            //m_staticAttractors.Clear();

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

            m_data.Bake(transform.position, m_size, m_gravityResolution, m_allAttractors.ToArray());
        }

        #region Static Methods

        /// <summary>
        /// Add an attractor to the gravity field system.
        /// </summary>
        public static void AddAttractor(Attractor attractor)
        {
            if (!m_allAttractors.Contains(attractor))
                m_allAttractors.Add(attractor);
        }

        /// <summary>
        /// Remove an attractor from the gravity field system. This won't do much
        /// if you remove a static attractor.
        /// </summary>
        public static void RemoveAttractor(Attractor attractor)
        {
            m_allAttractors.Remove(attractor);
        }

        /// <summary>
        /// Gets the total gravity at the given position. Optionally can ignore given attractors.
        /// </summary>
        public static Vector2 GetGravity(Vector2 position, params Attractor[] ignore)
        {
            Vector2 gravityForce = Vector3.zero;

            ///gravityForce += GetStaticGravity(position);
            //gravityForce += GetDynamicGravity(position, ignore);

            return gravityForce;
        }

        /// <summary>
        /// Get the gravity for the given attractor, both static and dynamic.
        /// </summary>
        public static Vector2 GetGravity(Attractor attractor)
        {
            return GetGravity(attractor.Position, attractor);
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