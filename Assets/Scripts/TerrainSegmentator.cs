
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
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
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

            var terrainOccurenceCounter = new NativeArray<int>(terrainTypes.Length * height, Allocator.Persistent);
            TerrainSegmentationJob.ScheduleParallel(
                terrainMap,
                seedPoints,
                width,
                height,
                borderGranularity,
                terrainTypes,
                terrainOccurenceCounter,
                perlinOffset,
                perlinScale,
                default).Complete();
            AddUpTerrainCounters(width, terrainTypes, terrainOccurenceCounter);
            terrainOccurenceCounter.Dispose();
            seedPoints.Dispose();
        }

        private static void AddUpTerrainCounters(int width, NativeList<TerrainTypeUnmanaged> terrainTypes, NativeArray<int> terrainOccurenceCounter)
        {
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                TerrainTypeUnmanaged type = terrainTypes[i];
                for (int j = 0; j < width; j++)
                {
                    type.NumTrianglePairs += terrainOccurenceCounter[j * terrainTypes.Length + i];
                }
                terrainTypes[i] = type;
            }
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
