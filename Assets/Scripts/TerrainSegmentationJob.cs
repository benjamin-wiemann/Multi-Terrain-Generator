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
            //string output = "";
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

                Float9 minDistance = Float9.zero;
                Int9 indices = new Int9( - 1);

                for (int shiftX = -2; shiftX <= 2; shiftX++)
                {
                    for (int shiftY = -2; shiftY <= 2; shiftY++)
                    {   
                        float2 shift = float2(shiftX, shiftY);
                        float2 worleySeed = HashHelper.Hash2(cell + shift + _seed) + shift;
                        float dist = length(worleySeed - inCell);
                        int seedTerrainIndex = (int)(cell.y + shiftY + 3) * (int)_seedResolution + (int)(cell.x + shiftX + 3);
                        //uint neighborIndex = (uint)((shiftY + 1) * 3 + shiftX + 1);
                        uint terrainIndexPosition = indices.Add( _terrainIndices[seedTerrainIndex]);
                        minDistance[terrainIndexPosition] += exp2(-32.0f * dist);                           
                    }
                }
                for ( uint i = 0; i < indices.Length; i++) 
                {
                    minDistance[i] = - (1.0f / (32.0f)) * log2(minDistance[i]);
                }
                //output += string.Format(" {0:0.00}", minDistance[2]);
                TerrainInfo terrainInfo = new TerrainInfo(indices, minDistance);
                _segmentation[y * _width + x] = terrainInfo;
                if(x < _width && y < _height)
                {
                    int maxIndex = terrainInfo.GetMaxIndex();
                    // count the occurences of each terrain for each job execution
                    NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) maxIndex);                    
                }                
                
            }
            //Debug.Log(output);

        }


    }
}
