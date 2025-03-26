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


    }

}
