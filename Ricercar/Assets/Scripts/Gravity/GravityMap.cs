using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.UI;

namespace Ricercar.Gravity
{
    public class GravityMap : ScriptableObject
    {
        public const int SIZE = 2048;

        [SerializeField]
        [ReadOnly]
        private Texture m_sourceTexture;

        [SerializeField]
        [ShowAssetPreview]
        [ReadOnly]
        private Texture2D m_texture;
        public Texture2D Texture => m_texture;

        [SerializeField]
        private Vector2 m_centreOfGravity;
        public Vector2 CentreOfGravity => m_centreOfGravity;

        [SerializeField]
        [ReadOnly]
        private float m_size;
        public float Size => m_size;

        [SerializeField]
        [ReadOnly]
        private int m_guid;
        public int GUID => m_guid;

        public Vector2 LocalTopLeftCorner => new Vector2(m_size * -0.5f, m_size * 0.5f);
        public Vector2 LocalTopRightCorner => new Vector2(m_size * 0.5f, m_size * 0.5f);
        public Vector2 LocalBottomLeftCorner => new Vector2(m_size * -0.5f, m_size * -0.5f);
        public Vector2 LocalBottomRightCorner => new Vector2(m_size * 0.5f, m_size * -0.5f);

        /// <summary>
        /// Converts given 2d coordinate from the texture space, where the origin is the bottom left, to the world space,
        /// where the origin is the centre.
        /// </summary>
        public Vector2 TextureSpaceToWorldSpace(Vector2 input)
        {
            return new Vector2(Mathf.Lerp(-m_size * 0.5f, m_size * 0.5f, Mathf.InverseLerp(0f, m_size, input.x)), Mathf.Lerp(-m_size * 0.5f, m_size * 0.5f, Mathf.InverseLerp(0f, m_size, input.y)));
        }

        public static GravityMap Create(Texture sourceTexture, Texture2D texture, Vector2 centreOfGravity, string name)
        {
            Debug.Assert(texture.width == texture.height, "Gravity Maps must be created from square textures.");

            // gravity maps are always square
            int size = texture.width;

            Utils.SaveTexture(texture, "GravityMap_Texture_" + name + "_" + size);
            GravityMap map = Utils.CreateAsset<GravityMap>("GravityMap_" + name + "_" + size);

            map.m_guid = System.Guid.NewGuid().GetHashCode();
            map.m_sourceTexture = sourceTexture;
            map.m_texture = texture;
            map.m_centreOfGravity = centreOfGravity;

            // gravity maps are always square
            map.m_size = size;

            Debug.Log("Saving texture with graphics format: " + map.m_texture.graphicsFormat);

            EditorUtility.SetDirty(map);
            return map;
        }
    }
}