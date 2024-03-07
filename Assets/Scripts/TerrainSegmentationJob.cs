using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using LiquidPlanet.Helper;
using LiquidPlanet.DebugTools;
using System.Security.Cryptography;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentationJob : IJobFor
    {
        float _seedDensity;
        uint _seedResolution;
        int _width;
        int _height;
        float _borderGranularity;
        float _perlinOffset;
        float _noiseScale;
        float _resolution;
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
            uint seedPointResolution,
            int width,
            int height,
            float resolution,
            float borderGranularity,
            float perlinOffset,
            float perlinScale,
            NativeArray<TerrainInfo> terrainSegmentation,   // out
            NativeArray<int> terrainCounters        // out              
        )
        {
            TerrainSegmentationJob job = new();
            job._segmentation = terrainSegmentation;
            job._terrainIndices = terrainIndices;
            job._width = width;
            job._height = height;
            job._resolution = resolution;
            job._borderGranularity = borderGranularity;
            job._terrainTypes = terrainTypes;
            job._perlinOffset = perlinOffset;
            job._noiseScale = perlinScale;
            job._terrainCounters = terrainCounters;
            job._seedDensity = (float) seedPointResolution / (float) width;
            job._seedResolution = seedPointResolution;
            job._seed = seed;

            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(height, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                job.Run(height);
        }

        public void Execute( int y)
        {          
            //float gridStepSize = 1 / _seedDensity;
            for (int x = 0; x < _width; x++)
            {
                float xPos = (float) x / (float) _width;
                float yPos = (float) y / (float) _height;
                float2 noiseAdd = new float2();
                noiseAdd.x = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity)) * _noiseScale;
                noiseAdd.y = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity + _perlinOffset)) * _noiseScale;

                float2 pos = float2(x * _seedDensity, y * _seedDensity) + noiseAdd;
                // Which integer grid cell are we in?
                float2 cell = floor( pos );
                // Where are we within that cell [0-1)?
                float2 inCell = frac(pos);

                float4 minDistance = 0f;
                int4 indices = 0;              

                for (int shiftX = -1; shiftX <= 1; shiftX++)
                {
                    for (int shiftY = -1; shiftY <= 1; shiftY++)
                    {   
                        float2 shift = float2(shiftX, shiftY);
                        float2 worleySeed = HashHelper.Hash2(cell + shift + _seed) + shift;
                        float dist = length(worleySeed - inCell);
                        //float squaredDistance = dot(dist, dist);

                        int seedTerrainIndex = (int)(cell.y + shiftY + 2) * (int)_seedResolution + (int)(cell.x + shiftX + 2);
                        minDistance[_terrainIndices[seedTerrainIndex]] += exp2(-32.0f * dist) ;
                        
                        //minIndex = _terrainIndices[seedTerrainIndex];                            
                        
                    }
                }
                for ( int i = 0; i < 3; i++) 
                {
                    minDistance[i] = - (1.0f / 32.0f) * log2(max(minDistance[i], float.MinValue));
                }
                //minIndex = minIndex % 3;
                TerrainInfo terrainInfo = new (indices, minDistance);
                _segmentation[y * _width + x] = terrainInfo;
                if(x < _width && y < _height)
                {
                    // count the occurences of each terrain for each job execution
                    NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) terrainInfo.GetMaxIndex());                    
                }                
                
            }

        }


    }
}
