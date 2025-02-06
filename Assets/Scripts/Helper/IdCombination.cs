using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace MultiTerrain.Helper
{
    public static class IdCombination
    {
   
        public static void MapIdsToIndices(NativeList<TerrainTypeStruct> terrainList, int k, ref NativeHashMap<int, int> productToCombinationMap)
        {
            
            if (terrainList.Length < k)
            {
                throw new ArgumentException("Terrain list length must be greater than or equal to k.");
            }

            int n = terrainList.Length;

            float4 combination = new float4(0, 1, 2, 3); // Store current combination indices
            int indexCounter = 0;
            while (true)
            {
                // Calculate product of the current combination                
                int product = 1;
                for( int i = 0; i < k; i++)
                {
                    product *= terrainList[(int)combination[i]].PrimeId;
                }

                if (!productToCombinationMap.ContainsKey(product))
                {
                    productToCombinationMap[product] = indexCounter;
                    indexCounter++;
                }

                // Generate the next combination
                int t = k - 1;
                while (t >= 0 && combination[t] == n - k + t)
                {
                    t--;
                }

                if (t < 0)
                {
                    break; // No more combinations
                }

                combination[t]++;

                for (int i = t + 1; i < k; i++)
                {
                    combination[i] = combination[i - 1] + 1;
                }
            }
        }

    }
}