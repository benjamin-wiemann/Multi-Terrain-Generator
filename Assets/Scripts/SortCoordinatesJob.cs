using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using LiquidPlanet.Helper;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct SortCoordinatesJob : IJobFor
    {
        [ReadOnly]
        NativeArray<int> _terrainSegmentation;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<int> _subMeshIndices;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        NativeArray<int2> _coordinates;

        int _width;

        public static JobHandle ScheduleParallel(
            NativeArray<int> terrainSegmentation,
            NativeArray<int> terrainCounters,
            int height,
            NativeArray<int2> coordinates)
        {
            SortCoordinatesJob job = new();
            job._terrainSegmentation = terrainSegmentation;
            job._coordinates = coordinates;
            job._width = terrainSegmentation.Length / height;
            job._subMeshIndices = new(terrainCounters.Length, Allocator.Persistent);
            job._subMeshIndices[0] = 0;
            for (int i = 1; i < terrainCounters.Length; i++)
            {
                job._subMeshIndices[i] = terrainCounters[i - 1] + job._subMeshIndices[i - 1];
            }
            //var handle = job.ScheduleParallel(
            //    height - 1,
            //    1,
            //    default);
            job.Run(height);
            JobHandle handle = default;            
            job._subMeshIndices.Dispose();
            return handle;
        }

        public void Execute(int y)
        {
            for(int x = 0; x < _width; x++)
            {
                int terrainIndex = _terrainSegmentation[y * _width + x ];
                int trianglePairIndex = NativeCollectionHelper.IncrementAt(_subMeshIndices, (uint) terrainIndex) - 1;
                _coordinates[trianglePairIndex] = new int2(x + 1, y + 1);
            }
        }
    }
}