using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentationJob : IJobFor
    {
        int width;
        int height;
        int numPatches;
        int numTerrainTypes;
        float perlinOffset;
        float noiseScale;

        [ReadOnly]
        NativeArray<float2> seedPoints;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<int> segmentation;

        public static JobHandle ScheduleParallel(
            NativeArray<int> terrainSegmentation,
            NativeArray<float2> seedPoints,
            int width,
            int height,
            int numTerrainTypes,
            int numPatches,
            float perlinOffset,
            float perlinScale,
            JobHandle dependency
        )
        {
            TerrainSegmentationJob job = new();
            job.segmentation = terrainSegmentation;
            job.seedPoints = seedPoints;
            job.width = width;
            job.height = height;
            job.numTerrainTypes = numTerrainTypes;
            job.numPatches = numPatches;
            job.perlinOffset = perlinOffset;
            job.noiseScale = perlinScale;

            return job.ScheduleParallel(height, 1, default);
        }

        public void Execute( int y)
        {
            for (int x = 0; x < width; x++)
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

                segmentation[y * width + x] = minIndex % numTerrainTypes;
                
            }

        }


        //void ApplyTextureToPlane(Texture2D texture)
        //{
        //    Material material = GetComponent<Renderer>().material;
        //    material.mainTexture = texture;
        //}

    }
}
