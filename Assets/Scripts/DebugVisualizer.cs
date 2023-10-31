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
        private DataView _dataView;

        [SerializeField]
        private TerrainGenerator _generator;

        int _width;

        int _height;
        
        NativeArray<int> _segmentation;

        TerrainTypeUnmanaged[] _terrainTypes;

        NativeArray<float> _heigthMap;

        bool _listening = false;

        private void OnEnable()
        {
            if (!_listening)
            {
                TerrainGenerator.MeshFinishedEvent += this.OnMeshGenerationFinished;
                _listening = true;
            }
        }

        void OnValidate()
        {
            if (!_listening)
            {
                TerrainGenerator.MeshFinishedEvent += this.OnMeshGenerationFinished;
                _listening = true;
            }                
            if (_segmentation.Length > 0 ) 
            {
                Visualize();
            }
            
        }

        void OnMeshGenerationFinished(object sender, Event.MeshGenFinishedEventArgs args)
        {
            _width = args.NumVerticesX; 
            _height = args.NumVerticesY;
            _segmentation = args.TerrainSegmentation;
            _terrainTypes = args.TerrainTypes;
            _heigthMap = args.HeightMap;
            Visualize();
        }

        void Visualize()
        {
            switch (_dataView)
            {
                case DataView.TerrainSegmentation:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = VisualizeSegmentation(_segmentation, _terrainTypes, _width, _height);
                    break;
                case DataView.HeightMap:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = VisualizeHeightMap(_heigthMap, _width, _height);
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

        Texture2D VisualizeSegmentation( NativeArray<int> segmentation, TerrainTypeUnmanaged[] terrainTypes, int width, int height )
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
            _listening = false;
        }
    }

}
