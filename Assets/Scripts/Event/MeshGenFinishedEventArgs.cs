using System;
using System.Collections.Generic;
using Unity.Collections;

namespace LiquidPlanet.Event
{
    public class MeshGenFinishedEventArgs : EventArgs
    {
        public MeshGenFinishedEventArgs(
            int numVerticesX,
            int numVerticesY,
            NativeArray<float> heightMap,
            NativeArray<int> terrainSegmentation,
            NativeList<TerrainTypeUnmanaged> terrainTypes
            )
        {
            NumVerticesX = numVerticesX;
            NumVerticesY = numVerticesY;
            TerrainSegmentation = terrainSegmentation;
            TerrainTypes = terrainTypes;
            HeightMap = heightMap;
        }

        public int NumVerticesX { get; }
        public int NumVerticesY { get; }

        public NativeArray<float> HeightMap { get; }

        public NativeArray<int> TerrainSegmentation { get; }
        public NativeList<TerrainTypeUnmanaged> TerrainTypes { get; }

    }
}


