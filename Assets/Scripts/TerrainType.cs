using Unity.Collections;
using UnityEngine;
using System;
using Unity.Mathematics;
using MultiTerrain.Helper;

namespace MultiTerrain
{

    /// <summary>
    /// Terrain type struct to be used with Burst and Jobs 
    /// </summary>
    [System.Serializable]
    public struct TerrainTypeStruct
    {
        public int PrimeId { get; }
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
            int primeId,
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
            PrimeId = primeId;
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

        public static TerrainTypeStruct Convert(TerrainType type, int primeId)
        {
            return new TerrainTypeStruct(
                primeId,
                type._name, 
                new float3(type._color.r, type._color.g, type._color.b),
                type._height,
                type._noiseScale,
                type._numOctaves,
                type._persistance,
                type._lacunarity,
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
        
        [Header("Height Map")]
        [Range(0.1f, 20)] 
        public float _height = 1;
        public float _noiseScale;
        public int _numOctaves;
        [Range(0, 1f)] 
        public float _persistance;
        public float _lacunarity;
        public float _heightOffset;

        [Header("Surface Shader")]     
        public Texture2D _diffuse;
        public Vector2 _tiling;
        public Vector2 _offset;

        public Texture2D _normalMap;
        public float _bumpScale = 1f;

        public Texture2D _heightMap;
        [Range(0.005f, 0.08f)]
        public float _heightScale = 0.005f;
        [Range(0.01f, 1.00f)]
        public float _triplanarBlending = 0.01f;

        public Texture2D _occlusionMap;
        public float _occlusionStrength = 0.5f;

        public Texture2D _smoothnessMap;
        [Range(0f,1f)]
        public float _smoothness = 0.3f;

        public Texture2D _specularMap;
        public Color _specColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
    }
}