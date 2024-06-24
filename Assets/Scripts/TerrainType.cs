using Unity.Collections;
using UnityEngine;
using System;
using Unity.Mathematics;

namespace LiquidPlanet
{

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
        public uint HeigthSeed { get; }
        public float HeightOffset { get; }

        public TerrainTypeStruct(
            string name, 
            float3 color,
            float height = 1,
            float noiseScale = 1,
            int numOctaves = 1,
            float persistance = 1,
            float lacunarity = 1,
            uint heigthSeed = 1,
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
            HeigthSeed = heigthSeed;
            HeightOffset = heightOffset;
        }

        public static TerrainTypeStruct Convert(TerrainType type)
        {
            return new TerrainTypeStruct(
                type._name, 
                new float3(type._color.r, type._color.g, type._color.b),
                type._height,
                type._noiseScale,
                type._numOctaves,
                type._persistance,
                type._lacunarity,
                type._heigthSeed,
                type._heightOffset
                );
        }
        
    }

    /// <summary>
    /// Standard terrain type modifiable in inspector
    /// </summary>
    [System.Serializable]
    public class TerrainType
    {
        public bool _active = true;
        public string _name;
        public Color _color;
        public Material _material;

        [Header("Height Map")]
        [Range(0.1f, 20)] 
        public float _height = 1;
        public float _noiseScale;
        public int _numOctaves;
        [Range(0, 1f)] 
        public float _persistance;
        public float _lacunarity;
        public uint _heigthSeed;
        public float _heightOffset;
        
    }
}