using System.Collections;
using System.Collections.Generic;
using UnibusEvent;
using UnityEngine;
using System.Linq;

namespace Ricercar.Gravity
{
    [System.Serializable]
    public class GravityFieldTextureCreator
    {
        public enum ColourMode
        {
            Distortion,
            Legible
        }

        [SerializeField]
        private Texture m_texture;

        [SerializeField]
        private Material m_sharedMaterial;

        [SerializeField]
        private Material m_material;
        public Material Material => m_material;

        [SerializeField]
        private ColourMode m_colourMode;

        [SerializeField]
        private float m_colourScale;

        private ComputeBuffer m_computeBuffer;

        private const string GRID_TEXTURE_PROPERTY = "_MainTex";
        private const string POINT_ARRAY_PROPERTY = "_Points";
        private const string FIELD_SIZE_PROPERTY = "_FieldSize";
        private const string COLOUR_SCALE_PROPERTY = "_ColourScale";
        private const string IS_DISTORTION_MAP_PROPERTY = "IS_DISTORTION_MAP";

        //public void BlitToRenderTexture(Vector2[] field, int fieldSize, int textureSize, RenderTexture renderTexture)
        //{
        //    using (m_computeBuffer = new ComputeBuffer(field.Length, 8))
        //    {
        //        m_computeBuffer.SetData(field);

        //        BlitToRenderTexture(m_computeBuffer, fieldSize, textureSize, renderTexture);
        //    }
        //}

        public void BlitToRenderTexture(ComputeBuffer buffer, int fieldSize, RenderTexture renderTexture)
        {
            if (m_material == null)
                m_material = new Material(m_sharedMaterial);

            m_material.SetBuffer(POINT_ARRAY_PROPERTY, buffer);
            m_material.SetInt(FIELD_SIZE_PROPERTY, fieldSize);
            m_material.SetFloat(COLOUR_SCALE_PROPERTY, m_colourScale);

            if (m_colourMode == ColourMode.Distortion)
                m_material.EnableKeyword(IS_DISTORTION_MAP_PROPERTY);
            else
                m_material.DisableKeyword(IS_DISTORTION_MAP_PROPERTY);

            Graphics.Blit(m_texture, renderTexture, m_material);
        }

        public Texture2D GenerateTextureFromField(ComputeBuffer buffer, int fieldSize, int textureSize, RenderTexture texture)
        {
            BlitToRenderTexture(buffer, fieldSize, texture);

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = texture;

            Texture2D tex = new Texture2D(textureSize, textureSize)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0, false);
            tex.Apply();

            RenderTexture.active = active;

            return tex;
        }
        public Texture2D GenerateTextureFromField(ComputeBuffer buffer, int fieldSize, int textureSize)
        {
            RenderTexture destination = RenderTexture.GetTemporary(textureSize, textureSize);
            BlitToRenderTexture(buffer, fieldSize, destination);

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = destination;

            Texture2D tex = new Texture2D(textureSize, textureSize)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0, false);
            tex.Apply();

            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(destination);

            return tex;
        }
    }
}