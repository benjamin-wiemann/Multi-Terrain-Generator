using System;
using System.Collections.Generic;
using Unity.Collections;
using MultiTerrain.Segmentation;

namespace MultiTerrain.Event
{
    public class MeshGenFinishedEventArgs : EventArgs
    {
        public MeshGenFinishedEventArgs(
            int numVerticesX,
            int numVerticesY,
            NativeArray<float> heightMap,
            NativeArray<TerrainInfo> terrainSegmentation,
            TerrainTypeStruct[] terrainTypes
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

        public NativeArray<TerrainInfo> TerrainSegmentation { get; }
        public TerrainTypeStruct[] TerrainTypes { get; }

    }
}


