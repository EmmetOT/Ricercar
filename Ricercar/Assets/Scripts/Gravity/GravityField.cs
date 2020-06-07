﻿using NaughtyAttributes;
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

        [SerializeField]
        private ComputeShader m_gravityFieldComputeShader;

        private int m_computePointForcesKernel = -1;

        private Vector2[] m_attractorOutputData;
        private ComputeBuffer m_forcesOutputBuffer;

        private readonly List<AttractorData> m_pointInputData = new List<AttractorData>();
        private readonly List<BakedAttractorData> m_bakedInputData = new List<BakedAttractorData>();

        private Texture2DArray m_bakedAttractorTextureArray;
        private readonly List<Texture2D> m_bakedAttractorTextureList = new List<Texture2D>();
        private readonly Dictionary<int, int> m_bakedAttractorTextureGUIDToIndex = new Dictionary<int, int>();
        private Texture2DArray m_emptyTextureArray;

        private ComputeBuffer m_pointInputBuffer;
        private ComputeBuffer m_bakedInputBuffer;

        private int m_attractorCount = 0;
        private int m_bakedAttractorCount = 0;

        private void Start()
        {
            // exists solely to stop a complaint if we pass an empty one in
            m_emptyTextureArray = new Texture2DArray(GravityMap.SIZE, GravityMap.SIZE, 1, GRAPHICS_FORMAT, UnityEngine.Experimental.Rendering.TextureCreationFlags.None)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            m_emptyTextureArray.Apply();

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
                BuildBakedAttractorTextureArray();
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
                {
                    BuildBakedAttractorTextureArray();
                    RefreshComputeBuffers();
                }
            }
            else
            {
                if (m_attractors.Remove(attractor))
                    RefreshComputeBuffers();
            }
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

            m_forcesOutputBuffer = new ComputeBuffer(Mathf.Max(1, m_attractorCount + m_bakedAttractorCount), sizeof(float) * 2);
            m_pointInputBuffer = new ComputeBuffer(Mathf.Max(1, m_attractorCount), AttractorData.Stride);
            m_bakedInputBuffer = new ComputeBuffer(Mathf.Max(1, m_bakedAttractorCount), BakedAttractorData.Stride);

            m_attractorOutputData = new Vector2[m_attractorCount + m_bakedAttractorCount];

            // set global data (field and visualizers)

            Debug.Log("Setting buffers.");

            Shader.SetGlobalBuffer("PointAttractors", m_pointInputBuffer);
            Shader.SetGlobalBuffer("BakedAttractors", m_bakedInputBuffer);

            Shader.SetGlobalInt("PointCount", m_attractorCount);
            Shader.SetGlobalInt("BakedCount", m_bakedAttractorCount);

            // set field only data

            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointForces", m_forcesOutputBuffer);
        }

        /// <summary>
        /// To be called whenever a baked attractor is added or removed. First, generates a lookup from baked attractor
        /// gravity map's guid to the index of its texture in a texture list. This system is in place to prevent
        /// redundant texture information from being sent to the GPU.
        /// 
        /// Next, creates a texture array from only these unique textures.
        /// </summary>
        private void BuildBakedAttractorTextureArray()
        {
            // first, generate the lookup

            m_bakedAttractorTextureGUIDToIndex.Clear();
            m_bakedAttractorTextureList.Clear();

            for (int i = 0; i < m_bakedAttractors.Count; i++)
            {
                if (!m_bakedAttractorTextureGUIDToIndex.ContainsKey(m_bakedAttractors[i].GravityMap.GUID))
                {
                    m_bakedAttractorTextureGUIDToIndex.Add(m_bakedAttractors[i].GravityMap.GUID, m_bakedAttractorTextureList.Count);
                    m_bakedAttractorTextureList.Add(m_bakedAttractors[i].GravityMap.Texture);
                }
            }

            if (m_bakedAttractorTextureList.Count > 0)
            {
                m_bakedAttractorTextureArray = Utils.CreateTextureArray(m_bakedAttractorTextureList, GravityMap.SIZE, GravityMap.SIZE, GRAPHICS_FORMAT);
            }
            else
            {
                m_bakedAttractorTextureArray = m_emptyTextureArray;
            }

            Shader.SetGlobalTexture("BakedAttractorTextures", m_bakedAttractorTextureArray);
        }

        private void SetAttractorData()
        {
            if (m_attractors.IsNullOrEmpty() && m_bakedAttractors.IsNullOrEmpty())
                return;

            m_pointInputData.Clear();
            m_bakedInputData.Clear();

            for (int i = 0; i < m_attractorCount; i++)
                m_pointInputData.Add(new AttractorData(m_attractors[i]));

            for (int i = 0; i < m_bakedAttractorCount; i++)
            {
                BakedAttractorData data = new BakedAttractorData(m_bakedAttractors[i]);

                if (m_bakedAttractorTextureGUIDToIndex.TryGetValue(m_bakedAttractors[i].GravityMap.GUID, out int index))
                    data.SetTextureIndex(index);

                m_bakedInputData.Add(data);
            }

            m_pointInputBuffer.SetData(m_pointInputData);
            m_bakedInputBuffer.SetData(m_bakedInputData);
        }

        private void ComputePointForces()
        {
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

            SetAttractorData();
        }

        private void FixedUpdate()
        {
            if (m_computePointForcesKernel < 0)
                return;

            ComputePointForces();
            ApplyPointForces();
        }
    }
}
