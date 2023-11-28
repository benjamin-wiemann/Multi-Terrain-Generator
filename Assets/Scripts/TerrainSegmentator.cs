
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
            uint seed,
            int numPatches,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            NativeList<TerrainTypeUnmanaged> terrainTypes,  // inout
            NativeArray<int> terrainMap,        // out
            NativeArray<int2> coordinates     // out
        )
        {
            NativeArray<float2> seedPoints = new(numPatches, Allocator.Persistent);
            GenerateRandomSeedPoints(seedPoints, seed, width, height);

            var terrainOccurenceCounters = new NativeArray<uint>(terrainTypes.Length, Allocator.Persistent);            
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                terrainOccurenceCounters[i] = 0;
            }
            TerrainSegmentationJob.ScheduleParallel(                
                seedPoints,
                terrainTypes,
                width,
                height,
                borderGranularity,                
                perlinOffset,
                perlinScale,
                default,
                terrainMap,
                terrainOccurenceCounters).Complete();
            SaveTerrainCounters(width, terrainTypes, terrainOccurenceCounters);
            terrainOccurenceCounters.Dispose();

            seedPoints.Dispose();
        }

        private static void SaveTerrainCounters(int width, NativeList<TerrainTypeUnmanaged> terrainTypes, NativeArray<uint> terrainOccurenceCounter)
        {
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                TerrainTypeUnmanaged type = new TerrainTypeUnmanaged( 
                    terrainTypes[i].Name,
                    terrainTypes[i].Color,
                    terrainOccurenceCounter[i]);          
                terrainTypes[i] = type;
            }
        }

        private static void GenerateRandomSeedPoints(NativeArray<float2> seedPoints, uint seed, int width, int height)
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
