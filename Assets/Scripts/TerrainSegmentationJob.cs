using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using MultiTerrain.Helper;
using MultiTerrain.DebugTools;
using MultiTerrain.Segmentation;

namespace MultiTerrain
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentationJob : IJobFor
    {
        int _numTerrainTypes;
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
        float _terrainInclusionThreshold;

        [WriteOnly, NativeDisableParallelForRestriction]
        NativeArray<TerrainCombination> _segmentation;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _submeshCounters;

        [ReadOnly, NativeDisableParallelForRestriction]
        NativeArray<int> _terrainIds;


        public static void ScheduleParallel(
            NativeArray<int> terrainIds,
            int numTerrainTypes,
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
            float terrainInclusionThreshold,
            NativeArray<TerrainCombination> terrainSegmentation,   // out
            NativeArray<int> submeshCounters        // out              
        )
        {
            TerrainSegmentationJob job = new();
            job._numTerrainTypes = numTerrainTypes;
            job._segmentation = terrainSegmentation;
            job._terrainIds = terrainIds;
            job._seedPointsX = seedPointsX;
            job._seedPointsY = seedPointsY;
            job._trianglePairsX = trianglePairsX;
            job._trianglePairsY = trianglePairsY;
            job._width = trianglePairsX / triangleResolution;
            job._height = trianglePairsY / triangleResolution;
            job._borderGranularity = borderGranularity;
            job._borderSmoothing = borderSmoothing;
            job._perlinOffset = perlinOffset;
            job._noiseScale = perlinScale;
            job._submeshCounters = submeshCounters;
            job._seed = seed;
            job._terrainInclusionThreshold = terrainInclusionThreshold;

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

                Float9 terrainShares = new Float9();
                Int9 ids = new Int9();

                float minDistance = 8f;
                int shiftSize = 2;
                for (int shiftX = -shiftSize; shiftX <= shiftSize; shiftX++)
                {
                    for (int shiftY = -shiftSize; shiftY <= shiftSize; shiftY++)
                    {   
                        float2 shift = float2(shiftX, shiftY);
                        float2 worleySeed = HashHelper.Hash2(cellIndex + shift + _seed) + shift;                        
                        float dist = length(worleySeed - inCellLocation);
                        int seedTerrainIndex = (int)(cellIndex.y + shiftY + 3) * (int)_seedPointsX + (int)(cellIndex.x + shiftX + 3);
                        int terrainId = _terrainIds[seedTerrainIndex];
                        uint terrainIdPosition = ids.Add( terrainId );
                        float h = smoothstep(-1.0f, 1.0f, (minDistance - dist) / _borderSmoothing);
                        minDistance = lerp(minDistance, dist, h);                        
                        Float9 dist9 = new Float9();
                        dist9[terrainIdPosition] = 1;
                        terrainShares = Float9.lerp(terrainShares, dist9, h, ids.Length);
                    }
                }
                TopKSorter sorter = new(ids, terrainShares);
                int4 top4Ids;
                float4 top4Weightings;
                int numTerrainIds = min(_numTerrainTypes, 4);
                int numAboveThreshold;
                sorter.GetKHighestValues( numTerrainIds, _terrainInclusionThreshold, out top4Ids, out top4Weightings, out numAboveThreshold);                
                if(x < _trianglePairsX && y < _trianglePairsY)
                {                    
                    NativeCollectionHelper.IncrementAt(_submeshCounters, (uint) numAboveThreshold - 1);                    
                } 
                // TopKSorter.SortTopKById( numTerrainIds, ref top4Ids, ref top4Weightings);
                TerrainCombination terrainCombi = new TerrainCombination(top4Ids, top4Weightings);
                _segmentation[y * _trianglePairsX + x] = terrainCombi;               
                
            }
        }


    }
}
