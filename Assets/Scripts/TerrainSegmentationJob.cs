using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using LiquidPlanet.Helper;

namespace LiquidPlanet
{

    //[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
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
        NativeArray<uint> _terrainCounters;

        public static JobHandle ScheduleParallel(
            NativeArray<float2> seedPoints,
            NativeArray<TerrainTypeUnmanaged> terrainTypes,
            int width,
            int height,
            float borderGranularity,
            float perlinOffset,
            float perlinScale,
            JobHandle dependency,
            NativeArray<int> terrainSegmentation,   // out
            NativeArray<uint> terrainCounters        // out              
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
            job._terrainCounters = terrainCounters;

            return job.ScheduleParallel(height, 1, default);
            //job.Run(height);
            //return default;
        }

        public void Execute( int y)
        {                   
            for (int x = 0; x < _width; x++)
            {
                
                float minDistance = float.MaxValue;
                int minIndex = -1;

                for (int i = 0; i < _seedPoints.Length; i++)
                {
                    float2 seed = _seedPoints[i];

                    float noiseX = noise.cnoise(new float2(x * 0.1f * _borderGranularity, y * 0.1f * _borderGranularity)) * _noiseScale - 1; 
                    float noiseY = noise.cnoise(new float2(x * 0.1f * _borderGranularity, y * 0.1f * _borderGranularity + _perlinOffset)) * _noiseScale - 1; 

                    float dist = distance(new float2(x + noiseX, y + noiseY), seed);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        minIndex = i;
                    }
                }
                int terrainIndex = minIndex % _terrainTypes.Length;
                _segmentation[y * _width + x] = terrainIndex;
                if(x < _width - 1 && y < _height -1)
                {
                    // count the occurences of each terrain for each job execution
                    int index = NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) terrainIndex) - 1;                    
                }                
                
            }

        }


    }
}
