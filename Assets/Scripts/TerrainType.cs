using UnityEngine;
using System;
using Unity.Mathematics;

namespace MultiTerrain
{


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
        public Vector2 _tiling = new Vector2(1, 1);
        public Vector2 _offset = new Vector2(0, 0);

        public Texture2D _normalMap;
        public float _bumpScale = 1f;

        public Texture2D _heightMap;
        [Range(0.005f, 0.08f)]
        public float _parallaxHeightScale = 0.005f;
        [Range(0.001f, 1.00f)]
        public float _triplanarBlending = 0.01f;

        public Texture2D _occlusionMap;
        public float _occlusionStrength = 0.5f;

        public Texture2D _smoothnessMap;
        [Range(0f, 1f)]
        public float _smoothness = 0.3f;

        public Texture2D _specularMap;
        public Color _specColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
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
                type._heightOffset
                );
        }
        
    }
}