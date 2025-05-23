using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using MultiTerrain.Helper;
using MultiTerrain.DebugTools;
using MultiTerrain.Segmentation;

namespace MultiTerrain
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct SortCoordinatesJob : IJobFor
    {
        [ReadOnly]
        NativeArray<TerrainCombination> _terrainSegmentation;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<int> _subMeshIndices;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        NativeArray<int2> _coordinates;

        int _width;


        public static void ScheduleParallel(
            NativeArray<TerrainCombination> terrainSegmentation,
            NativeArray<int> subMeshCounters,
            int height,
            NativeArray<int2> coordinates)
        {
            SortCoordinatesJob job = new();
            job._terrainSegmentation = terrainSegmentation;
            job._coordinates = coordinates;
            job._width = terrainSegmentation.Length / height;
            job._subMeshIndices = new(subMeshCounters.Length, Allocator.Persistent);
            job._subMeshIndices[0] = 0;
            
            for (int i = 1; i < subMeshCounters.Length; i++)
            {
                int subMeshSize = subMeshCounters[i - 1];
                job._subMeshIndices[i] = job._subMeshIndices[i - 1] + subMeshSize;
                // at this point, it's possible that _subMeshIndices will contain the same indices multiple times in a row
            }

            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(height, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                job.Run(height);
            
            job._subMeshIndices.Dispose();            
        }

        public void Execute(int y)
        {
            for(int x = 0; x < _width; x++)
            {
                TerrainCombination combination = _terrainSegmentation[y * _width + x ];
                int submeshIndex = combination.Length - 1;                
                int trianglePairIndex = NativeCollectionHelper.IncrementAt(_subMeshIndices, (uint) submeshIndex) - 1;
                _coordinates[trianglePairIndex] = new int2(x + 1, y + 1);
            }
        }
    }
}