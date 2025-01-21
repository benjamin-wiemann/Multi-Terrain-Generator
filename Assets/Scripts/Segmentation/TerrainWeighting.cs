using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MultiTerrain.Segmentation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainWeighting
    {

        public int4 Ids;
        public float4 Intensities;

        public TerrainWeighting( int4 indices, float4 intensities)
        {
            this.Ids = indices;
            this.Intensities = intensities;            
        }

        public static unsafe int SizeInBytes { get => sizeof(int4) + sizeof(float4);} 

        // public int GetMaxIndex()
        // {
        //     int index = 0;
        //     float val = 0f;
        //     for (uint i = 0; i < Indices.Length; i++)
        //     {
        //         if (this.Intensities[i] > val )
        //         {
        //             val = this.Intensities[i];
        //             index = this.Indices[i];
        //         }
        //     }
        //     return index;
        // }

        // public int GetMinIndex()
        // {
        //     int index = 0;
        //     float val = float.MaxValue;
        //     for (uint i = 0; i < Indices.Length; i++)
        //     {
        //         if (this.Intensities[i] < val )
        //         {
        //             val = this.Intensities[i];
        //             index = this.Indices[i];
        //         }
        //     }
        //     return index;
        // }

    }

}
