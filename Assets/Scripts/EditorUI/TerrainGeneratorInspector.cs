using UnityEngine;
using System.Collections;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;



namespace LiquidPlanet.EditorUI
{


    [CustomEditor(typeof(TerrainGenerator))]
    public class MapGeneratorInspector : Editor
    {

        public override void OnInspectorGUI()
        {
            TerrainGenerator mapGen = (TerrainGenerator)target;

            serializedObject.Update();

            //EditorGUILayout.BeginVertical("box");
            //GUILayout.Label("Mesh size generation");

            //// Add properties from the first group
            //EditorGUI.indentLevel++;
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("meshX"), true);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("meshY"), true);
            //EditorGUI.indentLevel--;

            //EditorGUILayout.EndVertical();

            //// Group 2: Second group with a custom name
            //EditorGUILayout.BeginVertical("box");
            //GUILayout.Label("Terrain");

            //// Add properties from the second group
            //EditorGUI.indentLevel++;
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("_terrainTypes"), true);
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainGranularity"), true);
            //EditorGUI.indentLevel--;

            //EditorGUILayout.EndVertical();

            // Apply any changes to the serialized object
            //serializedObject.ApplyModifiedProperties();

            if (DrawDefaultInspector())
            {
                if (mapGen.autoUpdate)
                {
                    mapGen.Init();
                    mapGen.GenerateMesh();
                }
            }

            if (GUILayout.Button("Generate"))
            { 
                mapGen.Init();
                mapGen.GenerateMesh();
            }
        }

    }
}