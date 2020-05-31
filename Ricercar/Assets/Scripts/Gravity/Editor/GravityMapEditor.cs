using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ricercar.Gravity
{
    [CustomEditor(typeof(GravityMap))]
    public class GravityMapEditor : Editor
    {
        private SerializedProperty m_texture;
        private SerializedProperty m_centreOfGravity;
        private SerializedProperty m_script;
        
        private Material m_lineMaterial;

        private void OnEnable()
        {
            m_texture = serializedObject.FindProperty("m_texture");
            m_centreOfGravity = serializedObject.FindProperty("m_centreOfGravity");
            m_script = serializedObject.FindProperty("m_Script");
            CreateLineMaterial();
        }

        private void OnDisable()
        {
            DestroyImmediate(m_lineMaterial);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_script);
            GUI.enabled = true;

            Texture2D tex = (Texture2D)m_texture.objectReferenceValue;

            float size = EditorGUIUtility.currentViewWidth;

            Rect rect = GUILayoutUtility.GetRect(size, size, size, size, GUILayout.ExpandHeight(false));

            Vector2 bottomLeft = new Vector2(rect.x, rect.y + rect.height);
            Vector2 bottomRight = new Vector2(rect.x + rect.width, rect.y + rect.height);
            Vector2 topRight = new Vector2(rect.x + rect.width, rect.y);
            Vector2 topLeft = new Vector2(rect.x, rect.y);

            GUI.DrawTexture(rect, tex);

            Vector2 centreOfGravity = m_centreOfGravity.vector2Value;

            m_lineMaterial.SetPass(0); 
            GL.PushMatrix();

            GL.Begin(GL.LINES);
            GL.Color(Color.white);

            // vertical line
            GL.Vertex(Vector2.Lerp(bottomLeft, bottomRight, centreOfGravity.x / GravityMap.SIZE));
            GL.Vertex(Vector2.Lerp(topLeft, topRight, centreOfGravity.x / GravityMap.SIZE));

            // horizontal line
            GL.Vertex(Vector2.Lerp(topLeft, bottomLeft, 1f - (centreOfGravity.y / GravityMap.SIZE)));
            GL.Vertex(Vector2.Lerp(topRight, bottomRight, 1f - (centreOfGravity.y / GravityMap.SIZE)));

            GL.End();
            GL.PopMatrix();

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateLineMaterial()
        {
            // Unity has a built-in shader that is useful for drawing simple colored things
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            m_lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // Turn on alpha blending
            m_lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            m_lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            m_lineMaterial.SetInt("_ZWrite", 0);
        }
    }
}