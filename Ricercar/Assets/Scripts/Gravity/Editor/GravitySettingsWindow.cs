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
        static void SetGravityInteractionValue(int layerA, int layerB, bool val) { GravityInteraction.IgnoreLayerInteraction(layerA, layerB, val); }

        [MenuItem("Gravity/Settings")]
        public static void ShowWindow()
        {
            GetWindow<GravitySettingsWindow>("Gravity Settings");
        }

        public void OnEnable()
        {
            GravityInteraction.Load();
        }

        public void OnDisable()
        {
            GravityInteraction.Save();
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                m_showLayers = EditorGUILayout.Foldout(m_showLayers, "Gravity Layers");

                if (m_showLayers)
                {
                    m_layerScrollVec = EditorGUILayout.BeginScrollView(m_layerScrollVec, EditorStyles.helpBox);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            EditorGUI.BeginChangeCheck();
                            string newVal = EditorGUILayout.TextField(new GUIContent("Gravity Layer " + (i + 1)), GravityInteraction.LayerToName(i));
                            if (EditorGUI.EndChangeCheck())
                            {
                                GravityInteraction.SetLayerName(i, newVal);
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();

                }

                GravityLayerMatrixGUI.DoGUI("Layer Interaction Matrix", ref m_showMatrix, ref m_matrixScrollVec, GetGravityInteractionValue, SetGravityInteractionValue);
            }
        }
    }
}