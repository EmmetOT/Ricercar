using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Rendering;

namespace Ricercar.Gravity
{
    public class GravityVisualizer : MonoBehaviour
    {
        private static Gradient m_colourGradient;

        [SerializeField]
        [Layer]
        private int m_visualizeForLayer;

        [SerializeField]
        private bool m_showField = false;

        [SerializeField]
        private Camera m_camera;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(0f)]
        private float m_vectorScale = 1f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(0f)]
        private float m_arrowScale = 0.3f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(0f)]
        private float m_minVectorMagnitude = 0.05f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(0f)]
        private float m_maxVectorMagnitude = 2f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(1)]
        private int m_fieldResolution = 10;

        private void OnValidate()
        {
            if (m_colourGradient == null)
                m_colourGradient = Utils.CreateGradient(Color.white, Color.white, Color.blue, Color.red);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!m_showField)
                return;
            
            Vector2 bottomLeft = m_camera.ViewportToWorldPoint(new Vector3(0f, 0f, 10f));
            Vector2 topLeft = m_camera.ViewportToWorldPoint(new Vector3(0f, 1f, 10f));
            Vector2 bottomRight = m_camera.ViewportToWorldPoint(new Vector3(1f, 0f, 10f));
            
            Vector2 xComponent = Vector2.Lerp(bottomLeft, bottomRight, 1f / (m_fieldResolution - 1f)) - bottomLeft;
            Vector2 yComponent = Vector2.Lerp(bottomLeft, topLeft, 1f / (m_fieldResolution - 1f)) - bottomLeft;

            for (int x = 0; x < m_fieldResolution; x++)
            {
                for (int y = 0; y < m_fieldResolution; y++)
                {
                    Vector2 pos = bottomLeft + xComponent * x + yComponent * y;

                    Vector2 attraction = Attractor.GetDynamicGravity(pos/*, 1f*//*, layer: m_visualizeForLayer*/);

                    if (attraction.magnitude * m_vectorScale <= m_minVectorMagnitude)
                        continue;

                    float attractionMagnitude = Mathf.Clamp(attraction.magnitude * m_vectorScale, m_minVectorMagnitude, m_maxVectorMagnitude);

                    float arrowScalar = Mathf.InverseLerp(m_minVectorMagnitude, m_maxVectorMagnitude, attractionMagnitude);

                    Vector3 attractionDirection = attraction.normalized;

                    Color col = m_colourGradient.Evaluate(Mathf.InverseLerp(0f, 2f, attractionMagnitude));

                    Utils.DrawArrow(pos, attractionDirection, col, attractionMagnitude * m_vectorScale, m_arrowScale * arrowScalar);
                }
            }
        }
#endif
    }

}