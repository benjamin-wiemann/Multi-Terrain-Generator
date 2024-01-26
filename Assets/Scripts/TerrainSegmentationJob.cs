using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using LiquidPlanet.Helper;
using LiquidPlanet.Debug;

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
        float _resolution;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TerrainTypeStruct> _terrainTypes;

        [ReadOnly]
        NativeArray<float2> _seedPoints;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<int> _segmentation;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _terrainCounters;

        public static void ScheduleParallel(
            NativeArray<float2> seedPoints,
            NativeArray<TerrainTypeStruct> terrainTypes,
            int width,
            int height,
            float resolution,
            float borderGranularity,
            float perlinOffset,
            float perlinScale,
            NativeArray<int> terrainSegmentation,   // out
            NativeArray<int> terrainCounters        // out              
        )
        {
            TerrainSegmentationJob job = new();
            job._segmentation = terrainSegmentation;
            job._seedPoints = seedPoints;
            job._width = width;
            job._height = height;
            job._resolution = resolution;
            job._borderGranularity = borderGranularity;
            job._terrainTypes = terrainTypes;
            job._perlinOffset = perlinOffset;
            job._noiseScale = perlinScale;
            job._terrainCounters = terrainCounters;

            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(height, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                job.Run(height);
        }

        public void Execute( int y)
        {                   
            for (int x = 0; x < _width; x++)
            {
                
                float minDistance = float.MaxValue;
                int minIndex = -1;
                float xPos = x / _resolution;
                float yPos = y / _resolution;

                for (int i = 0; i < _seedPoints.Length; i++)
                {
                    float2 seed = _seedPoints[i];

                    float noiseX = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity)) * _noiseScale - 1; 
                    float noiseY = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity + _perlinOffset)) * _noiseScale - 1; 

                    float dist = distance(new float2(xPos + noiseX, yPos + noiseY), seed);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        minIndex = i;
                    }
                }
                int terrainIndex = minIndex % _terrainTypes.Length;
                _segmentation[y * _width + x] = terrainIndex;
                if(x < _width && y < _height)
                {
                    // count the occurences of each terrain for each job execution
                    int index = NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) terrainIndex) - 1;                    
                }                
                
            }

        }


    }
}
