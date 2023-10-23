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

        TerrainTypeUnmanaged(string name, Color color)
        {
            Name = new FixedString128Bytes(name);
            Color = color;
        }

        public static TerrainTypeUnmanaged Convert(TerrainType type)
        {
            return new TerrainTypeUnmanaged(type._name, type._color);
        }
    }

    /// <summary>
    /// Standard terrain type modifiable in inspector
    /// </summary>
    [System.Serializable]
    public class TerrainType
    {
        
        public string _name;
        public Color _color;

    }
}