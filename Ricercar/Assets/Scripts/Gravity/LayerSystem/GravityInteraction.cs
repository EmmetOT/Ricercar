using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    public static class GravityInteraction
    {
        private static GravityLayerData m_data;
        public static GravityLayerData Data
        {
            get
            {
                if (m_data == null)
                    LoadData();

                return m_data;
            }
        }

        private static string DATA_PATH = "Assets/Data/Resources/Gravity Layer Data.asset";

        public static void LoadData()
        {
            m_data = AssetDatabase.LoadAssetAtPath<GravityLayerData>(DATA_PATH);
        }


        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            AssemblyReloadEvents.afterAssemblyReload += AfterReload;
        }

        private static void AfterReload()
        {
            RefreshPublicArrays();
        }

        public static string[] s_layerNames;
        public static int[] s_layerIndices;

        public static void RefreshPublicArrays()
        {
            int count = 0;
            foreach (string layerName in Data.LayerNames)
                if (!layerName.IsNullOrEmpty())
                    ++count;

            s_layerNames = new string[count];
            s_layerIndices = new int[count];

            int index = 0;
            foreach (string layerName in Data.LayerNames)
            {
                s_layerNames[index] = index + ": " + layerName;
                s_layerIndices[index] = index;
                ++index;
            }
        }

        public static string LayerToName(int index) => Data.LayerToName(index);
        public static IEnumerable<string> LayerNames => Data.LayerNames;
        public static void SetLayerName(int index, string name) => Data.SetLayerName(index, name);
        public static int NameToLayer(string layerName) => Data.NameToLayer(layerName);
        public static bool GetIgnoreLayerInteraction(int layerA, int layerB) => Data.GetIgnoreLayerInteraction(layerA, layerB);

        public static void IgnoreLayerInteraction(int layerA, int layerB, bool val) => Data.IgnoreLayerInteraction(layerA, layerB, val);
        public static int[] GetGravityInteractionsArray() => Data.GetGravityInteractionsArray();
    }
}