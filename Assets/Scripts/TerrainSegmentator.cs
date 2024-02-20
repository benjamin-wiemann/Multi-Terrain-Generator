
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System;
using Unity.Jobs;

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
            uint seedPointResolution,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            NativeList<TerrainTypeStruct> terrainTypes,  // inout
            NativeArray<int> terrainMap,        // out
            NativeArray<int2> coordinates,     // out
            NativeArray<int> terrainCounters // out
        )
        {
            // Creating random terrain indices for the worley seed points. Overlap of 2 is need for each side.
            NativeArray<int> terrainIndices = new((int) ((seedPointResolution + 4) * (seedPointResolution + 4)), Allocator.Persistent);
            GenerateSeedTerrainIndices(terrainIndices, seed, terrainTypes.Length);
                        
            TerrainSegmentationJob.ScheduleParallel(                
                terrainTypes,
                terrainIndices,
                seed,
                seedPointResolution,
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

            //seedPoints.Dispose();
        }

        private static void GenerateSeedTerrainIndices(
            NativeArray<int> indices, 
            uint seed, 
            int numTerrainTypes)
        {
            Unity.Mathematics.Random random = new(seed);
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = random.NextInt(numTerrainTypes);                
            }

        }

    }

}
