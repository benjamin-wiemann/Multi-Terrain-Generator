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
            var segmentation = generator.Segmentator.Segmentation;    
            GetComponent<Renderer>().material.mainTexture = VisualizeVoronoiDiagram( segmentation );
        }

        Texture2D VisualizeVoronoiDiagram( NativeArray<int> segmentation )
        {
            Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int patchIndex = segmentation[y * width + x] % colors.Length;
                    Color color = colors[patchIndex];
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
