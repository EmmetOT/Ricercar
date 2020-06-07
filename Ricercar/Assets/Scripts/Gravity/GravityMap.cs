using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace Ricercar.Gravity
{
    public class GravityMap : ScriptableObject
    {
        public const int SIZE = 256;

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
            if (texture.width != SIZE || texture.height != SIZE)
            {
                Debug.LogError("GravityMap texture must be " + SIZE + "x" + SIZE);
                return null;
            }

            Utils.SaveTexture(texture, "GravityMap_Texture_" + name);
            GravityMap map = Utils.CreateAsset<GravityMap>("GravityMap_" + name);

            map.m_guid = System.Guid.NewGuid().GetHashCode();
            map.m_sourceTexture = sourceTexture;
            map.m_texture = texture;
            map.m_centreOfGravity = centreOfGravity;

            // for now, this is just done to remind me what "size" means - 
            // gravity maps are created in a 256x256 space, and therefore need to be scaled to match the strength
            // of gravity at other scales.
            // if i want to make the size, 256, variable, i'd need to set it here.
            map.m_size = SIZE;

            Debug.Log("Saving texture with graphics format: " + map.m_texture.graphicsFormat);

            EditorUtility.SetDirty(map);
            return map;
        }
    }
}