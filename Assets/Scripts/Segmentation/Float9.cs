using Unity.Burst;
using System.Runtime.CompilerServices;

namespace MultiTerrain.Segmentation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Float9
    {
        public float a;
        public float b;
        public float c;
        public float d;
        public float e;
        public float f;
        public float g;
        public float h;
        public float i;

        public static readonly Float9 zero;

        public static int SizeInBytes { get => sizeof(float) * 9; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float9(float a, float b, float c, float d, float e, float f, float g, float h, float i)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
            this.g = g;
            this.h = h;
            this.i = i;
        }

        public unsafe float this[uint index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > 9)
                    throw new System.ArgumentException("index must be between[0...8]");
#endif
                fixed (float* array = &a) { return ((float*)array)[index]; }
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > 9)
                    throw new System.ArgumentException("index must be between[0...8]");
#endif
                fixed (float* array = &a) { array[index] = value; }
            }
        }

        public static Float9 lerp(Float9 left, Float9 right, float delta, uint count = 9)
        {            
            Float9 result = Float9.zero;
            for (uint i = 0; i < count; i++)
                result[i] = (1 - delta) * left[i] + delta * right[i];

            return result;
        }

    }

}
