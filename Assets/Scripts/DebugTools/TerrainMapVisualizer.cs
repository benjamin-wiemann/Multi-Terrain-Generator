using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.IO;
using System;
using MultiTerrain.Segmentation;

namespace MultiTerrain.DebugTools
{

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainMapVisualizer : MonoBehaviour
    {
        enum DataView
        {
            HeightMap,
            TerrainSegmentation
        }

        [SerializeField]
        DataView _dataView;

        [SerializeField]
        int _terrainFilter;

        [SerializeField]
        bool _writeToDisk = false;

        [SerializeField]
        string _filePath = "./GeneratedTextures/";

        int _width;

        int _height;

        Texture2D[] _segmentationTextures;

        Texture2D _heightTexture;
        
        NativeArray<TerrainCombination> _segmentation;

        TerrainTypeStruct[] _terrainTypes;

        NativeArray<float> _heigthMap;


        void OnValidate()
        {
            if (_segmentation.Length > 0 && _heigthMap.Length > 0) 
            {
                _terrainFilter = Mathf.Clamp( _terrainFilter, 0, _terrainTypes.Length );
                Visualize();
            }
            
        }

        public void OnMeshGenerationFinished(Event.MeshGenFinishedEventArgs args)
        {
            _width = args.NumVerticesX;
            _height = args.NumVerticesY;
            _segmentation = args.TerrainSegmentation;
            _terrainTypes = args.TerrainTypes;
            _heigthMap = args.HeightMap;
            _segmentationTextures = VisualizeSegmentation(_segmentation, _terrainTypes, _width - 1, _height - 1);
            _heightTexture = VisualizeHeightMap(_heigthMap, _width, _height);
            OnValidate();
            Visualize();
            if (_writeToDisk && Directory.Exists(_filePath))
            {
                DateTime dt = DateTime.Now;                
                File.WriteAllBytes(_filePath + "/" + dt.ToString("yyyy-MM-dd_HH-mm-ss") + "_Segmentation.png", _segmentationTextures[_segmentationTextures.Length - 1].EncodeToPNG());
                File.WriteAllBytes(_filePath + "/" + dt.ToString("yyyy-MM-dd_HH-mm-ss") + "_Height.png", _heightTexture.EncodeToPNG());
            }
        }

        void Visualize()
        {            
            switch (_dataView)
            {
                case DataView.TerrainSegmentation:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = _segmentationTextures[_terrainFilter];
                    break;
                case DataView.HeightMap:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = _heightTexture;
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

        Texture2D[] VisualizeSegmentation( 
            NativeArray<TerrainCombination> segmentation, 
            TerrainTypeStruct[] terrainTypes, 
            int width, 
            int height )
        {

            Texture2D[] textures = new Texture2D[terrainTypes.Length + 1];
            for(int i = 0; i < textures.Length; i++)
            {
                textures[i] = new Texture2D(width, height);
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    TerrainCombination weighting = segmentation[y * width + x];
                    float4 intensities = weighting.Weightings;
                    int4 indices = weighting.Ids;
                    Color[] colors = new Color[terrainTypes.Length + 1];
                    for( int i = 0; i < colors.Length; i++ )
                    {
                        colors[i] = Color.black; 
                    }
                    for (int i = 0; i < Mathf.Min(terrainTypes.Length, 4); i++)
                    {                        
                        int terrainIndex = indices[i];
                        float3 col = terrainTypes[terrainIndex].Color;
                        Color terrainColor = new Color(col.x, col.y, col.z) * intensities[i];
                        colors[terrainIndex] = terrainColor;                        
                        colors[colors.Length - 1] += terrainColor;
                        textures[terrainIndex].SetPixel(x, y, terrainColor);
                    }                    
                    textures[textures.Length - 1].SetPixel(x, y, colors[colors.Length - 1]);                    
                }
                
            }
            
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Apply();
            }
            
            return textures;
        }

    }

}
