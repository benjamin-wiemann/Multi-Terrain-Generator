
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
            NativeArray<TerrainInfo> terrainMap,        // out
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
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainInfo
    {
        public int4 TerrainIndices { get; set; }
        public float4 Intensity { get; set; }

        public TerrainInfo(int4 indices, float4 intensity)
        {
            TerrainIndices = indices;
            Intensity = intensity;
        }

        public int GetMaxIndex()
        {
            int res = 0;
            float val = 0f;
            for(int i = 0; i < 4; i++)
            {
                if(Intensity[i] > val )
                {
                    val = Intensity[i];
                    res = i;
                }
            }
            return res;
        }
    }

}
