
using Unity.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using LiquidPlanet;
using Unity.Jobs;
using System.Collections.Generic;
using System;

namespace LiquidPlanet
{
    [BurstCompile]
    public struct TerrainSegmentator
    {
        NativeArray<int> _segmentation;

        public NativeArray<int> Segmentation { get => _segmentation; }


        public NativeArray<int> GetTerrainSegmentation(
            int width,
            int height,
            List<TerrainType> terrainTypes,
            int numPatches,
            float perlinOffset,
            float perlinScale
        )
        {
            if (_segmentation.IsCreated)
            {
                _segmentation.Dispose();
            }
            _segmentation = new NativeArray<int>(width * height, Allocator.Persistent);

            NativeArray<float2> seedPoints = new(numPatches, Allocator.Persistent);
            GenerateRandomSeedPoints(seedPoints, width, height);
            TerrainSegmentationJob.ScheduleParallel(
                _segmentation,
                seedPoints,
                width,
                height,
                terrainTypes.Count,
                numPatches,
                perlinOffset,
                perlinScale,
                default).Complete();

            seedPoints.Dispose();

            return _segmentation;
        }

        void GenerateRandomSeedPoints(NativeArray<float2> seedPoints, int width, int height)
        {
            var dateTime = DateTime.Now;
            Unity.Mathematics.Random random = new((uint) dateTime.Ticks);
            for (int i = 0; i < seedPoints.Length; i++)
            {
                float2 seed = random.NextFloat2(new float2(width, height));
                seedPoints[i] = seed;
            }

        }

        public void Dispose()
        {
            if( _segmentation.IsCreated )
                Segmentation.Dispose();
        }
    }

}
