using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ricercar.Gravity
{
    public class GravityField : MonoBehaviour
    {
        public const float G = 667.4f;

        public const UnityEngine.Experimental.Rendering.GraphicsFormat GRAPHICS_FORMAT = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;

        private readonly List<IAttractor> m_attractors = new List<IAttractor>();
        private readonly List<IBakedAttractor> m_bakedAttractors = new List<IBakedAttractor>();
        private readonly List<GravityVisualizer> m_visualizers = new List<GravityVisualizer>();

        [SerializeField]
        private ComputeShader m_gravityFieldComputeShader;

        private int m_computePointForcesKernel = -1;

        private Vector2[] m_attractorOutputData;
        private ComputeBuffer m_forcesOutputBuffer;

        private readonly List<AttractorData> m_pointInputData = new List<AttractorData>();
        private readonly List<BakedAttractorData> m_bakedInputData = new List<BakedAttractorData>();

        private Texture2DArray m_bakedAttractorTextureArray;
        private readonly List<Texture2D> m_bakedAttractorTextureList = new List<Texture2D>();

        private ComputeBuffer m_pointInputBuffer;
        private ComputeBuffer m_bakedInputBuffer;

        private int m_attractorCount = 0;
        private int m_bakedAttractorCount = 0;

        private void Start()
        {
            m_computePointForcesKernel = m_gravityFieldComputeShader.FindKernel("ComputeForces");
            RefreshComputeBuffers();
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        public void ReleaseBuffers()
        {
            m_forcesOutputBuffer?.Release();
            m_pointInputBuffer?.Release();
            m_bakedInputBuffer?.Release();
        }

        public void RegisterAttractor(IAttractor attractor)
        {
            if (attractor is IBakedAttractor bakedAttractor)
            {
                if (m_bakedAttractors.Contains(bakedAttractor))
                    return;

                m_bakedAttractors.Add(bakedAttractor);
            }
            else
            {
                if (m_attractors.Contains(attractor))
                    return;

                m_attractors.Add(attractor);
            }

            RefreshComputeBuffers();
        }

        public void DeregisterAttractor(IAttractor attractor)
        {
            if (attractor is IBakedAttractor bakedAttractor)
            {
                if (m_bakedAttractors.Remove(bakedAttractor))
                    RefreshComputeBuffers();
            }
            else
            {
                if (m_attractors.Remove(attractor))
                    RefreshComputeBuffers();
            }
        }

        public void RegisterVisualizer(GravityVisualizer visualizer)
        {
            if (m_visualizers.Contains(visualizer))
                return;

            m_visualizers.Add(visualizer);
            RefreshComputeBuffers();
        }

        public void DeregisterVisualizer(GravityVisualizer visualizer)
        {
            if (m_visualizers.Remove(visualizer))
                RefreshComputeBuffers();
        }

        private void RefreshComputeBuffers()
        {
            // prevent this class from doing anything until after start
            if (m_computePointForcesKernel < 0)
                return;

            m_attractorCount = m_attractors.Count;
            m_bakedAttractorCount = m_bakedAttractors.Count;

            ReleaseBuffers();

            if (m_attractorCount == 0 && m_bakedAttractorCount == 0)
                return;

            m_bakedAttractorTextureList.Clear();

            if (m_bakedAttractorCount > 0)
            {
                m_bakedAttractorTextureArray = new
                    Texture2DArray(GravityMap.SIZE, GravityMap.SIZE, m_bakedAttractorCount, GRAPHICS_FORMAT, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                for (int i = 0; i < m_bakedAttractorCount; i++)
                {
                    Debug.Log("Getting texture " + i + " from " + m_bakedAttractors[i].Texture.name);
                    m_bakedAttractorTextureArray.SetPixels(m_bakedAttractors[i].Texture.GetPixels(), i, 0);
                }
            }
            else
            {
                // exists solely to stop a complaint if we pass an empty one in. todo: cache this
                m_bakedAttractorTextureArray = new Texture2DArray(GravityMap.SIZE, GravityMap.SIZE, 1, GRAPHICS_FORMAT, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            m_bakedAttractorTextureArray.Apply();

            m_forcesOutputBuffer = new ComputeBuffer(Mathf.Max(1, m_attractorCount + m_bakedAttractorCount), sizeof(float) * 2);
            m_pointInputBuffer = new ComputeBuffer(Mathf.Max(1, m_attractorCount), AttractorData.Stride);
            m_bakedInputBuffer = new ComputeBuffer(Mathf.Max(1, m_bakedAttractorCount), BakedAttractorData.Stride);

            m_attractorOutputData = new Vector2[m_attractorCount + m_bakedAttractorCount];

            // set global data (field and visualizers)

            Debug.Log("Setting buffers.");

            Shader.SetGlobalBuffer("PointAttractors", m_pointInputBuffer);
            Shader.SetGlobalBuffer("BakedAttractors", m_bakedInputBuffer);
            Shader.SetGlobalTexture("BakedAttractorTextures", m_bakedAttractorTextureArray);

            m_gravityFieldComputeShader.SetInt("PointCount", m_attractorCount);
            m_gravityFieldComputeShader.SetInt("BakedCount", m_bakedAttractorCount);

            // set field only data

            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointForces", m_forcesOutputBuffer);
        }

        private void ComputePointForces()
        {
            if (m_attractors.IsNullOrEmpty() && m_bakedAttractors.IsNullOrEmpty())
                return;

            m_pointInputData.Clear();
            m_bakedInputData.Clear();

            for (int i = 0; i < m_attractorCount; i++)
                m_pointInputData.Add(new AttractorData(m_attractors[i]));

            for (int i = 0; i < m_bakedAttractorCount; i++)
                m_bakedInputData.Add(new BakedAttractorData(m_bakedAttractors[i]));

            m_pointInputBuffer.SetData(m_pointInputData);
            m_bakedInputBuffer.SetData(m_bakedInputData);

            m_gravityFieldComputeShader.Dispatch(m_computePointForcesKernel, m_attractorCount, 1, 1);
            m_forcesOutputBuffer.GetData(m_attractorOutputData);
        }

        private void ApplyPointForces()
        {
            for (int i = 0; i < m_attractorCount; i++)
            {
                Vector2 data = m_attractorOutputData[i];

                if (!data.IsNaN())
                {
                    m_attractors[i].SetGravity(data);
                }
            }

            for (int i = 0; i < m_bakedAttractorCount; i++)
            {
                Vector2 data = m_attractorOutputData[i + m_attractorCount];

                if (!data.IsNaN())
                {
                    m_bakedAttractors[i].SetGravity(data);
                }
            }
        }

        private void Update()
        {
            if (m_computePointForcesKernel < 0)
                return;

            ComputePointForces();
        }

        private void FixedUpdate()
        {
            if (m_computePointForcesKernel < 0)
                return;

            ApplyPointForces();
        }


        public static RenderTexture CreateTempRenderTexture(int width, int height, Color? col = null, UnityEngine.Experimental.Rendering.GraphicsFormat? format = null)
        {
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 24);
            texture.enableRandomWrite = true;

            if (format != null)
                texture.graphicsFormat = format.Value;

            texture.Create();

            if (col != null)
                texture.FillWithColour(col.Value);

            return texture;
        }

    }
}
