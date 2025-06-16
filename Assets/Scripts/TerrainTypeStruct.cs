using Unity.Mathematics;
using Unity.Collections;

/// <summary>
    /// Terrain type struct to be used with Burst and Jobs 
    /// </summary>
    [System.Serializable]
    public struct TerrainTypeStruct
    {
        public FixedString128Bytes Name { get; }
        public float3 Color { get; }
        public uint NumTrianglePairs { get; }
        public float Height { get; }
        public float NoiseScale { get; }
        public int NumOctaves { get; }
        public float Persistance { get; }
        public float Lacunarity { get; }
        public float HeightOffset { get; }

        public TerrainTypeStruct(
            string name, 
            float3 color,
            float height = 1,
            float noiseScale = 1,
            int numOctaves = 1,
            float persistance = 1,
            float lacunarity = 1,
            float heightOffset = 0,
            uint numTrianglePairs = 0
            )
        {
            Name = new FixedString128Bytes(name);
            Color = color;
            NumTrianglePairs = numTrianglePairs;
            Height      = height;
            NoiseScale = noiseScale;
            NumOctaves = numOctaves; 
            Persistance = persistance;
            Lacunarity = lacunarity;
            HeightOffset = heightOffset;
        }
        
    }