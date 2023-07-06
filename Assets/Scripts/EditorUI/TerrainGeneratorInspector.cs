using UnityEngine;
using System.Collections;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;



namespace Waterworld.EditorUI
{


    [CustomEditor(typeof(TerrainGenerator))]
    public class MapGeneratorInspector : Editor
    {

        SerializedProperty myProperty;

        public void OnEnable()
        {
            myProperty = serializedObject.FindProperty("myVariable");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Terrain Generator"));

            // Create the default inspector view
            var defaultInspector = new IMGUIContainer(() => DrawDefaultInspector());
            root.Add(defaultInspector);

            // Create the custom button
            var customButton = new Button(OnClickGenerateButton);
            customButton.text = "Generate";
            root.Add(customButton);

            return root;
        }

        public override void OnInspectorGUI()
        {
            TerrainGenerator mapGen = (TerrainGenerator)target;

            if (DrawDefaultInspector())
            {
                if (mapGen.autoUpdate)
                {
                    mapGen.Init();
                    mapGen.GenerateMesh();
                }
            }
        }

        void OnClickGenerateButton()
        {
            TerrainGenerator mapGen = (TerrainGenerator)target;
            mapGen.Init();
            mapGen.GenerateMesh();
            
        }



    }
}