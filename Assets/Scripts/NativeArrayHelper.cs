
using Unity.Collections;
using UnityEngine;
using static Unity.Mathematics.math;

namespace LiquidPlanet
{
    public class NativeArrayHelper
    {
        public static float SampleValueAt(float xPos, float zPos, int resolution, int numX, NativeArray<float> noiseMap)
        {
            float x = clamp(xPos * resolution, 0, numX - 1);
            float z = clamp(zPos * resolution, 0, (int)(noiseMap.Length / numX) - 1);
            float lowXLowZ = noiseMap[(int)floor(x) + (int)floor(z) * numX];
            float lowXHighZ = noiseMap[(int)floor(x) + (int)ceil(z) * numX];
            float highXLowZ = noiseMap[(int)ceil(x) + (int)floor(z) * numX];
            float highXHighZ = noiseMap[(int)ceil(x) + (int)ceil(z) * numX];
            float lowX = lerp(lowXLowZ, lowXHighZ, abs(z) - floor(abs(z)));
            float highX = lerp(highXLowZ, highXHighZ, abs(z) - floor(abs(z)));
            return lerp(lowX, highX, abs(x) - floor(abs(x)));
        }
    }
}
