using Boo.Lang.Runtime.DynamicDispatching;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ricercar.Gravity
{
    public class GravityField : MonoBehaviour
    {
        [System.Serializable]
        private struct AtomicVector2
        {
            public const int Factor = 1000;
            public const int Stride = 8;

            [SerializeField]
            private int m_x;
            public int X => m_x;

            [SerializeField]
            private int m_y;
            public int Y => m_y;

            public AtomicVector2(int x, int y)
            {
                m_x = x;
                m_y = y;
            }

            public AtomicVector2(Vector2 vec) : this((int)(vec.x * Factor), (int)(vec.y * Factor)) { }

            public static implicit operator Vector2(AtomicVector2 atomicVector2)
            {
                return new Vector2((float)atomicVector2.m_x / Factor, (float)atomicVector2.m_y / Factor);
            }

            public static implicit operator AtomicVector2(Vector2 vector2)
            {
                return new AtomicVector2(vector2);
            }

            public override string ToString()
            {
                return $"({m_x}, {m_y})";
            }
        }

        public const float G = 667.4f;

        private const float MIN_MOVEMENT_SQR_MAGNITUDE = 0.1f;

        private List<IAttractor> m_attractors = new List<IAttractor>();
        private readonly List<GravityVisualizer> m_visualizers = new List<GravityVisualizer>();

        [SerializeField]
        private ComputeShader m_gravityFieldComputeShader;

        [SerializeField]
        [OnValueChanged("OnParallelModeChanged")]
        [Tooltip("Decide whether each pairwise point attraction is calculated in a different thread, or in a single series for loop. May produce different results.")]
        private bool m_parallelMode = false;

        private int m_computePointForcesKernel = -1;

        private Vector2[] m_attractorOutputData;
        private ComputeBuffer m_pointForcesOutputBuffer;

        private readonly List<AttractorData> m_attractorInputData = new List<AttractorData>();
        private ComputeBuffer m_pointInputBuffer;

        private ComputeBuffer m_parallelOutputBuffer;
        private AtomicVector2[] m_parallelOutputData;

        private int m_attractorCount = 0;

        private void Start()
        {
            m_computePointForcesKernel = m_gravityFieldComputeShader.FindKernel(m_parallelMode ? "ComputePointForcesParallel" : "ComputePointForcesSeries");
            RefreshComputeBuffers();
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        public void ReleaseBuffers()
        {
            m_pointForcesOutputBuffer?.Release();
            m_pointInputBuffer?.Release();
            m_parallelOutputBuffer?.Release();
        }

        public void RegisterAttractor(IAttractor attractor)
        {
            if (m_attractors.Contains(attractor))
                return;

            m_attractors.Add(attractor);

            RefreshComputeBuffers();
        }

        public void DeregisterAttractor(IAttractor attractor)
        {
            if (m_attractors.Remove(attractor))
                RefreshComputeBuffers();
        }

        public void RegisterVisualizer(GravityVisualizer visualizer)
        {
            if (m_visualizers.Contains(visualizer))
                return;

            Debug.Log("Registering " + visualizer.name, visualizer);

            m_visualizers.Add(visualizer);

            visualizer.SetInputData(m_pointInputBuffer);
        }

        public void DeregisterVisualizer(GravityVisualizer visualizer)
        {
            if (m_visualizers.Remove(visualizer))
                RefreshComputeBuffers();
        }

        /// <summary>
        /// Not recommended for normal use. Forces this class to generate a buffer full of point attractor data. This could be used to generate 
        /// static textures outside of runtime.
        /// </summary>
        public ComputeBuffer ForceGeneratePointInputBuffer()
        {
            m_attractors = new List<IAttractor>(FindObjectsOfType<PointAttractor>());
            m_computePointForcesKernel = m_gravityFieldComputeShader.FindKernel(m_parallelMode ? "ComputePointForcesParallel" : "ComputePointForcesSeries");
            RefreshComputeBuffers();
            ComputePointForces();
            return m_pointInputBuffer;
        }

        private void RefreshComputeBuffers()
        {
            // prevent this class from doing anything until after start
            if (m_computePointForcesKernel < 0)
                return;

            m_attractorCount = m_attractors.Count;

            ReleaseBuffers();

            if (m_attractorCount == 0)
                return;

            m_pointForcesOutputBuffer = new ComputeBuffer(m_attractorCount, sizeof(float) * 2);
            m_pointInputBuffer = new ComputeBuffer(m_attractorCount, AttractorData.Stride);

            if (m_parallelMode)
            {
                m_parallelOutputBuffer = new ComputeBuffer(m_attractorCount, AtomicVector2.Stride);
                m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "AtomicForcesOutputBuffer", m_parallelOutputBuffer);
            }

            m_attractorOutputData = new Vector2[m_attractorCount];
            m_parallelOutputData = new AtomicVector2[m_attractorCount];

            // send the data to the kernel of the compute shader meant for the gravity force between point attractors
            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointAttractors", m_pointInputBuffer);
            m_gravityFieldComputeShader.SetBuffer(m_computePointForcesKernel, "PointForces", m_pointForcesOutputBuffer);
            m_gravityFieldComputeShader.SetInt("PointCount", m_attractorCount);

            for (int i = 0; i < m_visualizers.Count; i++)
            {
                if (m_visualizers[i] != null)
                    m_visualizers[i].SetInputData(m_pointInputBuffer);
            }
        }

        private void ComputePointForces()
        {
            if (m_attractors.IsNullOrEmpty())
                return;

            m_attractorInputData.Clear();

            for (int i = 0; i < m_attractorCount; i++)
                m_attractorInputData.Add(new AttractorData(m_attractors[i]));

            m_pointInputBuffer.SetData(m_attractorInputData);

            if (m_parallelMode)
            {
                m_parallelOutputBuffer.SetData(m_parallelOutputData);
                m_gravityFieldComputeShader.Dispatch(m_computePointForcesKernel, m_attractorCount, m_attractorCount, 1);
                m_parallelOutputBuffer.GetData(m_parallelOutputData);

                for (int i = 0; i < m_parallelOutputData.Length; i++)
                {
                    m_attractorOutputData[i] = m_parallelOutputData[i];
                    m_parallelOutputData[i] = new AtomicVector2(0, 0);
                }
            }
            else
            {
                m_gravityFieldComputeShader.Dispatch(m_computePointForcesKernel, m_attractorCount, 1, 1);
                m_pointForcesOutputBuffer.GetData(m_attractorOutputData);
            }
        }

        private void ApplyPointForces()
        {
            for (int i = 0; i < m_attractorCount; i++)
            {
                if (!m_attractorOutputData[i].IsNaN()
                    && m_attractorOutputData[i].sqrMagnitude > MIN_MOVEMENT_SQR_MAGNITUDE)
                {
                    m_attractors[i].SetGravity(m_attractorOutputData[i]);
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_computePointForcesKernel < 0)
                return;

            ComputePointForces();
            ApplyPointForces();
        }

        private void OnParallelModeChanged()
        {
            m_computePointForcesKernel = m_gravityFieldComputeShader.FindKernel(m_parallelMode ? "ComputePointForcesParallel" : "ComputePointForcesSeries");
            RefreshComputeBuffers();
        }
    }
}
