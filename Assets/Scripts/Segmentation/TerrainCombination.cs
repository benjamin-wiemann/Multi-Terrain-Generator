using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MultiTerrain.Segmentation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainCombination
    {

        public int4 Ids;
        public float4 Weightings;

        public TerrainCombination( int4 indices, float4 weightings)
        {
            this.Ids = indices;
            this.Weightings = weightings;            
        }

        public int Length
        {
            get
            {
                int length = 0;
                for( int i = 0; i < 4; i++)
                {
                    if( Weightings[i] > 0)
                    {
                        length++;
                    }
                    else 
                    {
                        break;
                    }
                }
                return length;
            }            
        }

        public static unsafe int SizeInBytes { get => sizeof(int4) + sizeof(float4);} 


    }

}
