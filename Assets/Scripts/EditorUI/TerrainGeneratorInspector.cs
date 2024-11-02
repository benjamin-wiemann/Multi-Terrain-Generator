using UnityEngine;
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

            if (GUILayout.Button("Delete"))
            {
                mapGen.Init();
            }
        }

    }
}