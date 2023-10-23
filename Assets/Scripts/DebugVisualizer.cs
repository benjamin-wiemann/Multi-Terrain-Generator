using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.VisualScripting;
using System;

namespace LiquidPlanet
{
    
    public class DebugVisualizer : MonoBehaviour
    {
        enum DataView
        {
            HeightMap,
            TerrainSegmentation
        }

        [SerializeField]
        private DataView dataView;

        [SerializeField]
        private TerrainGenerator generator;

        int width;

        int height;
        
        NativeArray<int> segmentation;

        NativeList<TerrainTypeUnmanaged> terrainTypes;

        NativeArray<float> heigthMap;

        bool listening = false;

        void OnValidate()
        {
            if (!listening)
                TerrainGenerator.MeshFinishedEvent += this.OnMeshGenerationFinished;
            if (segmentation.Length > 0 ) 
            {
                Visualize();
            }
            
        }

        void OnMeshGenerationFinished(object sender, Event.MeshGenFinishedEventArgs args)
        {
            width = args.NumVerticesX; 
            height = args.NumVerticesY;
            segmentation = args.TerrainSegmentation;
            terrainTypes = args.TerrainTypes;
            heigthMap = args.HeightMap;
            Visualize();
        }

        void Visualize()
        {
            switch (dataView)
            {
                case DataView.TerrainSegmentation:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = VisualizeSegmentation(segmentation, terrainTypes, width, height);
                    break;
                case DataView.HeightMap:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = VisualizeHeightMap(heigthMap, width, height);
                    break;
            }
        }

        private Texture2D VisualizeHeightMap(NativeArray<float> heightMap, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float val = heightMap[y * width + x];
                    Color color = new Color(val, val, val);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        Texture2D VisualizeSegmentation( NativeArray<int> segmentation, NativeList<TerrainTypeUnmanaged> terrainTypes, int width, int height )
        {
            
            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int patchIndex = segmentation[y * width + x] % terrainTypes.Length;
                    Color color = terrainTypes[patchIndex].Color;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        void OnDestroy( ) 
        {
            TerrainGenerator.MeshFinishedEvent -= this.OnMeshGenerationFinished;
            listening = false;
        }
    }

}
