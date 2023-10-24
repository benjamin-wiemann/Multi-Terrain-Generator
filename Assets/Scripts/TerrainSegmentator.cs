
using Unity.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using LiquidPlanet;
using Unity.Jobs;
using System.Collections.Generic;
using System;

namespace LiquidPlanet
{
    [BurstCompile]
    public struct TerrainSegmentator
    {
        
        public static void GetTerrainSegmentation(
            NativeArray<int> terrainMap,
            int width,
            int height,
            uint seed,
            NativeList<TerrainTypeUnmanaged> terrainTypes,
            int numPatches,
            float perlinOffset,
            float perlinScale,
            float borderGranularity
        )
        {
            NativeArray<float2> seedPoints = new(numPatches, Allocator.Persistent);
            GenerateRandomSeedPoints(seedPoints, seed, width, height);
            TerrainSegmentationJob.ScheduleParallel(
                terrainMap,
                seedPoints,
                width,
                height,
                borderGranularity,
                terrainTypes,
                perlinOffset,
                perlinScale,
                default).Complete();

            seedPoints.Dispose();
        }

        static void GenerateRandomSeedPoints(NativeArray<float2> seedPoints, uint seed, int width, int height)
        {
            Unity.Mathematics.Random random = new(seed);
            for (int i = 0; i < seedPoints.Length; i++)
            {
                float2 seedPoint = random.NextFloat2(new float2(width, height));
                seedPoints[i] = seedPoint;
            }

        }

    }

}
