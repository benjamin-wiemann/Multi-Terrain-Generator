
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace LiquidPlanet
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentator
    {
        
        public static void GetTerrainSegmentation(
            NativeArray<int> terrainMap,
            NativeArray<float3> points,
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

            var terrainOccurenceCounter = new NativeArray<int>(terrainTypes.Length, Allocator.Persistent);            
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                terrainOccurenceCounter[i] = 0;
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
                points,
                terrainOccurenceCounter).Complete();
            SaveTerrainCounters(width, terrainTypes, terrainOccurenceCounter);
            terrainOccurenceCounter.Dispose();
            seedPoints.Dispose();
        }

        private static void SaveTerrainCounters(int width, NativeList<TerrainTypeUnmanaged> terrainTypes, NativeArray<int> terrainOccurenceCounter)
        {
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                TerrainTypeUnmanaged type = terrainTypes[i];                
                type.NumTrianglePairs = terrainOccurenceCounter[i];                
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
