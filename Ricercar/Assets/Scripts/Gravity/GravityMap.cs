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
        [ShowAssetPreview]
        [ReadOnly]
        private Texture2D m_texture;
        public Texture2D Texture => m_texture;

        [SerializeField]
        private Vector2 m_centreOfGravity;
        public Vector2 CentreOfGravity => m_centreOfGravity;

        public static GravityMap Create(Texture2D texture, Vector2 centreOfGravity, string name)
        {
            if (texture.width != SIZE || texture.height != SIZE)
            {
                Debug.LogError("GravityMap texture must be " + SIZE + "x" + SIZE);
                return null;
            }

            Utils.SaveTexture(texture, "GravityMap_Texture_" + name);
            GravityMap map = Utils.CreateAsset<GravityMap>("GravityMap_" + name);

            map.m_texture = texture;
            map.m_centreOfGravity = centreOfGravity;

            Debug.Log("Saving texture with graphics format: " + map.m_texture.graphicsFormat);

            EditorUtility.SetDirty(map);
            return map;
        }
    }
}