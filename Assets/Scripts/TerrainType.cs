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

        uint _numTrianglePairs;
        public uint NumTrianglePairs { get => _numTrianglePairs; }

        public TerrainTypeUnmanaged(FixedString128Bytes name, Color color, uint numTrianglePairs = 0)
        {
            Name = new FixedString128Bytes(name);
            Color = color;
            _numTrianglePairs = numTrianglePairs;
        }

        public TerrainTypeUnmanaged(string name, Color color, uint numTrianglePairs) :
            this(new FixedString128Bytes(name), color, numTrianglePairs) { }
        

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