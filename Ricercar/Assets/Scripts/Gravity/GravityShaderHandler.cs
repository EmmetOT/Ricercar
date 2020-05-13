using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class GravityShaderHandler : MonoBehaviour
    {
        private const string POINTS_ARRAY_PROPERTY = "_Points";
        private const string POINTS_ARRAY_COUNT_PROPERTY = "_PointCount";

        [SerializeField]
        private Shader m_gravityFieldShader;

        [SerializeField]
        private Vector2 m_testPos;

        [SerializeField]
        private float m_testMass = 3f;

        [SerializeField]
        [MinValue(1)]
        private int m_resolution = 2048;

        [SerializeField]
        private Vector3 m_worldBottomLeft;

        [SerializeField]
        private Vector3 m_worldTopRight;

        private Material m_gravityFieldMaterial;

        [SerializeField]
        [ReadOnly]
        private RenderTexture m_gravityFieldTexture;

        [SerializeField]
        [ReadOnly]
        private Texture2D m_texture;

        [SerializeField]
        private Renderer m_quad;

        [SerializeField]
        private Attractor[] m_attractors;

        private readonly List<Vector4> m_attractorData = new List<Vector4>();

        private void Awake()
        {
            m_gravityFieldMaterial = new Material(m_gravityFieldShader);
            m_gravityFieldTexture = new RenderTexture(m_resolution, m_resolution, 0)//, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point,
                isPowerOfTwo = true
            };

            m_quad.transform.position = (m_worldBottomLeft + m_worldTopRight) * 0.5f;
            m_quad.transform.localScale = new Vector3(m_worldTopRight.x - m_worldBottomLeft.x, m_worldTopRight.y - m_worldBottomLeft.y, 1f);

            m_quad.gameObject.SetActive(true);
            m_quad.material = m_gravityFieldMaterial;

            m_texture = new Texture2D(m_resolution, m_resolution, TextureFormat.RGB24, false, false);

            Generate();
        }

        private void Update()
        {
            if (m_attractors.IsNullOrEmpty())
                return;

            DrawAttractors(m_attractors);
        }

        public Vector2 NormalizePosition(Vector2 position)
        {
            float x = Utils.InverseLerpUnclamped(m_worldBottomLeft.x, m_worldTopRight.x, position.x);
            float y = Utils.InverseLerpUnclamped(m_worldBottomLeft.y, m_worldTopRight.y, position.y);

            return new Vector2(x, y);
        }

        public void DrawAttractors(Attractor[] attractors)
        {
            m_attractorData.Clear();

            for (int i = 0; i < attractors.Length; i++)
            {
                Vector2 pos = NormalizePosition(attractors[i].Position);
                float mass = attractors[i].Mass;

                Vector4 data = new Vector4(pos.x, pos.y, mass, 0f);

                m_attractorData.Add(data);
            }
            
            m_gravityFieldMaterial.SetInt(POINTS_ARRAY_COUNT_PROPERTY, attractors.Length);
            m_gravityFieldMaterial.SetVectorArray(POINTS_ARRAY_PROPERTY, m_attractorData);

            TestBlit(m_gravityFieldTexture);
            //TestToTexture();
        }

        [Button]
        private void Generate()
        {
            m_gravityFieldMaterial.SetInt(POINTS_ARRAY_COUNT_PROPERTY, 1);
            m_gravityFieldMaterial.SetVectorArray(POINTS_ARRAY_PROPERTY, new List<Vector4>(new Vector4[] { new Vector4(m_testPos.x, m_testPos.y, m_testMass, 0f) }));

            TestBlit(m_gravityFieldTexture);
        }

        private void TestBlit(RenderTexture target)
        {
            RenderTexture temp = RenderTexture.GetTemporary(target.width, target.height);

            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;

            Graphics.Blit(temp, target, m_gravityFieldMaterial);

            RenderTexture.ReleaseTemporary(temp);
        }

        [Button]
        private void TestToTexture()
        {
            //StartCoroutine(Cr_TestToTexture(m_gravityFieldTexture));

            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = m_gravityFieldTexture;

            RenderTexture temp = RenderTexture.GetTemporary(m_resolution, m_resolution, 24);
            m_texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            m_texture.Apply();

            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = rt;

            Color[] colors = m_texture.GetPixels();
            Debug.Log(colors.Length);

            if (colors.Length < 100)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    Debug.Log(colors[i].ToString());
                }
            }
        }

        private readonly WaitForEndOfFrame Cr_WaitForEndOfFrame = new WaitForEndOfFrame();
        
        private IEnumerator Cr_TestToTexture(RenderTexture source)
        {

            for (int i = 0; i < 60; i++)
            {
                yield return Cr_WaitForEndOfFrame;
            }

            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = source;
            
            RenderTexture temp = RenderTexture.GetTemporary(m_resolution, m_resolution, 24);
            m_texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            m_texture.Apply();

            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = rt;

        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(m_worldBottomLeft, 0.4f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(m_worldTopRight, 0.4f);
        }
#endif

    }
}