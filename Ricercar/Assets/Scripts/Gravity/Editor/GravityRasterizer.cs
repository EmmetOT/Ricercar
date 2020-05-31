using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Ricercar.Gravity
{
    public class GravityRasterizer : EditorWindow
    {
        private const int MAX_INPUT_SIZE = 300;

        private const string COMPUTE_SHADER_PATH = "GravityRasterizerShader";

        private const string MASS_DISTRIBUTION_KERNEL = "CalculateMassDistribution";
        private const string MASS_DISTRIBUTION_INPUT_TEXTURE = "ImageInput";
        private const string MASS_DISTRIBUTION_OUTPUT_BUFFER = "MassDistributionOutput";

        private const string GENERATE_GRAVITY_MAP_KERNEL = "GenerateGravityMap";
        private const string GRAVITY_MAP_OUTPUT_TEXTURE = "GravityMapOutput";
        private const string INPUT_TEXTURE_WIDTH = "InputWidth";
        private const string INPUT_TEXTURE_HEIGHT = "InputHeight";

        private const float MULTIPLICATION_FACTOR = 10000f;

        private static Texture2D m_inputTexture;
        private static ComputeShader m_computeShader;

        private static Texture2D m_outputFinal;
        private static Vector2 m_centreOfGravity;

        private static int m_massDistributionKernel;
        private static int m_generateGravityMapKernel;
        private static ComputeBuffer m_outputBuffer;

        private static readonly int[] m_outputData = new int[3];

        [MenuItem("Tools/Gravity Rasterizer")]
        public static void ShowWindow()
        {
            m_centreOfGravity = default;
            m_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_PATH);

            m_massDistributionKernel = m_computeShader.FindKernel(MASS_DISTRIBUTION_KERNEL);
            m_generateGravityMapKernel = m_computeShader.FindKernel(GENERATE_GRAVITY_MAP_KERNEL);
            m_outputBuffer = new ComputeBuffer(3, 4);
            m_computeShader.SetBuffer(m_massDistributionKernel, MASS_DISTRIBUTION_OUTPUT_BUFFER, m_outputBuffer);
            m_computeShader.SetBuffer(m_generateGravityMapKernel, MASS_DISTRIBUTION_OUTPUT_BUFFER, m_outputBuffer);

            GetWindow(typeof(GravityRasterizer));
        }

        private void OnDestroy()
        {
            m_outputBuffer?.Release();
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

                RenderTexture renderTexture = GravityField.CreateTempRenderTexture(GravityMap.SIZE, GravityMap.SIZE, Color.black, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16_SFloat);
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

                Debug.Log("MaxX = " + maxX);
                Debug.Log("MaxY = " + maxY);
                Debug.Log("MinX = " + minX);
                Debug.Log("MinY = " + minY);

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
                    GravityMap map = GravityMap.Create(m_outputFinal, m_centreOfGravity, m_inputTexture.name);
                    Debug.Log("Saved as " + map.name + "!", map);
                }
            }
        }

        public static void GenerateGravityTexture(Texture2D input, RenderTexture output, out Vector2 centreOfGravity)
        {
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
            m_computeShader.SetInt("TextureOutputOffset", (GravityMap.SIZE - input.width) / 2);
            m_computeShader.SetInt(INPUT_TEXTURE_WIDTH, input.width);
            m_computeShader.SetInt(INPUT_TEXTURE_HEIGHT, input.height);

            m_outputData[0] = 0;
            m_outputData[1] = 0;
            m_outputData[2] = 0;

            m_outputBuffer.SetData(m_outputData);

            m_computeShader.Dispatch(m_massDistributionKernel, input.width, input.height, 1);
            m_computeShader.Dispatch(m_generateGravityMapKernel, GravityMap.SIZE / 32, GravityMap.SIZE / 32, 1);

            m_outputBuffer.GetData(m_outputData);

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