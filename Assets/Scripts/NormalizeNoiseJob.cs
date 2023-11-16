using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct NormalizeNoiseJob : IJobFor
    {

        private int _mapWidth;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _noiseMap;

        private float _maxNoiseHeight;

        private float _minNoiseHeight;

        public static JobHandle ScheduleParallel(            
            NativeArray<float> noiseMapIn,
            NativeArray<float> maxNoiseValues,
            NativeArray<float> minNoiseValues,
            int mapWidth,
            int mapHeight,
            JobHandle dependency            
        )
        {
            NormalizeNoiseJob normalizeJob = new();
            normalizeJob._noiseMap = noiseMapIn;
            float maxNoiseValue = float.MinValue;
            float minNoiseValue = float.MaxValue;
            for (int i = 0; i < maxNoiseValues.Length; i++)
            {
                if (maxNoiseValue < maxNoiseValues[i])
                {
                    maxNoiseValue = maxNoiseValues[i];
                }
                if (minNoiseValue > minNoiseValues[i])
                {
                    minNoiseValue = minNoiseValues[i];
                }
            }
            normalizeJob._maxNoiseHeight = maxNoiseValue;
            normalizeJob._minNoiseHeight = minNoiseValue;
            normalizeJob._mapWidth = mapWidth;
            return normalizeJob.ScheduleParallel(mapHeight, 1, dependency);
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