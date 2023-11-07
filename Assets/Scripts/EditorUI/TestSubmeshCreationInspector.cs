using UnityEngine;
using System.Collections;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;



namespace LiquidPlanet.EditorUI
{


    [CustomEditor(typeof(TestSubmeshCreation))]
    public class TestSubmeshCreationInspector : Editor
    {

        public override void OnInspectorGUI()
        {
            TestSubmeshCreation mapGen = (TestSubmeshCreation)target;

            serializedObject.Update();

            //if (DrawDefaultInspector())
            //{
            //    if (mapGen.autoUpdate)
            //    {
            //        mapGen.Init();
            //        mapGen.GenerateMesh();
            //    }
            //}
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
            {                 
                mapGen.Generate();
            }
        }

    }
}