using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using MultiTerrain.DebugTools;
using MultiTerrain.Segmentation;

namespace MultiTerrain
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct NoiseJob : IJobFor
    {

        private int _mapWidth;

        private int _mapHeight;

        private float _noiseScale;

        private int _numOctaves;

        private float _persistance;

        private float _lacunarity;

        private float _resolution;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _noiseMap;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float2> _octaveOffsets;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<NativeArray<float2>> _octaveOffsetsPerTerrain;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _maxNoiseHeights;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _minNoiseHeights;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        private NativeArray<TerrainCombination> _terrainMap;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TerrainTypeStruct> _terrainTypes;

        [ReadOnly, NativeDisableContainerSafetyRestriction]
        private NativeHashMap<int, int> _terrainIdsToIndices;

        public static void ScheduleParallel( 
            NativeArray<TerrainCombination> terrainMap,
            NativeArray<TerrainTypeStruct> terrainTypes,
            NativeHashMap<int, int> terrainIdsToIndices,
            int mapWidth,
            int mapHeight,
            float scale,
            int numOctaves,
            float persistance,
            float lacunarity,
            float resolution,
            NativeArray<float> noiseMap,      // out
            NativeArray<float> maxNoiseHeights, // out
            NativeArray<float> minNoiseHeights  // out
        )
        {
            NoiseJob noiseJob = new();

            noiseJob._terrainMap = terrainMap;
            noiseJob._terrainTypes = terrainTypes;
            noiseJob._terrainIdsToIndices = terrainIdsToIndices;
            noiseJob._noiseScale = scale;
            noiseJob._mapHeight = mapHeight;
            noiseJob._mapWidth = mapWidth;
            noiseJob._numOctaves = numOctaves;            
            noiseJob._noiseMap = noiseMap;
            noiseJob._maxNoiseHeights = maxNoiseHeights;
            noiseJob._minNoiseHeights = minNoiseHeights;            
            noiseJob._persistance = persistance;
            noiseJob._lacunarity = lacunarity;
            noiseJob._resolution = resolution;

            if ( JobTools.Get()._runParallel)
                noiseJob.ScheduleParallel(mapHeight, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                noiseJob.Run(mapHeight);            
            
        }

        public void Execute(int y)
        {         
            
            if (_noiseScale <= 0)
            {
                _noiseScale = 0.0001f;
            }

            _maxNoiseHeights[y] = float.MinValue;
            _minNoiseHeights[y] = float.MaxValue;

            float halfWidth = _mapWidth / 2f;
            float halfHeight = _mapHeight / 2f;

            for (int x = 0; x < _mapWidth; x++)
            {
                // clamp coordinates for terrain map since its dimensions are 1 unit smaller than the dimensions of the height map.
                int terrainY = clamp(y, 0, _mapHeight - 2);
                int terrainX = clamp(x, 0, _mapWidth - 2);                
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                float2 coordinate = 0;
                for (int i = 0; i < _numOctaves; i++)
                {
                    amplitude *= _persistance;
                    frequency *= _lacunarity;
                    coordinate.x = (x - halfWidth) / (_resolution * _noiseScale) * frequency;
                    coordinate.y = (y - halfHeight) / (_resolution * _noiseScale) * frequency;

                    float perlinValue = noise.cnoise(coordinate) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                                        
                }

                TerrainCombination info = _terrainMap[terrainY * (_mapWidth - 1) + terrainX];
                for (int j = 0; j < min(_terrainTypes.Length, 4); j++)
                {
                    int terrainIndex = _terrainIdsToIndices[info.Ids[j]];
                    float terrainAmplitude = amplitude;
                    float terrainFrequency = frequency;
                    float terrainNoiseHeight = 0;
                    for (int i = 0; i < _terrainTypes[terrainIndex].NumOctaves; i++)
                    {
                        terrainAmplitude *= _terrainTypes[terrainIndex].Persistance;
                        terrainFrequency *= _terrainTypes[terrainIndex].Lacunarity;
                        float sampleX = (x - halfWidth) / (_resolution * _terrainTypes[terrainIndex].NoiseScale) * terrainFrequency;
                        float sampleY = (y - halfHeight) / (_resolution * _terrainTypes[terrainIndex].NoiseScale) * terrainFrequency;
                        float perlinValue = noise.cnoise(new float2(sampleX, sampleY)) * 2 - 1;
                        terrainNoiseHeight += perlinValue * terrainAmplitude;
                    }
                    noiseHeight += (terrainNoiseHeight * _terrainTypes[terrainIndex].Height + _terrainTypes[terrainIndex].HeightOffset) * info.Intensities[j];
                }

                _maxNoiseHeights[y] = max(noiseHeight, _maxNoiseHeights[y]);
                _minNoiseHeights[y] = min(noiseHeight, _minNoiseHeights[y]);
                                    
                _noiseMap[y * _mapWidth + x] = noiseHeight;                
                
            }
            
        }

    }
}