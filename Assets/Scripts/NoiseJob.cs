using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using System.Net.NetworkInformation;

namespace LiquidPlanet
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct NoiseJob : IJobFor
    {

        private int mapWidth;

        private int mapHeight;

        private uint seed;

        private float scale;

        private int octaves;

        private float persistance;

        private float lacunarity;

        private float2 offset;

        //NativeArray<float> NoiseMap { set=>_noiseMap = value; }
        
        [WriteOnly, NativeDisableContainerSafetyRestriction]
        private NativeArray<float> _noiseMap;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float2> octaveOffsets;

        //NativeArray<float> noiseMap = new(mapWidth * mapHeight, Allocator.Persistent);

        public static JobHandle ScheduleParallel(
            NativeArray<float> noiseMapIn,
            int mapWidth,
            int mapHeight,
            uint seed,
            float scale,
            int octaves,
            float persistance,
            float lacunarity,
            float2 offset,
            JobHandle dependency
        )
        {
            NoiseJob noiseJob = new();
            noiseJob._noiseMap = noiseMapIn;

            noiseJob.seed = seed;
            noiseJob.scale = scale;
            noiseJob.mapHeight = mapHeight;
            noiseJob.mapWidth = mapWidth;
            noiseJob.scale = scale;
            noiseJob.octaves = octaves;
            noiseJob.octaveOffsets = new(octaves, Allocator.Persistent);
            Unity.Mathematics.Random prng = new(seed);
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.NextFloat(-100000, 100000) + offset.x;
                float offsetY = prng.NextFloat(-100000, 100000) + offset.y;
                noiseJob.octaveOffsets[i] = new float2(offsetX, offsetY);
            }
            noiseJob.persistance = persistance;
            noiseJob.lacunarity = lacunarity;
            noiseJob.offset = offset;
            return noiseJob.ScheduleParallel(mapHeight, 1, dependency);
        }

        public void Execute(int y)
        {         
            
            if (scale <= 0)
            {
                scale = 0.0001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;

            for (int x = 0; x < mapWidth; x++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = noise.cnoise(new float2(sampleX, sampleY)) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                _noiseMap[y * mapWidth + x] = noiseHeight;
            }
            
        }

    }
}