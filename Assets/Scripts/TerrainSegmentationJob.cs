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
        int _width;
        int _height;
        float _borderGranularity;
        float _perlinOffset;
        float _noiseScale;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TerrainTypeUnmanaged> _terrainTypes;

        [ReadOnly]
        NativeArray<float2> _seedPoints;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<int> _segmentation;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _terrainCounters;

        public static JobHandle ScheduleParallel(
            NativeArray<int> terrainSegmentation,
            NativeArray<float2> seedPoints,
            int width,
            int height,
            float borderGranularity,
            NativeArray<TerrainTypeUnmanaged> terrainTypes,
            NativeArray<int> terrainCounters,
            float perlinOffset,
            float perlinScale,
            JobHandle dependency
        )
        {
            TerrainSegmentationJob job = new();
            job._segmentation = terrainSegmentation;
            job._seedPoints = seedPoints;
            job._width = width;
            job._height = height;
            job._borderGranularity = borderGranularity;
            job._terrainTypes = terrainTypes;
            job._perlinOffset = perlinOffset;
            job._noiseScale = perlinScale;

            // count the occurences of each terrain for each job execution
            job._terrainCounters = terrainCounters;
            //return job.ScheduleParallel(height, 1, default);
            job.Run(height);
            return default;
        }

        public void Execute( int y)
        {
            // Initialize terrain counters for every job
            for (int i = 0; i < _terrainTypes.Length; i++)
            {
                _terrainCounters[y * _terrainTypes.Length + i] = 0;
            }            
            for (int x = 0; x < _width; x++)
            {
                
                float minDistance = float.MaxValue;
                int minIndex = -1;

                for (int i = 0; i < _seedPoints.Length; i++)
                {
                    Vector2 seed = _seedPoints[i];

                    float noiseX = Mathf.PerlinNoise(x * 0.1f * _borderGranularity, y * 0.1f * _borderGranularity) * _noiseScale - 1; 
                    float noiseY = Mathf.PerlinNoise(x * 0.1f * _borderGranularity, y * 0.1f * _borderGranularity + _perlinOffset) * _noiseScale - 1; 

                    float distance = Vector2.Distance(new Vector2(x + noiseX, y + noiseY), seed);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minIndex = i;
                    }
                }
                int terrainIndex = minIndex % _terrainTypes.Length;
                _segmentation[y * _width + x] = terrainIndex;
                if(x < _width - 1 && y < _height -1)
                {                                                             
                    _terrainCounters[y * _terrainTypes.Length + terrainIndex]++;
                }                
                
            }

        }


    }
}
