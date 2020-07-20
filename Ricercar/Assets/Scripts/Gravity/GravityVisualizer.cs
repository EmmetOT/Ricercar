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
        private static readonly int EFFECT_SCALAR_PROPERTY = Shader.PropertyToID("_EffectScalar");
        private static readonly int GRID_SCALE_PROPERTY = Shader.PropertyToID("_GridScale");
        private static readonly int NEUTRAL_GRAVITY_COLOUR_PROPERTY = Shader.PropertyToID("_NeutralGravityColour");
        private static readonly int POSITIVE_GRAVITY_COLOUR_PROPERTY = Shader.PropertyToID("_PositiveGravityColour");
        private static readonly int NEGATIVE_GRAVITY_COLOUR_PROPERTY = Shader.PropertyToID("_NegativeGravityColour");
        private static readonly int GRAVITY_AURA_SIZE_PROPERTY = Shader.PropertyToID("_GravityAuraSize");
        private static readonly int ROTATION_PROPERTY = Shader.PropertyToID("_CameraRotationDegrees");

        private const string IS_DISTORTION_MAP_PROPERTY = "IS_DISTORTION_MAP";

        public enum ColourMode
        {
            DISTORTED_TEXTURE,
            GRAVITY
        }

        [SerializeField]
        [BoxGroup("Components")]
        private GravityField m_gravityField;

        private Transform m_transform;

        [SerializeField]
        [BoxGroup("Components")]
        private ComputeShader m_gravityFieldComputeShader;

        private int m_computeFullFieldKernel = -1;

        [SerializeField]
        [BoxGroup("Components")]
        private Canvas m_canvas;
        private RectTransform m_canvasRectTransform;

        [SerializeField]
        [BoxGroup("Components")]
        private RawImage m_rawImage;

        [SerializeField]
        [BoxGroup("Components")]
        private Material m_material;
        private Material m_materialInstance;

        private GravityQueryObject m_gravityQuery;

        [SerializeField]
        [GravityLayer]
        private int m_cameraLayer;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        private bool m_setRotation = true;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        [GravityLayer]
        private int m_visualizationLayer;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        private Texture m_background;

        [SerializeField]
        [OnValueChanged("OnEffectScalarChanged")]
        [BoxGroup("Visual Settings")]
        private float m_effectScalar = 0.01f;

        [SerializeField]
        [OnValueChanged("OnGridScaleChanged")]
        [BoxGroup("Visual Settings")]
        private float m_gridScale = 0.2f;

        [SerializeField]
        [MinValue(1f)]
        [BoxGroup("Visual Settings")]
        [OnValueChanged("OnCanvasWidthChanged")]
        private float m_canvasWidth = 20f;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        [OnValueChanged("ApplyColourSettings")]
        private Color m_neutralGravityColour;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        [OnValueChanged("ApplyColourSettings")]
        private Color m_positiveGravityColour;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        [OnValueChanged("ApplyColourSettings")]
        private Color m_negativeGravityColour;

        [SerializeField]
        [BoxGroup("Visual Settings")]
        [MinValue(0f)]
        [OnValueChanged("ApplyAuraSize")]
        private float m_auraSize = 1f;

        [SerializeField]
        [OnValueChanged("ApplyColourSettings")]
        [BoxGroup("Visual Settings")]
        private ColourMode m_colourMode;

        [SerializeField]
        [MinValue(16)]
        [BoxGroup("Sample Size")]
        private int m_sampleWidth;

        [SerializeField]
        [MinValue(16)]
        [BoxGroup("Sample Size")]
        private int m_sampleHeight;

        private float AspectRatio => (float)m_sampleHeight / m_sampleWidth;

        private float CanvasHeight => m_canvasWidth * AspectRatio;

        private Vector2 CanvasSize => new Vector2(m_canvasWidth, CanvasHeight);

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private Vector2 m_bottomLeft;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private Vector2 m_topRight;

        [SerializeField]
        [ReadOnly]
        [BoxGroup("State")]
        private RenderTexture m_renderTexture;

        private bool m_initialized = false;

        private void Initialize()
        {
            m_initialized = true;

            m_gravityFieldComputeShader = Instantiate(m_gravityFieldComputeShader);

            m_transform = transform;
            m_canvasRectTransform = m_canvas.transform as RectTransform;
            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();

            m_computeFullFieldKernel = m_gravityFieldComputeShader.FindKernel("ComputeFullField");

            m_renderTexture = Utils.CreateTempRenderTexture(m_sampleWidth, m_sampleHeight, format: GravityField.GRAPHICS_FORMAT);

            m_gravityFieldComputeShader.SetTexture(m_computeFullFieldKernel, "GravityFieldOutputTexture", m_renderTexture);

            m_materialInstance = new Material(m_material);
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
            m_materialInstance.SetFloat(EFFECT_SCALAR_PROPERTY, m_effectScalar);
            m_materialInstance.SetTexture("_GravityFieldOutputTexture", m_renderTexture);

            SetVisualizationLayer();
            ApplyColourSettings();
            ApplyAuraSize();

            m_gravityFieldComputeShader.SetVector("BottomLeft", m_bottomLeft);
            m_gravityFieldComputeShader.SetVector("TopRight", m_topRight);
            m_gravityFieldComputeShader.SetVector("FullFieldSampleSize", new Vector2(m_sampleWidth, m_sampleHeight));

            m_materialInstance.SetTexture("_GravityFieldOutputTexture", m_renderTexture);

            m_rawImage.texture = null;
            m_rawImage.material = m_materialInstance;

            m_transform.hasChanged = false;
        }

        public void SetGravityQuery(GravityQueryObject query)
        {
            m_gravityQuery = query;
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

            if (m_transform.hasChanged || m_canvasRectTransform.sizeDelta != CanvasSize)
            {
                OnMoved();
                m_transform.hasChanged = false;
            }

            if (!enabled || m_computeFullFieldKernel < 0 || m_materialInstance == null)
                return;

            // only really need to do this when an attractor or this visualizer moves
            m_gravityFieldComputeShader.Dispatch(m_computeFullFieldKernel, (int)m_sampleWidth / 16, (int)m_sampleHeight / 16, 1);
        }

        private void LateUpdate()
        {
            if (!m_setRotation)
                return;

            // passing a rotation value into the shader so we can measure the screen space gradient of the gravity texture
            // without having to worry about rotation affecting it
            Vector2 gravityVector = m_gravityQuery.CurrentGravity.normalized;
            float gravityAngle = -Mathf.Atan2(gravityVector.y, gravityVector.x) * Mathf.Rad2Deg - 90f;

            m_materialInstance.SetFloat(ROTATION_PROPERTY, gravityAngle);
        }

        public Vector2 GetTopRight()
        {
            return (Vector2)m_transform.position + CanvasSize * 0.5f;
        }

        public Vector2 GetBottomLeft()
        {
            return (Vector2)m_transform.position - CanvasSize * 0.5f;
        }

        private void ApplyColourSettings()
        {
            if (m_materialInstance == null)
                return;

            if (m_colourMode == ColourMode.DISTORTED_TEXTURE)
                m_materialInstance.EnableKeyword(IS_DISTORTION_MAP_PROPERTY);
            else
                m_materialInstance.DisableKeyword(IS_DISTORTION_MAP_PROPERTY);

            m_materialInstance.SetColor(NEUTRAL_GRAVITY_COLOUR_PROPERTY, m_neutralGravityColour);
            m_materialInstance.SetColor(POSITIVE_GRAVITY_COLOUR_PROPERTY, m_positiveGravityColour);
            m_materialInstance.SetColor(NEGATIVE_GRAVITY_COLOUR_PROPERTY, m_negativeGravityColour);
        }

        private void OnMoved()
        {
            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();

            m_canvasRectTransform.sizeDelta = CanvasSize;
            m_gravityFieldComputeShader.SetVector("BottomLeft", m_bottomLeft);
            m_gravityFieldComputeShader.SetVector("TopRight", m_topRight);
        }

        private void OnGridScaleChanged()
        {
            m_materialInstance.SetFloat(GRID_SCALE_PROPERTY, m_gridScale);
        }

        private void OnEffectScalarChanged()
        {
            m_materialInstance.SetFloat(EFFECT_SCALAR_PROPERTY, m_effectScalar);
        }

        private void OnCanvasWidthChanged()
        {
            m_canvasRectTransform = m_canvas.transform as RectTransform;
            m_canvasRectTransform.sizeDelta = CanvasSize;

            if (EditorApplication.isPlaying)
                OnMoved();
        }

        private void ApplyAuraSize()
        {
            m_materialInstance.SetFloat(GRAVITY_AURA_SIZE_PROPERTY, m_auraSize);
        }

        private void SetVisualizationLayer()
        {
            m_gravityFieldComputeShader.SetInt("VisualLayer", m_visualizationLayer);
        }
    }

}