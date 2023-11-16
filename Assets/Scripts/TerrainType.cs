using Unity.Collections;
using UnityEngine;
using System;

namespace LiquidPlanet
{

    /// <summary>
    /// Unmanaged terrain type to be used with Burst and Jobs 
    /// </summary>
    [System.Serializable]
    public struct TerrainTypeUnmanaged
    {
        public FixedString128Bytes Name { get; }
        public Color Color { get; }

        int _numTrianglePairs;
        public int NumTrianglePairs { get => _numTrianglePairs; set => _numTrianglePairs = value; }

        int _subMeshTriangleIndex;

        public int GetAndIncrementSubMeshTriangleIndex { get => _subMeshTriangleIndex++; }

        TerrainTypeUnmanaged(string name, Color color)
        {
            Name = new FixedString128Bytes(name);
            Color = color;
            _numTrianglePairs = 0;
            _subMeshTriangleIndex = 0;
        }

        public static TerrainTypeUnmanaged Convert(TerrainType type) => new TerrainTypeUnmanaged(type._name, type._color);
        
    }

    /// <summary>
    /// Standard terrain type modifiable in inspector
    /// </summary>
    [System.Serializable]
    public class TerrainType
    {
        [SerializeField]
        public bool active = true;
        [SerializeField]
        public string _name;
        [SerializeField]
        public Color _color;

    }
}