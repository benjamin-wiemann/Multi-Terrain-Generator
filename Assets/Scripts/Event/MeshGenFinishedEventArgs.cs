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
            NativeArray<TerrainWeighting> terrainSegmentation,
            TerrainTypeStruct[] terrainTypes,
            NativeHashMap<int, int> terrainIdsToIndices
            )
        {
            NumVerticesX = numVerticesX;
            NumVerticesY = numVerticesY;
            TerrainSegmentation = terrainSegmentation;
            TerrainTypes = terrainTypes;
            HeightMap = heightMap;
            TerrainIdsToIndices = terrainIdsToIndices;
        }

        public int NumVerticesX { get; }
        public int NumVerticesY { get; }

        public NativeArray<float> HeightMap { get; }

        public NativeArray<TerrainWeighting> TerrainSegmentation { get; }
        public TerrainTypeStruct[] TerrainTypes { get; }

        public NativeHashMap<int, int> TerrainIdsToIndices { get; }

    }
}


