using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Ricercar.Gravity
{
    public class GravityVisualizer : MonoBehaviour
    {
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
        private float m_minVectorMagnitude = 0.05f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(0f)]
        private float m_maxVectorMagnitude = 2f;

        [SerializeField]
        [ShowIf("m_showField")]
        [MinValue(1)]
        private int m_fieldResolution = 10;

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

                    Vector2 attraction = Attractor.GetGravityAtPoint(pos, 1f);

                    float attractionMagnitude = Mathf.Clamp(attraction.magnitude * m_vectorScale, m_minVectorMagnitude, m_maxVectorMagnitude);
                    attraction = attraction.normalized * attractionMagnitude;

                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(pos, pos + attraction * m_vectorScale);
                }
            }
        }
#endif
    }

}