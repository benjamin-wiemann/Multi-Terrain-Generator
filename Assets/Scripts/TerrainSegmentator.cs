
using Unity.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using LiquidPlanet;
using Unity.Jobs;

[BurstCompile]
public struct TerrainSegmentator
{
    NativeArray<float> terrainSegments;

    NativeArray<float> GetTerrainSegmentation(
        int width,
        int height,
        int numPatches,
        float perlinOffset,
        float perlinScale
    )
    {
        if( terrainSegments.IsCreated )
        {
            terrainSegments.Dispose();
        }
        terrainSegments = new NativeArray<float>( width * height, Allocator.Persistent );

        NativeArray<float2> seedPoints = new( numPatches, Allocator.Temp );
        GenerateRandomSeedPoints(seedPoints, numPatches, width, height);
        TerrainSegmentationJob.ScheduleParallel(
            terrainSegments,
            seedPoints,
            width,
            height,
            numPatches,
            perlinOffset,
            perlinScale,
            default).Complete();

        seedPoints.Dispose();

        return terrainSegments;
    }

    void GenerateRandomSeedPoints(NativeArray<float2> seedPoints, int numPatches, int width, int height)
    {
        Unity.Mathematics.Random random = new();
        for (int i = 0; i < numPatches; i++)
        {
            float2 seed = random.NextFloat2(new float2(width, height));
            seedPoints[i] = seed;
        }

    }
}
