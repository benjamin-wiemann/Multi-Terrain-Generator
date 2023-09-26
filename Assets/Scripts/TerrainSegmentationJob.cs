using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace LiquidPlanet
{
    public struct TerrainSegmentationJob : IJobFor
    {
        int width;
        int height;
        int numPatches;
        float perlinOffset;
        float noiseScale;

        NativeArray<float2> seedPoints;
        int[,] voronoiDiagram;

        public static JobHandle ScheduleParallel(
            NativeArray<float> terrainSegmentation,
            NativeArray<float2> seedPoints,
            int width,
            int height,
            int numTerrains,
            float perlinOffset,
            float perlinScale,
            JobHandle dependency
        )
        {
            TerrainSegmentationJob job = new();
            job.width = width;
            job.height = height;
            job.numPatches = numTerrains;
            job.perlinOffset = perlinOffset;
            job.noiseScale = perlinScale;

            return job.ScheduleParallel(height, 1, default);
        }

        public void Execute( int index)
        {
            int[,] diagram = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float minDistance = float.MaxValue;
                    int minIndex = -1;

                    for (int i = 0; i < seedPoints.Length; i++)
                    {
                        Vector2 seed = seedPoints[i];

                        // Apply Perlin noise to perturb the position
                        float noiseX = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * noiseScale - 1; // Adjust the scale and range as needed
                        float noiseY = Mathf.PerlinNoise(x * 0.1f, y * 0.1f + perlinOffset) * noiseScale - 1; // Adjust the scale and range as needed

                        float distance = Vector2.Distance(new Vector2(x + noiseX, y + noiseY), seed);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            minIndex = i;
                        }
                    }

                    diagram[x, y] = minIndex;
                }
            }

        }

        Texture2D VisualizeVoronoiDiagram()
        {
            Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

            Texture2D texture = new Texture2D(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int patchIndex = voronoiDiagram[x, y] % colors.Length;
                    Color color = colors[patchIndex];
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        //void ApplyTextureToPlane(Texture2D texture)
        //{
        //    Material material = GetComponent<Renderer>().material;
        //    material.mainTexture = texture;
        //}

    }
}
