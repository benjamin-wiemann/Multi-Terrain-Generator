using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using MultiTerrain.DebugTools;

namespace MultiTerrain
{

    /// <summary>
    /// Normalizes the height map 
    /// </summary>
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct NormalizeNoiseJob : IJobFor
    {

        private int _mapWidth;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _noiseMap;

        private float _maxNoiseHeight;

        private float _minNoiseHeight;

        public static void ScheduleParallel(            
            NativeArray<float> noiseMapIn,      //inout
            float maxNoiseValue,
            float minNoiseValue,
            int mapWidth,
            int mapHeight          
        )
        {
            NormalizeNoiseJob normalizeJob = new();
            normalizeJob._noiseMap = noiseMapIn;
            
            normalizeJob._maxNoiseHeight = maxNoiseValue;
            normalizeJob._minNoiseHeight = minNoiseValue;
            normalizeJob._mapWidth = mapWidth;
            if ( JobTools.Get()._runParallel )
                normalizeJob.ScheduleParallel(mapHeight, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
            {
                normalizeJob.Run(mapHeight);
            }
        }

        public void Execute(int y)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                _noiseMap[y * _mapWidth + x] = unlerp(_minNoiseHeight, _maxNoiseHeight, _noiseMap[y * _mapWidth + x]);
            }   
        }

    }
}