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
        private const string EFFECT_SCALAR_PROPERTY = "_EffectScalar";
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

        private int m_computeFullFieldKernel = -1;

        [SerializeField]
        private Canvas m_canvas;
        private RectTransform m_canvasRectTransform;

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
        [OnValueChanged("OnEffectScalarChanged")]
        private float m_effectScalar = 0.01f;

        [SerializeField]
        [OnValueChanged("OnGridScaleChanged")]
        private float m_gridScale = 0.2f;

        [SerializeField]
        private PowerOfTwoResolution m_gravitySampleResolution = PowerOfTwoResolution._256;

        [SerializeField]
        private PowerOfTwoResolution m_textureResolution = PowerOfTwoResolution._512;

        [SerializeField]
        [OnValueChanged("OnColourModeChanged")]
        private ColourMode m_colourMode;

        [SerializeField]
        [ReadOnly]
        private float m_size;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_bottomLeft;

        [SerializeField]
        [ReadOnly]
        private Vector2 m_topRight;

        [SerializeField]
        [ReadOnly]
        private RenderTexture m_renderTexture;

        private bool m_initialized = false;

        private void Initialize()
        {
            m_initialized = true;

            m_gravityFieldComputeShader = Instantiate(m_gravityFieldComputeShader);

            m_transform = transform;
            m_canvasRectTransform = m_canvas.transform as RectTransform;
            m_size = m_canvasRectTransform.rect.width;
            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();

            m_computeFullFieldKernel = m_gravityFieldComputeShader.FindKernel("ComputeFullField");

            m_renderTexture = GravityField.CreateTempRenderTexture((int)m_gravitySampleResolution, (int)m_gravitySampleResolution, format: GravityField.GRAPHICS_FORMAT);

            m_gravityFieldComputeShader.SetTexture(m_computeFullFieldKernel, "GravityFieldOutputTexture", m_renderTexture);

            m_materialInstance = new Material(m_material);
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
            m_materialInstance.SetFloat(EFFECT_SCALAR_PROPERTY, m_effectScalar);
            m_materialInstance.SetInt(FIELD_SIZE_PROPERTY, (int)m_gravitySampleResolution);
            m_materialInstance.SetTexture("_GravityFieldOutputTexture", m_renderTexture);

            if (m_colourMode == ColourMode.Distortion)
                m_materialInstance.EnableKeyword(IS_DISTORTION_MAP_PROPERTY);
            else
                m_materialInstance.DisableKeyword(IS_DISTORTION_MAP_PROPERTY);

            m_size = (m_canvas.transform as RectTransform).rect.width;
            m_gravityFieldComputeShader.SetVector("BottomLeft", m_bottomLeft);
            m_gravityFieldComputeShader.SetVector("TopRight", m_topRight);

            m_materialInstance.SetTexture("_GravityFieldOutputTexture", m_renderTexture);

            m_rawImage.texture = null;
            m_rawImage.material = m_materialInstance;

            m_transform.hasChanged = false;
        }

        private void OnEnable()
        {
            Initialize();
            //m_gravityField.RegisterVisualizer(this);
        }

        private void OnDisable()
        {
            //m_gravityField.DeregisterVisualizer(this);
            Destroy(m_materialInstance);
            m_renderTexture.Release();
        }

        private void Update()
        {
            if (!m_initialized)
                return;

            if (m_transform.hasChanged || m_size != m_canvasRectTransform.rect.width)
            {
                OnMoved();
                m_transform.hasChanged = false;
            }

            if (!enabled || m_computeFullFieldKernel < 0 || m_materialInstance == null)
                return;

            // only really need to do this when an attractor or this visualizer moves
            m_gravityFieldComputeShader.Dispatch(m_computeFullFieldKernel, (int)m_gravitySampleResolution / 16, (int)m_gravitySampleResolution / 16, 1);
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
            m_size = m_canvasRectTransform.rect.width;
            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();

            m_size = (m_canvas.transform as RectTransform).rect.width;
            m_gravityFieldComputeShader?.SetVector("BottomLeft", m_bottomLeft);
            m_gravityFieldComputeShader?.SetVector("TopRight", m_topRight);
        }

        private void OnGridScaleChanged()
        {
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
        }

        private void OnEffectScalarChanged()
        {
            m_materialInstance.SetFloat(EFFECT_SCALAR_PROPERTY, m_effectScalar);
        }
    }

}