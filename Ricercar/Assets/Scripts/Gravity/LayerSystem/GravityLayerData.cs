using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ricercar.Gravity
{
    [CreateAssetMenu(fileName = "Gravity Layer Data", menuName = "Ricercar/Gravity Layer Data Object", order = 1)]
    public class GravityLayerData : ScriptableObject
    {
        public const int MAX_LAYERS = 32;

        [SerializeField]
        private int[] m_gravityInteractions = new int[MAX_LAYERS];

        [SerializeField]
        private string[] m_layerNames = new string[MAX_LAYERS];

        public string LayerToName(int index) => m_layerNames[index];

        public IEnumerable<string> LayerNames
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

        public void SetLayerName(int index, string name)
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Set Layer Name");
#endif

            m_layerNames[index] = name;
        }

        public int NameToLayer(string layerName)
        {
            for (int i = 0; i < m_layerNames.Length; i++)
                if (m_layerNames.Equals(layerName))
                    return i;

            return -1;
        }

        public bool GetIgnoreLayerInteraction(int layerA, int layerB)
        {
            return ((1 << layerA) & m_gravityInteractions[layerB]) != 0;
        }

        public void IgnoreLayerInteraction(int layerA, int layerB, bool val)
        {

#if UNITY_EDITOR
            Undo.RecordObject(this, "Changed Layer Interaction");
#endif

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
        }

        public int[] GetGravityInteractionsArray()
        {
            int[] copy = new int[MAX_LAYERS];
            m_gravityInteractions.CopyTo(copy, 0);
            return copy;
        }
    }
}