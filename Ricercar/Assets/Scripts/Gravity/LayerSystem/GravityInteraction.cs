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
        public static string[] s_layerNames;
        public static int[] s_layerIndices;

        private const int MAX_LAYERS = 32;
        private static int[] m_gravityInteractions = new int[MAX_LAYERS];
        private static string[] m_layerNames = new string[MAX_LAYERS];

        private const string PLAYER_PREFS_INTERACTIONS_NAME = "GravityInteractions";
        private const string PLAYER_PREFS_LAYER_NAMES = "GravityLayerNames";

#if UNITY_EDITOR

        public static void Load()
        {
            for (int i = 0; i < MAX_LAYERS; i++)
                m_gravityInteractions[i] = PlayerPrefs.GetInt(PLAYER_PREFS_INTERACTIONS_NAME + i, 0);

            for (int i = 0; i < MAX_LAYERS; i++)
                m_layerNames[i] = PlayerPrefs.GetString(PLAYER_PREFS_LAYER_NAMES + i, "");

            RefreshPublicArrays();
        }

        public static void Save()
        {
            for (int i = 0; i < MAX_LAYERS; i++)
                PlayerPrefs.SetInt(PLAYER_PREFS_INTERACTIONS_NAME + i, m_gravityInteractions[i]);

            for (int i = 0; i < MAX_LAYERS; i++)
                PlayerPrefs.SetString(PLAYER_PREFS_LAYER_NAMES + i, m_layerNames[i]);
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += AfterReload;
        }

        private static void BeforeReload()
        {
            Save();
        }

        private static void AfterReload()
        {
            Load();
        }
#endif

        public static void RefreshPublicArrays()
        {
            int count = 0;
            for (int i = 0; i < MAX_LAYERS; i++)
            {
                if (!m_layerNames[i].IsNullOrEmpty())
                {
                    ++count;
                }
            }

            s_layerNames = new string[count];
            s_layerIndices = new int[count];

            int index = 0;
            for (int i = 0; i < MAX_LAYERS; i++)
            {
                if (!m_layerNames[i].IsNullOrEmpty())
                {
                    s_layerNames[index] = index + ": " + m_layerNames[i];
                    s_layerIndices[index] = index;
                    ++index;
                }
            }
        }

        public static string LayerToName(int index) => m_layerNames[index];

        public static IEnumerable<string> LayerNames
        {
            get
            {
                for (int i = 0; i < m_layerNames.Length; i++)
                {
                    if (!m_layerNames.IsNullOrEmpty())
                        yield return m_layerNames[i];
                }
            }
        }

        public static void SetLayerName(int index, string name)
        {
            m_layerNames[index] = name;
            Save();
            RefreshPublicArrays();
        }

        public static int NameToLayer(string layerName)
        {
            for (int i = 0; i < m_layerNames.Length; i++)
                if (m_layerNames.Equals(layerName))
                    return i;

            return -1;
        }

        public static bool GetIgnoreLayerInteraction(int layerA, int layerB)
        {
            return ((1 << layerA) & m_gravityInteractions[layerB]) != 0;
        }

        public static void IgnoreLayerInteraction(int layerA, int layerB, bool val)
        {
            int layerAMask = 1 << layerA;
            int layerBMask = 1 << layerB;

            if (val)
            {
                m_gravityInteractions[layerA] = m_gravityInteractions[layerA] | layerBMask;
                m_gravityInteractions[layerB] = m_gravityInteractions[layerB] | layerAMask;
            }
            else
            {
                m_gravityInteractions[layerA] = m_gravityInteractions[layerA] & ~layerBMask;
                m_gravityInteractions[layerB] = m_gravityInteractions[layerB] & ~layerAMask;
            }

            Save();
        }
    }
}