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
        DataView _dataView;

        [SerializeField]
        int _terrainFilter;

        int _width;

        int _height;

        Texture2D[] _segmentationTextures;

        Texture2D _heightTexture;
        
        NativeArray<TerrainInfo> _segmentation;

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
            Visualize();
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

        Texture2D[] VisualizeSegmentation( NativeArray<TerrainInfo> segmentation, TerrainTypeStruct[] terrainTypes, int width, int height )
        {

            Texture2D[] textures = new Texture2D[terrainTypes.Length + 1];
            for(int i = 0; i < textures.Length; i++)
            {
                textures[i] = new Texture2D(width, height);
            }
            //string row = "";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Float9 intensities = segmentation[y * width + x].Intensities;
                    Int9 indices = segmentation[y * width + x].Indices;
                    Color[] colors = new Color[terrainTypes.Length + 1];
                    for( int i = 0; i < colors.Length; i++ )
                    {
                        colors[i] = Color.black; 
                    }
                    for (uint i = 0; i < indices.Length; i++)
                    {                        
                        int terrainIndex = indices[i];
                        Color intensity = terrainTypes[terrainIndex].Color * intensities[i] * 0.5f;
                        colors[terrainIndex] += intensity;
                        colors[colors.Length - 1] += intensity;
                        textures[terrainIndex].SetPixel(x, y, colors[terrainIndex]);
                    }
                    textures[textures.Length - 1].SetPixel(x, y, colors[colors.Length - 1] / (terrainTypes.Length));
                    //row += string.Format(" {0:0.00}", intensities[2]);
                }
                //row += "\n";
            }
            //Debug.Log(row);

            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Apply();
            }
            //Color[] tex = textures[0].GetPixels();
            //string mat = "";
            //for ( int i = 0; i < width; i++)
            //{
            //    for (int j = 0; j < height; j++)
            //    {
            //        mat += string.Format(" {0:0.00}", tex[j * width + i].b);
            //    }
            //    mat+= "\n";
            //}
            //Debug.Log(mat);
            return textures;
        }

    }

}
