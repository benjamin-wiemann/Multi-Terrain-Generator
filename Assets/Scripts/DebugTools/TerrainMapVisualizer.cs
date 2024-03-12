using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace LiquidPlanet.DebugTools
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
        private DataView _dataView;

        int _width;

        int _height;

        Texture2D _segmentationTexture;

        Texture2D _heightTexture;
        
        NativeArray<TerrainInfo> _segmentation;

        TerrainTypeStruct[] _terrainTypes;

        NativeArray<float> _heigthMap;


        void OnValidate()
        {
            if (_segmentation.Length > 0 && _heigthMap.Length > 0) 
            {
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
            _segmentationTexture = VisualizeSegmentation(_segmentation, _terrainTypes, _width - 1, _height - 1);
            _heightTexture = VisualizeHeightMap(_heigthMap, _width, _height);
            Visualize();
        }

        void Visualize()
        {            
            switch (_dataView)
            {
                case DataView.TerrainSegmentation:
                    GetComponent<Renderer>().sharedMaterial.mainTexture = _segmentationTexture;
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

        Texture2D VisualizeSegmentation( NativeArray<TerrainInfo> segmentation, TerrainTypeStruct[] terrainTypes, int width, int height )
        {
            
            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {                    
                    //Color color = terrainTypes[patchIndex].Color;
                    Float9 intensities = segmentation[y * width + x].Intensities;
                    Int9 indices = segmentation[y * width + x].Indices;
                    Color color = Color.black;
                    for (uint i = 0; i < 9; i++)
                    {
                        int terrainIndex = indices[i];
                        color += terrainTypes[terrainIndex].Color * intensities[i];
                    }
                    color = color / 18;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

    }

}
