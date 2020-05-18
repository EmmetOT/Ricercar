using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ricercar.Gravity
{
    [CreateAssetMenu(menuName = "Assets/GravityData")]
    public class GravityData : ScriptableObject
    {
        [SerializeField]
        [ShowAssetPreview(width: 2048, height: 2048)]
        private Texture2D m_texture;

        //[SerializeField]
        //[HideInInspector]
        //private Vector2[] m_gravityPoints;
        //public Vector2[] GravityPoints => m_gravityPoints;

        //private Vector2[] m_positions;
        //public Vector2[] Positions => m_positions;

        [SerializeField]
        [ReadOnly]
        private int m_gravityResolution;

        [SerializeField]
        [ReadOnly]
        private int m_textureResolution;

        [SerializeField]
        [ReadOnly]
        private float m_size = 1f;

        // the array is arranged such that (0,0) is the bottom left
        // and the ordinates are ordered x,y. In other words, it's column first:
        //
        // [ (0, 2) ] [ (1, 2) ] [ (2, 2) ]
        // [ (0, 1) ] [ (1, 1) ] [ (2, 1) ]
        // [ (0, 0) ] [ (1, 0) ] [ (2, 0) ]

        [SerializeField]
        [ReadOnly]
        private Vector2 m_position;

        [SerializeField]
        [HideInInspector]
        private Vector2 m_bottomLeft;

        [SerializeField]
        [HideInInspector]
        private Vector2 m_topRight;

        [SerializeField]
        [HideInInspector]
        private Vector2 m_bottomRight;

        [SerializeField]
        [HideInInspector]
        private Vector2 m_topLeft;

        //private Vector2 GetGravityPoint(int x, int y)
        //{
        //    return m_gravityPoints[y * m_gravityResolution + x];
        //}
        //private void SetGravityPoint(int x, int y, Vector2 gravityPoint)
        //{
        //    m_gravityPoints[y * m_gravityResolution + x] = gravityPoint;
        //}

        //private void AddToGravityPoint(int x, int y, Vector2 gravityPoint)
        //{
        //    m_gravityPoints[y * m_gravityResolution + x] += gravityPoint;
        //}

        //private Vector2 GetPosition(int x, int y)
        //{
        //    //if (m_positions.IsNullOrEmpty())
        //    //{
        //    //    float cellSize = CellSize;

        //    //    Vector2 pos = new Vector2(m_bottomLeft.x + cellSize * x, m_bottomLeft.y + cellSize * y);

        //    //    SetPosition(x, y, pos);
        //    //}

        //    //return m_positions[y * m_gravityResolution + x];
        //    return Vector2.zero;
        //}

        //private void SetPosition(int x, int y, Vector2 position)
        //{
        //    //m_positions[y * m_gravityResolution + x] = position;
        //}

        public Vector2 GetBottomLeft()
        {
            return m_position - Vector2.one * m_size * 0.5f;
        }

        public Vector2 GetTopRight()
        {
            return m_position + Vector2.one * m_size * 0.5f;
        }

        public Vector2 GetBottomRight()
        {
            return m_position + Vector2.down * m_size * 0.5f + Vector2.right * m_size * 0.5f;
        }

        public Vector2 GetTopLeft()
        {
            return m_position + Vector2.up * m_size * 0.5f + Vector2.left * m_size * 0.5f;
        }

        public float CellSize => m_size / (m_gravityResolution - 1);

//        public Texture2D CreateTexture(GravityFieldTextureCreator textureCreator, int resolution)
//        {
//            if (m_texture != null)
//                DestroyImmediate(m_texture);

//            m_textureResolution = resolution;
//            m_texture = textureCreator.GenerateTextureFromField(m_gravityPoints, m_gravityResolution, m_textureResolution);

//#if UNITY_EDITOR
//            EditorUtility.SetDirty(this);
//#endif

//            return m_texture;
//        }

        //public void BlitInto(GravityFieldTextureCreator textureCreator, RenderTexture rt)
        //{
        //    textureCreator.BlitToRenderTexture(m_gravityPoints, m_gravityResolution, m_textureResolution, rt);
        //}

        public void BlitInto(ComputeBuffer buffer, GravityFieldTextureCreator textureCreator, RenderTexture rt)
        {
            textureCreator.BlitToRenderTexture(buffer, m_gravityResolution, rt);
        }

        public Texture2D GenerateTexture2D(ComputeBuffer buffer, GravityFieldTextureCreator textureCreator)
        {
            m_texture = textureCreator.GenerateTextureFromField(buffer, m_gravityResolution, m_textureResolution);
            return m_texture;
        }

        public void Bake(Vector2 position, float size, int gravityResolution, Attractor[] attractors)
        {
            m_position = position;
            m_size = size;
            m_gravityResolution = gravityResolution;

            //m_gravityPoints = new Vector2[m_gravityResolution * m_gravityResolution];
            //m_positions = new Vector2[m_gravityResolution * m_gravityResolution];

            Vector2 zero = Vector2.zero;

            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();
            m_bottomRight = GetBottomRight();
            m_topLeft = GetTopLeft();

            //float cellSize = CellSize;

            //for (int x = 0; x < m_gravityResolution; x++)
            //{
            //    for (int y = 0; y < m_gravityResolution; y++)
            //    {
            //        Vector2 pos = new Vector2(m_bottomLeft.x + cellSize * x, m_bottomLeft.y + cellSize * y);

            //        SetGravityPoint(x, y, zero);
            //        SetPosition(x, y, pos);

            //        for (int i = 0; i < attractors.Length; i++)
            //        {
            //            AddToGravityPoint(x, y, attractors[i].CalculateGravitationalForce(pos));
            //        }
            //    }
            //}

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

//        public void SetGravity(Vector2[] gravityPoints)
//        {
//            if (gravityPoints.Length != (m_gravityResolution * m_gravityResolution))
//            {
//                Debug.Log("Size of gravity points array must equal the square of the resolution, i.e.: " + (m_gravityResolution * m_gravityResolution));
//                return;
//            }

//            m_gravityPoints = gravityPoints;

//#if UNITY_EDITOR
//            EditorUtility.SetDirty(this);
//#endif
//        }

//        /// <summary>
//        /// Sample from the grid using bilinear interpolation.
//        /// </summary>
//        public Vector2 SampleGravityAt(Vector2 worldPos)
//        {
//            (int x0, int y0) = GetCell(worldPos);

//            if (x0 >= m_gravityResolution - 1)
//                x0--;

//            if (y0 >= m_gravityResolution - 1)
//                y0--;

//            if (x0 < 0 || y0 < 0)
//                return Vector2.zero;

//            //x0 = Mathf.Clamp(x0, 0, m_resolution - 2);
//            //y0 = Mathf.Clamp(y0, 0, m_resolution - 2);

//            int x1 = x0 + 1;
//            int y1 = y0 + 1;

//            // first step is to get the normalized position in this 'quadrant' of the field

//            Vector2 bottomLeft = GetPosition(x0, y0);
//            Vector2 topRight = GetPosition(x1, y1);

//            //Debug.Log("BottomLeft: " + bottomLeft);
//            //Debug.Log("TopRight: " + topRight);

//            float x_t = Mathf.InverseLerp(bottomLeft.x, topRight.x, worldPos.x);
//            float y_t = Mathf.InverseLerp(bottomLeft.y, topRight.y, worldPos.y);

//            // using these values we lerp first across the x axis, on the top and bottom of this quadrant

//            Vector2 lerp_bottom = Vector2.Lerp(GetGravityPoint(x0, y0), GetGravityPoint(x1, y0), x_t);
//            Vector2 lerp_top = Vector2.Lerp(GetGravityPoint(x0, y1), GetGravityPoint(x1, y1), x_t);

//            // finally we lerp between these two positions to get the interpolated position

//            return Vector2.Lerp(lerp_bottom, lerp_top, y_t) * 0.01f;
//        }

        /// <summary>
        /// Given a world position, returns the grid coordinates. Always rounds down.
        /// </summary>
        //public (int, int) GetCell(Vector2 worldPos)
        //{
        //    // determine which cell we're in

        //    return (Mathf.FloorToInt(Mathf.InverseLerp(m_bottomLeft.x, m_topRight.x, worldPos.x) * (m_gravityResolution - 1f)),
        //        Mathf.FloorToInt(Mathf.InverseLerp(m_bottomLeft.y, m_topRight.y, worldPos.y) * (m_gravityResolution - 1f)));
        //}

        /// <summary>
        /// Returns true if the given position is inside this field segment.
        /// </summary>
        public bool Contains(Vector2 pos)
        {
            return pos.x >= m_bottomLeft.x && pos.x <= m_topRight.x && pos.y >= m_bottomLeft.y && pos.y <= m_topRight.y;
        }

        public static GravityData Create(GravityField field)
        {
            GravityData data = CreateInstance<GravityData>();
            AssetDatabase.CreateAsset(data, $"Assets/Data/{field.name}.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;

            return data;
        }
    }
}