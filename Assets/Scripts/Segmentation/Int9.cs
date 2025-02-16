using Unity.Burst;
using System.Runtime.CompilerServices;

namespace MultiTerrain.Segmentation
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Int9
    {
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;
        public int f;
        public int g;
        public int h;
        public int i;

        private uint _pointer;

        public uint Length {  get { return _pointer; } }

        public static int SizeInBytes { get=> sizeof(int) * 9 + sizeof(uint); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int9(int a, int b, int c, int d, int e, int f, int g, int h, int i)
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
            _pointer = 9;
        }

        public unsafe int this[uint index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > (((int) this.Length) - 1))
                    throw new System.ArgumentException("index must be between[0..." + (((int)this.Length) - 1) + "]");
#endif
                fixed (int* array = &a) { return ((int*)array)[index]; }
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > this.Length )
                    throw new System.ArgumentException("index must be between[0..." + this.Length + "]");
                else if (index > 8)
                    throw new System.ArgumentException("index must be between[0...8]");
#endif
                fixed (int* array = &a) { array[index] = value; _pointer = _pointer == index ? _pointer + 1 : _pointer;  }                
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Add(int v)
        {
            for(uint i = 0; i < _pointer; i++)
            {
                if (this[i] == v)
                    return i;
            }
            this[_pointer ] = v;
            return _pointer - 1;
        }

        public static Int9 operator -(Int9 a, int b)
        {
            for (uint i = 0; i < 9; i++)
            {
                a[i] -= b;
            }
            return a;
        }
    }

}
