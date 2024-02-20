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
        NativeArray<int> _segmentation;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _terrainCounters;

        [ReadOnly, NativeDisableParallelForRestriction]
        NativeArray<int> _terrainIndices;

        SmallXXHash _smallXXHash;

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
            NativeArray<int> terrainSegmentation,   // out
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

                float minDistance = 2.0f;
                int minIndex = 0;                

                //float2 noiseAdd = new float2();
                //noiseAdd.x = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity)) * _noiseScale - 1;
                //noiseAdd.y = noise.cnoise(new float2(xPos * 0.1f * _borderGranularity, yPos * 0.1f * _borderGranularity + _perlinOffset)) * _noiseScale - 1;

                for (int shiftX = -1; shiftX <= 1; shiftX++)
                {
                    for (int shiftY = -1; shiftY <= 1; shiftY++)
                    {   
                        float2 shift = float2(shiftX, shiftY);
                        float2 worleySeed = HashHelper.Hash2(cell + shift) + shift;
                        float2 dist = worleySeed - inCell;// - noiseAdd;
                        float squaredDistance = dot(dist, dist);

                        //minDistance = min(minDistance, squaredDistance);

                        if (squaredDistance < minDistance)
                        {
                            minDistance = squaredDistance;
                            int seedTerrainIndex = (int) (cell.y + shiftY + 2) * (int) _seedResolution + (int) (cell.x + shiftX + 2); 
                            minIndex = _terrainIndices[seedTerrainIndex];
                            //Debug.Log("Seed: " + seedTerrainIndex + " -> " + minIndex);
                        }
                    }
                }
                //int terrainIndex = (int) minIndex % _terrainTypes.Length;
                minIndex = minIndex % 3;
                _segmentation[y * _width + x] = minIndex;
                if(x < _width && y < _height)
                {
                    // count the occurences of each terrain for each job execution
                    NativeCollectionHelper.IncrementAt(_terrainCounters, (uint) minIndex);                    
                }                
                
            }

        }


    }
}
