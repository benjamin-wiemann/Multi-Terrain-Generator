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
        public Color Color { get; }

        uint _numTrianglePairs;
        public uint NumTrianglePairs { get => _numTrianglePairs; }

        public float   Height;
        public float NoiseScale;
        public int NumOctaves;
        public float Persistance;
        public float   Lacunarity;
        public uint    HeigthSeed;
        public float2  HeightOffset;

        public TerrainTypeStruct(
            FixedString128Bytes name, 
            Color color,
            float height = 1,
            float noiseScale = 1,
            int numOctaves = 1,
            float persistance = 1,
            float lacunarity = 1,
            uint heigthSeed = 1,
            float2 heightOffset = default,
            uint numTrianglePairs = 0
            )
        {
            Name = new FixedString128Bytes(name);
            Color = color;
            _numTrianglePairs = numTrianglePairs;
            Height      = height;
            NoiseScale = noiseScale;
            NumOctaves = numOctaves; 
            Persistance = persistance;
            Lacunarity = lacunarity;
            HeigthSeed = heigthSeed;
            HeightOffset = heightOffset;
        }

        public TerrainTypeStruct(
            string name,
            Color color,
            float height,
            float noiseScale,
            int numOctaves,
            float persistance,
            float lacunarity,
            uint heigthSeed,
            float2 heightOffset,
            uint numTrianglePairs) :
            this(
                new FixedString128Bytes(name),
                color,
                height,
                noiseScale,
                numOctaves,
                persistance,
                lacunarity,
                heigthSeed,
                heightOffset,
                numTrianglePairs) { }


        public static TerrainTypeStruct Convert(TerrainType type)
        {
            return new TerrainTypeStruct(
                type._name, 
                type._color,
                type._height,
                type._noiseScale,
                type._numOctaves,
                type._persistance,
                type._lacunarity,
                type._heigthSeed,
                new float2(type._heightOffset.x, type._heightOffset.y)
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
        public Vector2 _heightOffset;
        [Range(0, 1f)]
        public float _borderInterpolationWidth = 0.5f; 
        
    }
}