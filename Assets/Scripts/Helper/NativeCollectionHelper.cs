using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;

using static Unity.Mathematics.math;

namespace MultiTerrain
{
    namespace Helper
    {
        
        public static class NativeCollectionHelper
        {

            [BurstCompile]
            public static int SelectClosest(float xPos, float yPos, int resolution, int width, NativeArray<int> integerMap)
            {
                int x = (int)round(xPos * resolution);
                int y = (int)round(yPos * resolution);
                return integerMap[y * width + x];
            }


            /// <summary>
            /// Increments NativeArray element at given index
            /// </summary>
            /// <param name="integers"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            [BurstCompile]
            public static unsafe int IncrementAt(NativeArray<int> integers, uint index)
            {
                int* arrayData = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(integers);
                if (index > integers.Length - 1)
                    throw new IndexOutOfRangeException();
                var idx = Interlocked.Increment(ref *(arrayData + index));
                return idx;
            }

            [BurstCompile]
            public static float Max(NativeArray<float> floats)
            {
                float maxNoiseValue = float.MinValue;                
                for (int i = 0; i < floats.Length; i++)
                {
                    if (maxNoiseValue < floats[i])
                    {
                        maxNoiseValue = floats[i];
                    }                    
                }
                return maxNoiseValue;
            }

            [BurstCompile]
            public static float Min(NativeArray<float> floats)
            {
                float minNoiseValue = float.MaxValue;
                for (int i = 0; i < floats.Length; i++)
                {
                    if (minNoiseValue > floats[i])
                    {
                        minNoiseValue = floats[i];
                    }
                }
                return minNoiseValue;
            }

        }
    }
}
