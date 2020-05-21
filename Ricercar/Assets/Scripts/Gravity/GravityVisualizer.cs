using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEditor;

namespace Ricercar.Gravity
{
    public class GravityVisualizer : MonoBehaviour
    {
        private enum PowerOfTwoResolution 
        { 
            _16 = 16,
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
        };

        //// 256 x 256 = 16 x 16 x 16 x 16
        //private const int THREAD_GROUPS = 65536;

        //private const int THREAD_GROUPS_SQRT = 16;

        private const string POINT_ARRAY_PROPERTY = "_Points";
        private const string FIELD_SIZE_PROPERTY = "_FieldSize";
        private const string EFFECT_SCALAR_PROPERTY = "_ColourScale";
        private const string GRID_SCALE_PROPERTY = "_GridScale";
        private const string IS_DISTORTION_MAP_PROPERTY = "IS_DISTORTION_MAP";

        public enum ColourMode
        {
            Distortion,
            Legible
        }

        [SerializeField]
        private GravityField m_gravityField;

        private Transform m_transform;

        [SerializeField]
        private ComputeShader m_gravityFieldComputeShader;

        private ComputeBuffer m_fieldOutputBuffer;
        private int m_computeFullFieldKernel = -1;

        [SerializeField]
        private Canvas m_canvas;

        [SerializeField]
        private RawImage m_rawImage;

        [SerializeField]
        private Texture m_background;

        [SerializeField]
        private Material m_material;

        [SerializeField]
        private Material m_materialInstance;

        [SerializeField]
        [MinValue(0f)]
        private float m_effectScalar = 0.01f;

        [SerializeField]
        [OnValueChanged("OnGridScaleChanged")]
        private float m_gridScale = 0.2f;

        [SerializeField]
        private PowerOfTwoResolution m_gravitySampleResolution = PowerOfTwoResolution._256;

        [SerializeField]
        private PowerOfTwoResolution m_textureResolution = PowerOfTwoResolution._512;

        //private bool IsPowerOfTwo(int value) => value != 0 && ((value & (value - 1)) == 0);

        [SerializeField]
        [OnValueChanged("OnColourModeChanged")]
        private ColourMode m_colourMode;

        [SerializeField]
        [ReadOnly]
        private float m_size;

        private void Start()
        {
            Initialize();

            m_rawImage.texture = null;
            m_rawImage.material = m_materialInstance;

            m_transform.hasChanged = false;
        }

        private void Initialize()
        {
            m_transform = transform;
            m_size = (m_canvas.transform as RectTransform).rect.width;

            m_computeFullFieldKernel = m_gravityFieldComputeShader.FindKernel("ComputeFullField");

            // 256 x 256
            m_fieldOutputBuffer = new ComputeBuffer((int)m_gravitySampleResolution * (int)m_gravitySampleResolution, 8);
            m_gravityFieldComputeShader.SetBuffer(m_computeFullFieldKernel, "GravityField", m_fieldOutputBuffer);

            m_gravityFieldComputeShader.SetVector("BottomLeft", GetBottomLeft());
            m_gravityFieldComputeShader.SetVector("TopRight", GetTopRight());

            m_materialInstance = new Material(m_material);
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
            m_materialInstance.SetFloat(EFFECT_SCALAR_PROPERTY, m_effectScalar);
            m_materialInstance.SetInt(FIELD_SIZE_PROPERTY, (int)m_gravitySampleResolution);
            m_materialInstance.SetBuffer(POINT_ARRAY_PROPERTY, m_fieldOutputBuffer);

            if (m_colourMode == ColourMode.Distortion)
                m_materialInstance.EnableKeyword(IS_DISTORTION_MAP_PROPERTY);
            else
                m_materialInstance.DisableKeyword(IS_DISTORTION_MAP_PROPERTY);
        }

        private void ReleaseBuffers()
        {
            m_fieldOutputBuffer?.Release();
        }

        private void OnEnable()
        {
            m_gravityField.RegisterVisualizer(this);
        }

        private void OnDisable()
        {
            m_gravityField.DeregisterVisualizer(this);
        }

        private void OnDestroy()
        {
            ReleaseBuffers(); 
            Destroy(m_materialInstance);
        }
        
        public void SetInputData(ComputeBuffer inputBuffer)
        {
            if (m_computeFullFieldKernel == -1)
                return;

            Debug.Log("Setting input data in " + name, this);
            m_gravityFieldComputeShader.SetBuffer(m_computeFullFieldKernel, "PointAttractors", inputBuffer);
        }

        private void Update()
        {
            if (m_transform.hasChanged)
            {
                OnMoved();
                m_transform.hasChanged = false;
            }

            if (!enabled || m_computeFullFieldKernel < 0 || m_materialInstance == null)
                return;

            m_gravityFieldComputeShader.Dispatch(m_computeFullFieldKernel, (int)m_gravitySampleResolution / 16, (int)m_gravitySampleResolution / 16, 1);
        }

        [Button]
        private void SetTexture()
        {
            Initialize();

            ComputeBuffer inputBuffer = m_gravityField.ForceGeneratePointInputBuffer();

            if (inputBuffer == null)
                Debug.Log("It's null!");

            SetInputData(inputBuffer);
            m_gravityFieldComputeShader.Dispatch(m_computeFullFieldKernel, (int)m_gravitySampleResolution / 16, (int)m_gravitySampleResolution / 16, 1);

            m_rawImage.texture = GenerateTexture();

            ReleaseBuffers();
            m_gravityField.ReleaseBuffers();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(m_rawImage);
#endif
        }

        private Texture2D GenerateTexture()
        {
            RenderTexture destination = RenderTexture.GetTemporary((int)m_textureResolution, (int)m_textureResolution);
            Graphics.Blit(m_background, destination, m_materialInstance);

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = destination;

            Texture2D tex = new Texture2D((int)m_textureResolution, (int)m_textureResolution)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            tex.ReadPixels(new Rect(0, 0, (int)m_textureResolution, (int)m_textureResolution), 0, 0, false);
            tex.Apply();

            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(destination);

            return tex;
        }

        public Vector2 GetTopRight()
        {
            return (Vector2)m_transform.position + Vector2.one * m_size * 0.5f;
        }

        public Vector2 GetBottomLeft()
        {
            return (Vector2)m_transform.position - Vector2.one * m_size * 0.5f;
        }

        private void OnColourModeChanged()
        {
            if (m_materialInstance == null)
                return;

            if (m_colourMode == ColourMode.Distortion)
                m_materialInstance.EnableKeyword(IS_DISTORTION_MAP_PROPERTY);
            else
                m_materialInstance.DisableKeyword(IS_DISTORTION_MAP_PROPERTY);
        }

        private void OnMoved()
        {
            m_gravityFieldComputeShader.SetVector("BottomLeft", GetBottomLeft());
            m_gravityFieldComputeShader.SetVector("TopRight", GetTopRight());
        }

        private void OnGridScaleChanged()
        {
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
        }
    }

}