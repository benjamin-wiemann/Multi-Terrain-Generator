
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
            NativeList<TerrainTypeUnmanaged> terrainTypes,
            int numPatches,
            float perlinOffset,
            float perlinScale
        )
        {
            NativeArray<float2> seedPoints = new(numPatches, Allocator.Persistent);
            GenerateRandomSeedPoints(seedPoints, width, height);
            TerrainSegmentationJob.ScheduleParallel(
                terrainMap,
                seedPoints,
                width,
                height,
                terrainTypes.Length,
                numPatches,
                perlinOffset,
                perlinScale,
                default).Complete();

            seedPoints.Dispose();
        }

        static void GenerateRandomSeedPoints(NativeArray<float2> seedPoints, int width, int height)
        {
            var dateTime = DateTime.Now;
            Unity.Mathematics.Random random = new((uint) dateTime.Ticks);
            for (int i = 0; i < seedPoints.Length; i++)
            {
                float2 seed = random.NextFloat2(new float2(width, height));
                seedPoints[i] = seed;
            }

        }

    }

}
