using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using static Unity.Mathematics.math;

namespace LiquidPlanet
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentator
    {

        private const uint worleyBorderPadding = 3;
        
        public static void GetTerrainSegmentation(                        
            int trianglePairsX,
            int trianglePairsY,
            float width,
            float height,
            int resolution,
            uint seed,
            float seedPointDensity,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            float borderSmoothing,
            NativeList<TerrainTypeStruct> terrainTypes,  // inout
            NativeArray<TerrainInfo> terrainMap,        // out
            NativeArray<int2> coordinates,     // out
            NativeArray<int> terrainCounters // out
        )
        {
            // Creating random terrain indices for the worley seed points. Overlap of 3 is need for each side.
            uint seedPointsX = (uint)round(width * seedPointDensity);
            uint seedPointsY = (uint)round(height * seedPointDensity);
            NativeArray<int> terrainIndices = new((int) ((seedPointsX + 6) * (seedPointsY + 6)), Allocator.Persistent);
            GenerateSeedTerrainIndices(terrainIndices, seed, terrainTypes.Length);
            
            TerrainSegmentationJob.ScheduleParallel(                
                terrainTypes,
                terrainIndices,
                seed,
                seedPointsX,
                seedPointsY,
                trianglePairsX,
                trianglePairsY,
                resolution,
                borderGranularity,
                borderSmoothing,
                perlinOffset,
                perlinScale,
                terrainMap,
                terrainCounters);
            SortCoordinatesJob.ScheduleParallel(
                terrainMap,
                terrainCounters,
                trianglePairsY,
                coordinates);

            terrainIndices.Dispose();
        }

        private static void GenerateSeedTerrainIndices(
            NativeArray<int> indices, 
            uint seed, 
            int numTerrainTypes)
        {
            Unity.Mathematics.Random random = new(seed);
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = random.NextInt(numTerrainTypes);                
            }

        }
                
    }

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

        public static readonly Int9 zero;

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
            _pointer = 0;
        }

        public Int9(int init)
        {            
            this.a = init;
            this.b = init;
            this.c = init;
            this.d = init;
            this.e = init;
            this.f = init;
            this.g = init;
            this.h = init;
            this.i = init;
            _pointer = 0;
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
                fixed (int* array = &a) { array[index] = value; }                
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
            _pointer++;
            this[_pointer - 1] = v;
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
