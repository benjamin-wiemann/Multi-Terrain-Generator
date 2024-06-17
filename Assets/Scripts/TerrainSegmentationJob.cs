using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using LiquidPlanet.Helper;
using LiquidPlanet.DebugTools;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentationJob : IJobFor
    {
        uint _seedPointsX;
        uint _seedPointsY;
        int _trianglePairsX;
        int _trianglePairsY;
        float _width;
        float _height;
        float _borderGranularity;
        float _borderSmoothing;
        float _perlinOffset;
        float _noiseScale;
        uint _seed;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TerrainTypeStruct> _terrainTypes;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<TerrainInfo> _segmentation;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _terrainCounters;

        [ReadOnly, NativeDisableParallelForRestriction]
        NativeArray<int> _terrainIndices;

        public static void ScheduleParallel(
            NativeArray<TerrainTypeStruct> terrainTypes,
            NativeArray<int> terrainIndices,
            uint seed,
            uint seedPointsX,
            uint seedPointsY,
            int trianglePairsX,
            int trianglePairsY,
            int triangleResolution,
            float borderGranularity,
            float borderSmoothing,
            float perlinOffset,
            float perlinScale,
            NativeArray<TerrainInfo> terrainSegmentation,   // out
            NativeArray<int> terrainCounters        // out              
        )
        {
            TerrainSegmentationJob job = new();
            job._segmentation = terrainSegmentation;
            job._terrainIndices = terrainIndices;
            job._seedPointsX = seedPointsX;
            job._seedPointsY = seedPointsY;
            job._trianglePairsX = trianglePairsX;
            job._trianglePairsY = trianglePairsY;
            job._width = trianglePairsX / triangleResolution;
            job._height = trianglePairsY / triangleResolution;
            job._borderGranularity = borderGranularity;
            job._borderSmoothing = borderSmoothing;
            job._terrainTypes = terrainTypes;
            job._perlinOffset = perlinOffset;
            job._noiseScale = perlinScale;
            job._terrainCounters = terrainCounters;
            job._seed = seed;

            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(trianglePairsY, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                job.Run(trianglePairsY);
        }

        public void Execute( int y)
        {
            for (int x = 0; x < _trianglePairsX; x++)
            {
                float triangleX = (float) x / (float) _trianglePairsX;
                float triangleY = (float) y / (float) _trianglePairsY;
                float2 noiseAdd = new float2();
                noiseAdd.x = noise.cnoise(new float2(triangleX * _width * 0.1f * _borderGranularity, triangleY * _height * 0.1f * _borderGranularity)) * _noiseScale;
                noiseAdd.y = noise.cnoise(new float2(triangleX * _width * 0.1f * _borderGranularity, triangleY * _height * 0.1f * _borderGranularity + _perlinOffset)) * _noiseScale;

                float2 pos = float2(triangleX * _seedPointsX, triangleY * _seedPointsY) + noiseAdd;
                
                float2 cellIndex = floor( pos );                
                float2 inCellLocation = frac(pos);

                Float9 terrainShares = Float9.zero;
                //for (uint i = 0; i < 9; i++)
                //    terrainShares[i] = 8;
                Int9 indices = new Int9( - 1);

                float minDistance = 8f;
                int shiftSize = 1;
                for (int shiftX = -shiftSize; shiftX <= shiftSize; shiftX++)
                {
                    for (int shiftY = -shiftSize; shiftY <= shiftSize; shiftY++)
                    {   
                        float2 shift = float2(shiftX, shiftY);
                        float2 worleySeed = HashHelper.Hash2(cellIndex + shift + _seed) + shift;                        
                        float dist = length(worleySeed - inCellLocation);
                        int seedTerrainIndex = (int)(cellIndex.y + shiftY + 3) * (int)_seedPointsX + (int)(cellIndex.x + shiftX + 3);
                        int terrainIndex = _terrainIndices[seedTerrainIndex];
                        uint terrainIndexPosition = indices.Add( terrainIndex );
                        float h = smoothstep(-1.0f, 1.0f, (minDistance - dist) / _borderSmoothing);
                        minDistance = lerp(minDistance, dist, h);                        
                        Float9 dist9 = Float9.zero;
                        dist9[terrainIndexPosition] = 1;
                        terrainShares = Float9.lerp(terrainShares, dist9, h, indices.Length);
                    }
                }
                
               
                TerrainInfo terrainInfo = new TerrainInfo(indices, terrainShares);
                _segmentation[y * _trianglePairsX + x] = terrainInfo;
                if(x < _trianglePairsX && y < _trianglePairsY)
                {
                    int maxIndex = terrainInfo.GetMaxIndex();
                    // count the occurences of each terrain for each job execution
                    NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) maxIndex);                    
                }                
                
            }
        }


    }
}
