using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;

namespace Ricercar.Gravity
{
    [CustomEditor(typeof(GravityLayerData))]
    public class GravityLayerDataEditor : Editor
    {
        private bool m_showMatrix = true;
        private bool m_showLayers = true;
        private Vector2 m_layerScrollVec;
        private Vector2 m_matrixScrollVec;

        private GravityLayerData m_data;
        public GravityLayerData Data
        {
            get
            {
                m_data = target as GravityLayerData;
                return m_data;
            }
        }

        private bool GetGravityInteractionValue(int layerA, int layerB) => Data.GetIgnoreLayerInteraction(layerA, layerB);
        private void SetGravityInteractionValue(int layerA, int layerB, bool val) => Data.IgnoreLayerInteraction(layerA, layerB, val);

        public override void OnInspectorGUI()
        {
            GravityLayerMatrixGUI.DrawGUI(Data, ref m_showLayers, ref m_showMatrix, ref m_layerScrollVec, ref m_matrixScrollVec, GetGravityInteractionValue, SetGravityInteractionValue);
        }
    }
}