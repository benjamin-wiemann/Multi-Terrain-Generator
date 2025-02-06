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
        NativeArray<TerrainWeighting> _terrainSegmentation;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<int> _subMeshIndices;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        NativeArray<int2> _coordinates;

        [ReadOnly]
        private NativeHashMap<int, int> _terrainIdsToIndices;

        int _width;
        private int _subMeshSplitLevel;

        public static void ScheduleParallel(
            NativeArray<TerrainWeighting> terrainSegmentation,
            NativeArray<int> subMeshCounters,
            int height,
            int subMeshSplitLevel,
            NativeHashMap<int, int> terrainIdsToIndices,
            NativeArray<int2> coordinates)
        {
            SortCoordinatesJob job = new();
            job._terrainSegmentation = terrainSegmentation;
            job._coordinates = coordinates;
            job._width = terrainSegmentation.Length / height;
            job._subMeshSplitLevel = subMeshSplitLevel;
            job._terrainIdsToIndices = terrainIdsToIndices;
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
                TerrainWeighting weighting = _terrainSegmentation[y * _width + x ];
                int terrainCombinationId = 1;
                for(int i = 0; i < _subMeshSplitLevel; i++)
                {
                    terrainCombinationId *= weighting.Ids[i];
                }
                int submeshIndex = _terrainIdsToIndices[terrainCombinationId];
                int trianglePairIndex = NativeCollectionHelper.IncrementAt(_subMeshIndices, (uint) submeshIndex) - 1;
                _coordinates[trianglePairIndex] = new int2(x + 1, y + 1);
            }
        }
    }
}