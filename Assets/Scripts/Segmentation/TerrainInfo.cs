using Unity.Burst;

namespace MultiTerrain.Segmentation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainInfo
    {

        public Int9 Indices;
        public Float9 Intensities;

        public TerrainInfo( Int9 indices, Float9 intensities)
        {
            this.Indices = indices;
            this.Intensities = intensities;
            
        }

        public static int SizeInBytes { get => Int9.SizeInBytes + Float9.SizeInBytes; }

        public int GetMaxIndex()
        {
            int index = 0;
            float val = 0f;
            for (uint i = 0; i < Indices.Length; i++)
            {
                if (this.Intensities[i] > val )
                {
                    val = this.Intensities[i];
                    index = this.Indices[i];
                }
            }
            return index;
        }

        public int GetMinIndex()
        {
            int index = 0;
            float val = float.MaxValue;
            for (uint i = 0; i < Indices.Length; i++)
            {
                if (this.Intensities[i] < val )
                {
                    val = this.Intensities[i];
                    index = this.Indices[i];
                }
            }
            return index;
        }

    }

}
