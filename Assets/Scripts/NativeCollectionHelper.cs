using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;

using static Unity.Mathematics.math;

namespace LiquidPlanet
{
    public static class NativeCollectionHelper
    {
        /// <summary>
        /// Interpolates a float value from a NativeArray given a two dimensional coordinate
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="zPos"></param>
        /// <param name="resolution"></param>
        /// <param name="width"></param>
        /// <param name="floatMap"></param>
        /// <returns></returns>
        public static float SampleValueAt(float xPos, float zPos, int resolution, int width, NativeArray<float> floatMap)
        {
            float x = clamp(xPos * resolution, 0, width - 1);
            float z = clamp(zPos * resolution, 0, (int)(floatMap.Length / width) - 1);
            float lowXLowZ = floatMap[(int)floor(x) + (int)floor(z) * width];
            float lowXHighZ = floatMap[(int)floor(x) + (int)ceil(z) * width];
            float highXLowZ = floatMap[(int)ceil(x) + (int)floor(z) * width];
            float highXHighZ = floatMap[(int)ceil(x) + (int)ceil(z) * width];
            float lowX = lerp(lowXLowZ, lowXHighZ, abs(z) - floor(abs(z)));
            float highX = lerp(highXLowZ, highXHighZ, abs(z) - floor(abs(z)));
            return lerp(lowX, highX, abs(x) - floor(abs(x)));
        }

        public static int SelectClosest(float xPos, float yPos, int resolution, int width, NativeArray<int> integerMap)
        {
            int x = (int) round( xPos * resolution);
            int y = (int) round( yPos * resolution);
            return integerMap[ y * width + x];
        }

        /// <summary>
        /// Increments NativeArray element at given index
        /// </summary>
        /// <param name="integers"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static unsafe int IncrementAt(NativeArray<uint> integers, uint index) 
        {
            int* arrayData = (int*) NativeArrayUnsafeUtility.GetUnsafePtr(integers);
            if ( index > integers.Length -1 )
                throw new IndexOutOfRangeException();
            var idx = Interlocked.Increment( ref *(arrayData + index) );
            return idx;
        }
    }
}
