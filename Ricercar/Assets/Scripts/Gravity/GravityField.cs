using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    /// <summary>
    /// A gravity grid stores a discrete grid of gravity samples. We use bilinear sampling
    /// to make the grid continuous. Grids can be tiled to make a continuous gravity field.
    /// </summary>
    public class GravityField : MonoBehaviour
    {
        private static readonly List<GravityField> m_allFields = new List<GravityField>();

        private static readonly List<Attractor> m_allAttractors = new List<Attractor>();

        private static readonly List<Attractor> m_staticAttractors = new List<Attractor>();

        public const float G = 667.4f;

        [SerializeField]
        [MinValue(1)]
        private int m_resolution = 2048;

        [SerializeField]
        [MinValue(1)]
        private int m_textureResolution = 128;

        [SerializeField]
        [MinValue(0f)]
        private float m_size = 1f;

        [SerializeField]
        [ReadOnly]
        private Texture2D m_texture;

        [SerializeField]
        [OnValueChanged("OnDisplayDataChanged")]
        private bool m_displayData = true;

        [SerializeField]
        private Renderer m_quad;

        [SerializeField]
        private Material m_sharedMaterial;

        [SerializeField]
        [HideInInspector]
        private Material m_displayMaterial;

        public float CellSize => m_size / (m_resolution - 1);

        [SerializeField]
        [HideInInspector]
        private Transform m_transform;

        // the array is arranged such that (0,0) is the bottom left
        // and the ordinates are ordered x,y. In other words, it's column first:
        //
        // [ (0, 2) ] [ (1, 2) ] [ (2, 2) ]
        // [ (0, 1) ] [ (1, 1) ] [ (2, 1) ]
        // [ (0, 0) ] [ (1, 0) ] [ (2, 0) ]

        [SerializeField]
        //[HideInInspector]
        private Vector2[] m_gravityPoints;

        [SerializeField]
        //[HideInInspector]
        private Vector2[] m_positions;

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

        #region Unity Callbacks

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Destroy(m_displayMaterial);
        }

        private void OnEnable()
        {
            m_allFields.Add(this);
        }

        private void OnDisable()
        {
            m_allFields.Remove(this);
        }

        #endregion

        #region Array Access

        private Vector2 GetGravityPoint(int x, int y)
        {
            return m_gravityPoints[y * m_resolution + x];
        }
        private void SetGravityPoint(int x, int y, Vector2 gravityPoint)
        {
            m_gravityPoints[y * m_resolution + x] = gravityPoint;
        }

        private void AddToGravityPoint(int x, int y, Vector2 gravityPoint)
        {
            m_gravityPoints[y * m_resolution + x] += gravityPoint;
        }

        private Vector2 GetPosition(int x, int y)
        {

            if (m_positions.IsOutOfRange(y * m_resolution + x))
            {
                Debug.Log((y * m_resolution + x) + " is out of range of " + m_positions.Length);
                Debug.Log("X = " + x + ", " + y);
            }


            return m_positions[y * m_resolution + x];
        }

        private void SetPosition(int x, int y, Vector2 position)
        {
            m_positions[y * m_resolution + x] = position;
        }

        #endregion

        public Vector2 GetBottomLeft()
        {
            return (Vector2)m_transform.position - Vector2.one * m_size * 0.5f;
        }

        public Vector2 GetTopRight()
        {
            return (Vector2)m_transform.position + Vector2.one * m_size * 0.5f;
        }

        public Vector2 GetBottomRight()
        {
            return (Vector2)m_transform.position + Vector2.down * m_size * 0.5f + Vector2.right * m_size * 0.5f;
        }

        public Vector2 GetTopLeft()
        {
            return (Vector2)m_transform.position + Vector2.up * m_size * 0.5f + Vector2.left * m_size * 0.5f;
        }

        private void Initialize()
        {
            m_transform = transform;

            m_bottomLeft = GetBottomLeft();
            m_topRight = GetTopRight();
            m_bottomRight = GetBottomRight();
            m_topLeft = GetTopLeft();
        }

        private void GenerateTexture()
        {
            Debug.Log("Setting texture.");

            m_texture = CreateTexture(m_textureResolution);

            m_quad.gameObject.SetActive(m_displayData);
            m_quad.transform.localScale = Vector3.one * m_size;

            if (m_displayMaterial != null)
            {
                if (!Application.isPlaying)
                    DestroyImmediate(m_displayMaterial);
                else
                    Destroy(m_displayMaterial);
            }

            m_displayMaterial = new Material(m_sharedMaterial);

            m_quad.material = m_displayMaterial;

            m_displayMaterial.SetTexture("_MainTex", m_texture);
        }

        [Button]
        public void BakeAll()
        {
            FindAllStaticAttractors();

            GravityField[] fields = FindObjectsOfType<GravityField>();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].Initialize();
                fields[i].BakeGravity();
                fields[i].GenerateTexture();
            }
        }

        [Button]
        public void FindAllStaticAttractors()
        {
            Attractor[] attractors = FindObjectsOfType<Attractor>();

            for (int i = 0; i < attractors.Length; i++)
            {
                if (attractors[i].IsStatic)
                    AddAttractor(attractors[i]);
            }
        }

        [Button]
        public void ToggleDisplays()
        {
            bool display = !m_displayData;

            GravityField[] fields = FindObjectsOfType<GravityField>();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetDisplayData(display);
            }
        }

        public void BakeGravity()
        {
            m_gravityPoints = new Vector2[m_resolution * m_resolution];
            m_positions = new Vector2[m_resolution * m_resolution];

            Vector2 zero = Vector2.zero;

            float cellSize = CellSize;

            for (int x = 0; x < m_resolution; x++)
            {
                for (int y = 0; y < m_resolution; y++)
                {
                    SetGravityPoint(x, y, zero);

                    for (int i = 0; i < m_staticAttractors.Count; i++)
                    {
                        Vector2 pos = new Vector2(m_bottomLeft.x + cellSize * x, m_bottomLeft.y + cellSize * y);

                        SetPosition(x, y, pos);
                        AddToGravityPoint(x, y, m_staticAttractors[i].CalculateGravitationalForce(pos));
                    }
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Returns true if the given position is inside this field segment.
        /// </summary>
        public bool Contains(Vector2 pos)
        {
            return pos.x >= m_bottomLeft.x && pos.x <= m_topRight.x && pos.y >= m_bottomLeft.y && pos.y <= m_topRight.y;
        }

        /// <summary>
        /// Given a world position, returns the grid coordinates. Always rounds down.
        /// </summary>
        private (int, int) GetCell(Vector2 worldPos)
        {
            // determine which cell we're in

            return (Mathf.FloorToInt(Mathf.InverseLerp(m_bottomLeft.x, m_topRight.x, worldPos.x) * (m_resolution - 1f)),
                Mathf.FloorToInt(Mathf.InverseLerp(m_bottomLeft.y, m_topRight.y, worldPos.y) * (m_resolution - 1f)));
        }

        /// <summary>
        /// Sample from the grid using bilinear interpolation.
        /// </summary>
        private Vector2 SampleGravityAt(Vector2 worldPos)
        {
            (int x0, int y0) = GetCell(worldPos);

            if (x0 >= m_resolution - 1)
                x0--;

            if (y0 >= m_resolution - 1)
                y0--;

            if (x0 < 0 || y0 < 0)
                return Vector2.zero;

            //x0 = Mathf.Clamp(x0, 0, m_resolution - 2);
            //y0 = Mathf.Clamp(y0, 0, m_resolution - 2);

            int x1 = x0 + 1;
            int y1 = y0 + 1;

            Vector2 bottomLeft = GetPosition(x0, y0);
            Vector2 topRight = GetPosition(x1, y1);

            float x_t = Mathf.InverseLerp(bottomLeft.x, topRight.x, worldPos.x);
            float y_t = Mathf.InverseLerp(bottomLeft.y, topRight.y, worldPos.y);

            Vector2 lerp_bottom = Vector2.Lerp(GetGravityPoint(x0, y0), GetGravityPoint(x1, y0), x_t);
            Vector2 lerp_top = Vector2.Lerp(GetGravityPoint(x0, y0), GetGravityPoint(x1, y1), x_t);

            return Vector2.Lerp(lerp_bottom, lerp_top, y_t);
        }

        #region Static Methods

        /// <summary>
        /// Add an attractor to the gravity field system.
        /// </summary>
        public static void AddAttractor(Attractor attractor)
        {
            if (attractor.IsStatic && !m_staticAttractors.Contains(attractor))
                m_staticAttractors.Add(attractor);

            if (!m_allAttractors.Contains(attractor))
                m_allAttractors.Add(attractor);
        }

        /// <summary>
        /// Remove an attractor from the gravity field system. This won't do much
        /// if you remove a static attractor.
        /// </summary>
        public static void RemoveAttractor(Attractor attractor)
        {
            if (attractor.IsStatic)
                m_staticAttractors.Remove(attractor);

            m_allAttractors.Remove(attractor);
        }

        /// <summary>
        /// Gets the total gravity at the given position. Optionally can ignore given attractors.
        /// </summary>
        public static Vector2 GetGravity(Vector2 position, params Attractor[] ignore)
        {
            Vector2 gravityForce = Vector3.zero;

            gravityForce += GetStaticGravity(position);
            gravityForce += GetDynamicGravity(position, ignore);

            return gravityForce;
        }

        /// <summary>
        /// Get the gravity for the given attractor, both static and dynamic.
        /// </summary>
        public static Vector2 GetGravity(Attractor attractor)
        {
            return GetGravity(attractor.Position, attractor);
        }

        /// <summary>
        /// Returns only the "baked" gravity of objects which don't actively influence the gravity field.
        /// </summary>
        public static Vector2 GetStaticGravity(Vector2 worldPos)
        {
            for (int i = 0; i < m_allFields.Count; i++)
            {
                if (m_allFields[i].Contains(worldPos))
                {
                    return m_allFields[i].SampleGravityAt(worldPos);
                }
            }

            return GetDynamicGravity(worldPos);
        }

        /// <summary>
        /// Returns the dynamic gravity at the given position. Dynamic here means unbaked, it's more expensive. It's literally
        /// calculating the gravitational attraction to every other attractor!
        /// </summary>
        public static Vector2 GetDynamicGravity(Vector2 position, params Attractor[] ignore)
        {
            Vector2 result = Vector2.zero;

            for (int i = 0; i < m_allAttractors.Count; i++)
            {
                Attractor attractor = m_allAttractors[i];

                if (attractor.IsStatic)
                    continue;

                if (!attractor.AffectsFields)
                    continue;

                if (!ignore.IsNullOrEmpty())
                {
                    bool found = false;
                    for (int j = 0; j < ignore.Length; j++)
                    {
                        if (ignore[j] == attractor)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;
                }

                result += attractor.CalculateGravitationalForce(position);
            }

            return result;
        }

        #endregion

        #region Texture

        /// <summary>
        /// Create a square texture representing the static gravitational field, with the given size.
        /// </summary>
        private Texture2D CreateTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            Color[] colours = new Color[size * size];

            for (int x = 0; x < size; x++)
            {
                float worldX = Mathf.Lerp(m_bottomLeft.x, m_topRight.x, x / (size - 1f));

                for (int y = 0; y < size; y++)
                {
                    float worldY = Mathf.Lerp(m_bottomLeft.y, m_topRight.y, y / (size - 1f));

                    colours[x + y * size] = GravityToColour(SampleGravityAt(new Vector2(worldX, worldY)), 0.03f);
                }
            }

            texture.SetPixels(colours);
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// Convert a 2d direction vector to an arbitrary colour.
        /// </summary>
        private Color GravityToColour(Vector2 gravity, float scalar = 1f)
        {
            float left;
            float right;

            if (gravity.x < 0f)
            {
                left = -gravity.x;
                right = 0f;
            }
            else
            {
                left = 0f;
                right = gravity.x;
            }

            float up;
            float down;

            if (gravity.y < 0f)
            {
                down = -gravity.y;
                up = 0f;
            }
            else
            {
                down = 0f;
                up = gravity.y;
            }

            Color result = new Color(0f, 0f, 0f, 1f);

            left *= scalar;
            up *= scalar;
            down *= scalar;
            right *= scalar;

            result = Color.Lerp(result, Color.blue, left);
            result = Color.Lerp(result, Color.green, up);
            result = Color.Lerp(result, Color.red, down);
            result = Color.Lerp(result, Color.yellow, right);

            return result;
        }

        #endregion

        #region Editor Stuff

        public void SetDisplayData(bool isDisplaying)
        {
            m_displayData = isDisplaying;

            OnDisplayDataChanged();
        }

        private void OnDisplayDataChanged()
        {
            m_quad.gameObject.SetActive(m_displayData);
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.white;

            Handles.DrawAAPolyLine(m_bottomLeft, m_topLeft, m_topRight, m_bottomRight, m_bottomLeft);
        }
#endif
        #endregion
    }
}