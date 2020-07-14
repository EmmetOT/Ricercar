// Decompiled with JetBrains decompiler
// Type: UnityEditor.LayerMatrixGUI
// Assembly: UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 01B28312-B6F5-4E06-90F6-BE297B711E41
// Assembly location: C:\Users\Blake\sandbox\unity\test-project\Library\UnityAssemblies\UnityEditor.dll

using UnityEngine;
using Rotorz.ReorderableList.Internal;
using UnityEditor;

namespace Ricercar.Gravity
{
    public class GravityLayerMatrixGUI
    {
        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);

        public static void DoGUI(string title, ref bool show, ref Vector2 scrollVec, GetValueFunc getValue, SetValueFunc setValue)
        {
            const int kMaxLayers = 32;
            const int checkboxSize = 16;
            int labelSize = 100;
            const int indent = 30;

            int numLayers = 0;
            for (int i = 0; i < kMaxLayers; i++)
                if (!GravityLayerMask.LayerToName(i).IsNullOrEmpty())
                    numLayers++;

            // find the longest label
            for (int i = 0; i < kMaxLayers; i++)
            {
                var textDimensions = GUI.skin.label.CalcSize(new GUIContent(GravityLayerMask.LayerToName(i)));
                if (labelSize < textDimensions.x)
                    labelSize = (int)textDimensions.x;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(0);
            show = EditorGUILayout.Foldout(show, title, true);
            GUILayout.EndHorizontal();

            if (show)
            {
                scrollVec = EditorGUILayout.BeginScrollView(scrollVec, GUILayout.Height(labelSize + 10));
                Rect topLabelRect = GUILayoutUtility.GetRect(checkboxSize * numLayers + labelSize, labelSize);
                Rect scrollArea = GUIHelper.TopMostRect.Invoke();
                Vector2 topLeft = GUIHelper.Unclip(new Vector2(topLabelRect.x, topLabelRect.y));
                int y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (!GravityLayerMask.LayerToName(i).IsNullOrEmpty())
                    {
                        // Need to do some shifting around here, so the rotated text will still clip correctly
                        float clipOffset = (labelSize + indent + (numLayers - y) * checkboxSize) - (scrollArea.width);
                        if (clipOffset < 0)
                            clipOffset = 0;

                        Vector3 translate = new Vector3(labelSize + indent + checkboxSize * (numLayers - y) + topLeft.y + topLeft.x - clipOffset, topLeft.y, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

                        GUI.Label(new Rect(2 - topLeft.x, -clipOffset, labelSize, checkboxSize), GravityLayerMask.LayerToName(i), "RightLabel");
                        y++;
                    }
                }
                EditorGUILayout.EndScrollView();

                GUI.matrix = Matrix4x4.identity;
                y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (!GravityLayerMask.LayerToName(i).IsNullOrEmpty())
                    {
                        int x = 0;
                        var r = GUILayoutUtility.GetRect(indent + checkboxSize * numLayers + labelSize, checkboxSize);
                        GUI.Label(new Rect(r.x + indent, r.y, labelSize, checkboxSize), GravityLayerMask.LayerToName(i), "RightLabel");
                        for (int j = kMaxLayers - 1; j >= 0; j--)
                        {
                            if (!GravityLayerMask.LayerToName(j).IsNullOrEmpty())
                            {
                                if (x < numLayers - y)
                                {
                                    var tooltip = new GUIContent("", GravityLayerMask.LayerToName(i) + "/" + GravityLayerMask.LayerToName(j));
                                    bool val = getValue(i, j);
                                    bool toggle = GUI.Toggle(new Rect(labelSize + indent + r.x + x * checkboxSize, r.y, checkboxSize, checkboxSize), val, tooltip);
                                    if (toggle != val)
                                        setValue(i, j, toggle);
                                }
                                x++;
                            }
                        }
                        y++;
                    }
                }

                GUILayout.Space(40);
            }
        }
    }
}
