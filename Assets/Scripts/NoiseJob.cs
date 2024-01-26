using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using LiquidPlanet.Debug;

namespace LiquidPlanet
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

        private float2 _offset;

        private float _resolution;

        private bool _debug;

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
        private NativeArray<int> _terrainMap;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TerrainTypeStruct> _terrainTypes;

        public static void ScheduleParallel( 
            NativeArray<int> terrainMap,
            NativeArray<TerrainTypeStruct> terrainTypes,
            int mapWidth,
            int mapHeight,
            uint seed,
            float scale,
            int numOctaves,
            float persistance,
            float lacunarity,
            float2 offset,
            float resolution,
            bool debug,
            NativeArray<float> noiseMap,      // out
            NativeArray<float> maxNoiseHeights, // out
            NativeArray<float> minNoiseHeights  // out
        )
        {
            NoiseJob noiseJob = new();

            noiseJob._terrainMap = terrainMap;
            noiseJob._terrainTypes = terrainTypes;
            noiseJob._noiseScale = scale;
            noiseJob._mapHeight = mapHeight;
            noiseJob._mapWidth = mapWidth;
            noiseJob._numOctaves = numOctaves;            
            noiseJob._noiseMap = noiseMap;
            noiseJob._maxNoiseHeights = maxNoiseHeights;
            noiseJob._minNoiseHeights = minNoiseHeights;            
            noiseJob._persistance = persistance;
            noiseJob._lacunarity = lacunarity;
            noiseJob._offset = offset;
            noiseJob._resolution = resolution;
            noiseJob._debug = debug;

            noiseJob._octaveOffsets = new(numOctaves, Allocator.Persistent);
            noiseJob._octaveOffsetsPerTerrain = new(terrainTypes.Length, Allocator.Persistent);
            Unity.Mathematics.Random prng = new(seed);
            for (int i = 0; i < numOctaves; i++)
            {
                float offsetX = prng.NextFloat(-100000, 100000) + offset.x;
                float offsetY = prng.NextFloat(-100000, 100000) + offset.y;
                noiseJob._octaveOffsets[i] = new float2(offsetX, offsetY);
            }
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                noiseJob._octaveOffsetsPerTerrain[i] = new(terrainTypes[i].NumOctaves, Allocator.Persistent);
                for(int j = 0; j < terrainTypes[i].NumOctaves; j++ )
                {
                    float offsetX = prng.NextFloat(-100000, 100000) + offset.x;
                    float offsetY = prng.NextFloat(-100000, 100000) + offset.y;
                    var terrainOctaveOffsets = noiseJob._octaveOffsetsPerTerrain[i];
                    terrainOctaveOffsets[j] = new float2(offsetX, offsetY);
                }
            }

            if ( JobTools.Get()._runParallel)
                noiseJob.ScheduleParallel(mapHeight, (int) JobTools.Get()._batchCountInRow, default).Complete();
            else
                noiseJob.Run(mapHeight);            
            noiseJob._octaveOffsets.Dispose();
            for (int i = 0; i < terrainTypes.Length; i++)
            {
                noiseJob._octaveOffsetsPerTerrain[i].Dispose();
            }
            noiseJob._octaveOffsetsPerTerrain.Dispose();
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
                if (_debug)
                {
                    _noiseMap[y * _mapWidth + x] = sin(2 * PI * _lacunarity * x / _mapWidth) + sin(2 * PI * _lacunarity * y / _mapHeight);
                }
                else
                {
                    // clamp coordinates for terrain map since its dimensions are 1 unit smaller than the dimensions of the height map.
                    int terrainY = clamp(y, 0, _mapHeight - 2);
                    int terrainX = clamp(x, 0, _mapWidth - 2);
                    int terrainIndex = _terrainMap[terrainY * (_mapWidth - 1) + terrainX];
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    //int octaves = _numOctaves + _terrainTypes[terrainIndex].NumOctaves; 

                    for (int i = 0; i < _numOctaves; i++)
                    {
                        float sampleX = (x - halfWidth) / (_resolution * _noiseScale) * frequency + _octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / (_resolution * _noiseScale) * frequency + _octaveOffsets[i].y;

                        float perlinValue = noise.cnoise(new float2(sampleX, sampleY)) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= _persistance;
                        frequency *= _lacunarity;
                    }
                    for (int i = 0; i < _octaveOffsetsPerTerrain[terrainIndex].Length; i++)
                    {
                        float sampleX = (x - halfWidth) / (_resolution * _noiseScale) * frequency + _octaveOffsetsPerTerrain[terrainIndex][i].x;
                        float sampleY = (y - halfHeight) / (_resolution * _noiseScale) * frequency + _octaveOffsetsPerTerrain[terrainIndex][i].y;

                        float perlinValue = noise.cnoise(new float2(sampleX, sampleY)) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= _terrainTypes[terrainIndex].Persistance;
                        frequency *= _terrainTypes[terrainIndex].Lacunarity;
                    }
                    noiseHeight += _terrainTypes[terrainIndex].HeightOffset;
                    if (noiseHeight > _maxNoiseHeights[y])
                    {
                        _maxNoiseHeights[y] = noiseHeight;
                    }
                    else if (noiseHeight < _minNoiseHeights[y])
                    {
                        _minNoiseHeights[y] = noiseHeight;
                    }                
                    _noiseMap[y * _mapWidth + x] = noiseHeight;
                }
                
            }
            
        }

    }
}