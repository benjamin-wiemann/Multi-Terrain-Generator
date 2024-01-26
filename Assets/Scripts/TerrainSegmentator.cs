
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;

namespace LiquidPlanet
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentator
    {
        
        public static void GetTerrainSegmentation(                        
            int width,
            int height,
            float resolution,
            uint seed,
            int numPatches,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            NativeList<TerrainTypeStruct> terrainTypes,  // inout
            NativeArray<int> terrainMap,        // out
            NativeArray<int2> coordinates,     // out
            NativeArray<int> terrainCounters // out
        )
        {
            NativeArray<float2> seedPoints = new(numPatches, Allocator.Persistent);
            GenerateRandomSeedPoints(seedPoints, seed, width, height, resolution);
                        
            TerrainSegmentationJob.ScheduleParallel(                
                seedPoints,
                terrainTypes,
                width,
                height,
                resolution,
                borderGranularity,                
                perlinOffset,
                perlinScale,
                terrainMap,
                terrainCounters);
            SortCoordinatesJob.ScheduleParallel(
                terrainMap,
                terrainCounters,
                height,
                coordinates);

            seedPoints.Dispose();
        }

        private static void GenerateRandomSeedPoints(
            NativeArray<float2> seedPoints, 
            uint seed, 
            int width, 
            int height, 
            float resolution)
        {
            Unity.Mathematics.Random random = new(seed);
            for (int i = 0; i < seedPoints.Length; i++)
            {
                float2 seedPoint = random.NextFloat2(new float2(width / resolution, height / resolution));
                seedPoints[i] = seedPoint;
            }

        }

    }

}
