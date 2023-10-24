using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentationJob : IJobFor
    {
        int width;
        int height;
        float borderGranularity;
        float perlinOffset;
        float noiseScale;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TerrainTypeUnmanaged> terrainTypes;

        [ReadOnly]
        NativeArray<float2> seedPoints;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<int> segmentation;

        public static JobHandle ScheduleParallel(
            NativeArray<int> terrainSegmentation,
            NativeArray<float2> seedPoints,
            int width,
            int height,
            float borderGranularity,
            NativeArray<TerrainTypeUnmanaged> terrainTypes,
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
            job.borderGranularity = borderGranularity;
            job.terrainTypes = terrainTypes;
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

                    float noiseX = Mathf.PerlinNoise(x * 0.1f * borderGranularity, y * 0.1f * borderGranularity) * noiseScale - 1; 
                    float noiseY = Mathf.PerlinNoise(x * 0.1f * borderGranularity, y * 0.1f * borderGranularity + perlinOffset) * noiseScale - 1; 

                    float distance = Vector2.Distance(new Vector2(x + noiseX, y + noiseY), seed);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minIndex = i;
                    }
                }
                int terrainIndex = minIndex % terrainTypes.Length;
                segmentation[y * width + x] = terrainIndex;
                if(x < width - 1 && y < height -1)
                {
                    TerrainTypeUnmanaged type = terrainTypes[terrainIndex];
                    type.IncrementNumTrianglePairs();
                    terrainTypes[terrainIndex] = type;
                }                
                
            }

        }


    }
}
