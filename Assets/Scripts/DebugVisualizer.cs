using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace LiquidPlanet
{
    public class DebugVisualizer : MonoBehaviour
    {

        [SerializeField]
        private TerrainGenerator generator;

        int width;
        int height;
         

        void Start()
        {
            TerrainGenerator.OnMeshFinishedEvent += this.OnMeshGenerationFinished;
            
        }

        void OnMeshGenerationFinished()
        {
            var segmentation = generator.Segmentator.Segmentation;
            var terrainTypes = generator.terrainTypes;
            GetComponent<Renderer>().material.mainTexture = VisualizeSegmentation(segmentation, terrainTypes);
        }

        Texture2D VisualizeSegmentation( NativeArray<int> segmentation, List<TerrainType> terrainTypes )
        {
            
            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int patchIndex = segmentation[y * width + x] % terrainTypes.Count;
                    Color color = terrainTypes[patchIndex].Color;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
