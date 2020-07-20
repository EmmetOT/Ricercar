using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ricercar.Gravity
{
    public class GravitySettingsWindow : EditorWindow
    {
        private bool m_showMatrix = true;
        private bool m_showLayers = true;
        private Vector2 m_layerScrollVec;
        private Vector2 m_matrixScrollVec;

        private static bool GetGravityInteractionValue(int layerA, int layerB) => GravityInteraction.GetIgnoreLayerInteraction(layerA, layerB);
        private static void SetGravityInteractionValue(int layerA, int layerB, bool val) => GravityInteraction.IgnoreLayerInteraction(layerA, layerB, val);

        [MenuItem("Gravity/Settings")]
        public static void ShowWindow()
        {
            GetWindow<GravitySettingsWindow>("Gravity Settings");
        }

        public void OnEnable()
        {
            //GravityInteraction.LoadData();
        }

        private void OnGUI()
        {
            GravityLayerMatrixGUI.DrawGUI(GravityInteraction.Data, ref m_showLayers, ref m_showMatrix, ref m_layerScrollVec, ref m_matrixScrollVec, GetGravityInteractionValue, SetGravityInteractionValue);
        }
    }
}