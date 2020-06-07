using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Ricercar.Gravity
{
    public class GravityRasterizer : EditorWindow
    {
        private const int MAX_INPUT_SIZE = 256;

        private const string COMPUTE_SHADER_PATH = "Shaders/GravityRasterizerShader";

        private const string MASS_DISTRIBUTION_KERNEL = "CalculateMassDistribution";
        private const string MASS_DISTRIBUTION_INPUT_TEXTURE = "ImageInput";
        private const string MASS_DISTRIBUTION_OUTPUT_BUFFER = "MassDistributionOutput";
        private const string OCCUPIED_TEXEL_APPEND_BUFFER = "OccupiedTexelsAppendBuffer";
        private const string OCCUPIED_TEXEL_STRUCTURED_BUFFER = "OccupiedTexelsStructuredBuffer";
        private const string OCCUPIED_TEXEL_COUNT_BUFFER = "OccupiedTexelsCount";

        private const string GENERATE_GRAVITY_MAP_KERNEL = "GenerateGravityMap";
        private const string GRAVITY_MAP_OUTPUT_TEXTURE = "GravityMapOutput";
        private const string INPUT_TEXTURE_WIDTH = "InputWidth";
        private const string INPUT_TEXTURE_HEIGHT = "InputHeight";

        private const string CLEAR_OUTPUT_BUFFER_KERNEL = "ClearOutputBuffer";

        private const float MULTIPLICATION_FACTOR = 10000f;

        private static Texture2D m_inputTexture;
        private static ComputeShader m_computeShader;

        private static Texture2D m_outputFinal;
        private static Vector2 m_centreOfGravity;

        private static int m_massDistributionKernel;
        private static int m_generateGravityMapKernel;
        private static int m_clearOutputBufferKernel;

        private static ComputeBuffer m_outputBuffer;
        private static ComputeBuffer m_occupiedTexelsAppendBuffer;
        private static ComputeBuffer m_occupiedTexelsCountBuffer;

        private static readonly int[] m_outputData = new int[3];

        [MenuItem("Tools/Gravity Rasterizer")]
        public static void ShowWindow()
        {
            ReleaseBuffers();
            Initialize();

            GetWindow(typeof(GravityRasterizer));
        }

        private static void Initialize()
        {
            m_centreOfGravity = default;
            m_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_PATH);

            m_massDistributionKernel = m_computeShader.FindKernel(MASS_DISTRIBUTION_KERNEL);
            m_generateGravityMapKernel = m_computeShader.FindKernel(GENERATE_GRAVITY_MAP_KERNEL);
            m_clearOutputBufferKernel = m_computeShader.FindKernel(CLEAR_OUTPUT_BUFFER_KERNEL);

            m_outputBuffer = new ComputeBuffer(3, sizeof(int));
            m_computeShader.SetBuffer(m_massDistributionKernel, MASS_DISTRIBUTION_OUTPUT_BUFFER, m_outputBuffer);
            m_computeShader.SetBuffer(m_generateGravityMapKernel, MASS_DISTRIBUTION_OUTPUT_BUFFER, m_outputBuffer);
            m_computeShader.SetBuffer(m_clearOutputBufferKernel, MASS_DISTRIBUTION_OUTPUT_BUFFER, m_outputBuffer);

            m_occupiedTexelsAppendBuffer = new ComputeBuffer(MAX_INPUT_SIZE * MAX_INPUT_SIZE, sizeof(float) * 2, ComputeBufferType.Append);

            // setting the same buffer as an append buffer in one kernel
            // and a structured buffer in another
            m_computeShader.SetBuffer(m_massDistributionKernel, OCCUPIED_TEXEL_APPEND_BUFFER, m_occupiedTexelsAppendBuffer);
            m_computeShader.SetBuffer(m_generateGravityMapKernel, OCCUPIED_TEXEL_STRUCTURED_BUFFER, m_occupiedTexelsAppendBuffer);

            m_occupiedTexelsCountBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_computeShader.SetBuffer(m_generateGravityMapKernel, OCCUPIED_TEXEL_COUNT_BUFFER, m_occupiedTexelsCountBuffer);
        }

        private static void ReleaseBuffers()
        {
            m_outputBuffer?.Release();
            m_occupiedTexelsAppendBuffer?.Release();
            m_occupiedTexelsCountBuffer?.Release();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        public void OnGUI()
        {
            m_inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture", m_inputTexture, typeof(Texture2D), false);

            GUI.enabled = m_inputTexture != null;
            if (GUILayout.Button("Generate Gravity Texture"))
            {
                if (m_computeShader == null)
                {
                    m_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_PATH);

                    if (m_computeShader == null)
                    {
                        Debug.LogError("Couldn't find a Compute Shader at: " + COMPUTE_SHADER_PATH);
                        GUI.enabled = true;
                        return;
                    }
                }

                RenderTexture renderTexture = Utils.CreateTempRenderTexture(GravityMap.SIZE, GravityMap.SIZE, Color.black, GravityField.GRAPHICS_FORMAT);
                GenerateGravityTexture(m_inputTexture, renderTexture, out m_centreOfGravity);

                m_outputFinal = renderTexture.ToTexture2D();

                m_outputFinal.alphaIsTransparency = true;

                Color[] cols = m_outputFinal.GetPixels();

                float maxX = Mathf.NegativeInfinity;
                float maxY = Mathf.NegativeInfinity;

                float minX = Mathf.Infinity;
                float minY = Mathf.Infinity;

                for (int i = 0; i < cols.Length; i++)
                {
                    if (cols[i].r > maxX)
                        maxX = cols[i].r;

                    if (cols[i].g > maxY)
                        maxY = cols[i].g;

                    if (cols[i].r < minX)
                        minX = cols[i].r;

                    if (cols[i].g < minY)
                        minY = cols[i].g;
                }


                renderTexture.Release();
            }
            GUI.enabled = true;

            if (m_outputFinal != null)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Output Texture", m_outputFinal, typeof(Texture2D), false);
                EditorGUILayout.Vector2Field("Centre Of Gravity", m_centreOfGravity);
                GUI.enabled = true;

                if (GUILayout.Button("Save as Asset"))
                {
                    Debug.Log("Creating from " + m_inputTexture.name, m_inputTexture);
                    GravityMap map = GravityMap.Create(m_inputTexture, m_outputFinal, m_centreOfGravity, m_inputTexture.name);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Saved as " + map.name + "!", map);
                }
            }
        }

        public static void GenerateGravityTexture(Texture2D input, RenderTexture output, out Vector2 centreOfGravity)
        {
            if (m_computeShader == null || m_occupiedTexelsAppendBuffer == null || m_occupiedTexelsCountBuffer == null)
            {
                ReleaseBuffers();
                Initialize();
            }

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            centreOfGravity = default;

            if (input.width * input.height > MAX_INPUT_SIZE * MAX_INPUT_SIZE)
            {
                Debug.LogError("Input texture dimensions (" + input.width + ", " + input.height + ") exceeds maximum allowed texture size of " + MAX_INPUT_SIZE + "x" + MAX_INPUT_SIZE);
                return;
            }

            if (!IsPowerOfTwo(input.width) || !IsPowerOfTwo(input.height))
            {
                Debug.LogWarning("Input texture dimensions (" + input.width + ", " + input.height + ") are not powers of 2, output may look weird.");
            }

            m_computeShader.SetTexture(m_massDistributionKernel, MASS_DISTRIBUTION_INPUT_TEXTURE, input);
            m_computeShader.SetTexture(m_generateGravityMapKernel, MASS_DISTRIBUTION_INPUT_TEXTURE, input);
            m_computeShader.SetTexture(m_generateGravityMapKernel, GRAVITY_MAP_OUTPUT_TEXTURE, output);
            m_computeShader.SetInt(INPUT_TEXTURE_WIDTH, input.width);
            m_computeShader.SetInt(INPUT_TEXTURE_HEIGHT, input.height);

            // first we dispatch the kernel, which just empties the buffer 
            // equivalent to setting its data to 0, 0, 0, but faster than doing it on cpu side
            m_computeShader.Dispatch(m_clearOutputBufferKernel, 1, 1, 1);

            m_occupiedTexelsAppendBuffer.SetCounterValue(0);
            m_occupiedTexelsCountBuffer.SetCounterValue(0);

            // this kernel finds each texel which has mass, and sums the overall mass
            m_computeShader.Dispatch(m_massDistributionKernel, input.width, input.height, 1);
            ComputeBuffer.CopyCount(m_occupiedTexelsAppendBuffer, m_occupiedTexelsCountBuffer, 0);

            // finally, do a gravity calculation from each texel to every other texel with mass
            // (this is why sparser textures will get faster results)
            m_computeShader.Dispatch(m_generateGravityMapKernel, GravityMap.SIZE / 32, GravityMap.SIZE / 32, 1);

            // get the data for the centre of gravity. can't be avoided unfortunately :(
            m_outputBuffer.GetData(m_outputData);

            stopwatch.Stop();
            Debug.Log("Took " + stopwatch.ElapsedMilliseconds + " milliseconds");

            // since atomic operations can only be on ints, we compute it as ints and then divide it by a large value
            float xCentre = (m_outputData[1] / MULTIPLICATION_FACTOR / input.width) * GravityMap.SIZE;
            float yCentre = (m_outputData[2] / MULTIPLICATION_FACTOR / input.height) * GravityMap.SIZE;

            centreOfGravity = new Vector2(xCentre, yCentre);
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}