using System;
using UnityEditor;
using UnityEngine;

namespace Ricercar.Gravity
{
    public struct GravityLayerMask
    {
        private int m_mask;

        /// <summary>
        ///   <para>Converts a layer mask value to an integer value.</para>
        /// </summary>
        public int Value
        {
            get
            {
                return m_mask;
            }
            set
            {
                m_mask = value;
            }
        }

        public static implicit operator int(GravityLayerMask mask)
        {
            return mask.m_mask;
        }

        public static implicit operator GravityLayerMask(int intVal)
        {
            GravityLayerMask layerMask;
            layerMask.m_mask = intVal;
            return layerMask;
        }

        public static string LayerToName(int layer) => GravityInteraction.LayerToName(layer);

        public static int NameToLayer(string layerName) => GravityInteraction.NameToLayer(layerName);

        public static int GetMask(params string[] layerNames)
        {
            if (layerNames == null)
                throw new ArgumentNullException("layerNames");

            int num = 0;

            foreach (string layerName in layerNames)
            {
                int layer = NameToLayer(layerName);
                if (layer != -1)
                    num |= 1 << layer;
            }

            return num;
        }
    }
}