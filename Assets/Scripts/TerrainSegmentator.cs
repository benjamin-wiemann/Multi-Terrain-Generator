using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Reflection;
using MultiTerrain.Segmentation;
using System.Linq;

namespace MultiTerrain
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentator
    {
        
        public static void GetTerrainSegmentation(                        
            int trianglePairsX,
            int trianglePairsY,
            float width,
            float height,
            int resolution,
            uint seed,
            float seedPointDensity,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            float borderSmoothing,
            float terrainInclusionThreshold,
            NativeList<TerrainTypeStruct> terrainTypes,  // inout
            NativeArray<TerrainCombination> terrainMap,        // out
            NativeArray<int2> coordinates,     // out
            NativeArray<int> submeshCounters // out
        )
        {
            // Creating random terrain indices for the worley seed points. Overlap of 3 is need for each side.
            uint seedPointsX = (uint) round(width * seedPointDensity);
            uint seedPointsY = (uint) round(height * seedPointDensity);
            NativeArray<int> terrainIndices = new((int) ((seedPointsX + 6) * (seedPointsY + 6)), Allocator.Persistent);
            GenerateSeedTerrainIndices(terrainIndices, seed, terrainTypes);
            
            TerrainSegmentationJob.ScheduleParallel(       
                terrainIndices,
                terrainTypes.Length,
                seed,
                seedPointsX,
                seedPointsY,
                trianglePairsX,
                trianglePairsY,
                resolution,
                borderGranularity,
                borderSmoothing,
                perlinOffset,
                perlinScale,
                terrainInclusionThreshold,
                terrainMap,
                submeshCounters);
            SortCoordinatesJob.ScheduleParallel(
                terrainMap,
                submeshCounters,
                trianglePairsY,
                coordinates);

            terrainIndices.Dispose();
        }

        private static void GenerateSeedTerrainIndices(
            NativeArray<int> indices, 
            uint seed, 
            NativeList<TerrainTypeStruct> terrainTypes)
        {
            Unity.Mathematics.Random random = new(seed);
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = random.NextInt(terrainTypes.Length); 
            }

        }
                
    }

}
