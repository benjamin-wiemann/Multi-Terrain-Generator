
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.IO;

namespace LiquidPlanet
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainSegmentator
    {
        
        public static void GetTerrainSegmentation(                        
            int width,
            int height,
            float resolution,
            uint seed,
            uint seedPointResolution,
            float perlinOffset,
            float perlinScale,
            float borderGranularity,
            NativeList<TerrainTypeStruct> terrainTypes,  // inout
            NativeArray<TerrainInfo> terrainMap,        // out
            NativeArray<int2> coordinates,     // out
            NativeArray<int> terrainCounters // out
        )
        {
            // Creating random terrain indices for the worley seed points. Overlap of 2 is need for each side.
            NativeArray<int> terrainIndices = new((int) ((seedPointResolution + 4) * (seedPointResolution + 4)), Allocator.Persistent);
            GenerateSeedTerrainIndices(terrainIndices, seed, terrainTypes.Length);
                        
            TerrainSegmentationJob.ScheduleParallel(                
                terrainTypes,
                terrainIndices,
                seed,
                seedPointResolution,
                width,
                height,
                resolution,
                borderGranularity,                
                perlinOffset,
                perlinScale,
                terrainMap,
                terrainCounters);
            SortCoordinatesJob.ScheduleParallel(
                terrainMap,
                terrainCounters,
                height,
                coordinates);

            //seedPoints.Dispose();
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
    public struct IntFloat
    {
        public int x;
        public float y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFloat(int x, float y)
        {
            this.x = x; 
            this.y = y;
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
        }

        public unsafe int this[uint index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > 9)
                    throw new System.ArgumentException("index must be between[0...8]");
#endif
                fixed (int* array = &a) { return ((int*)array)[index]; }
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (index > 9)
                    throw new System.ArgumentException("index must be between[0...8]");
#endif
                fixed (int* array = &a) { array[index] = value; }
            }
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
            for (uint i = 0; i < 4; i++)
            {
                if (this.Intensities[i] > val)
                {
                    val = this.Intensities[i];
                    index = this.Indices[i];
                }
            }
            return index;
        }

    }

}
