using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using LiquidPlanet.Event;
using static UnityEngine.Rendering.DebugUI;

namespace LiquidPlanet
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField, Range(1, 100)]
        float meshX = 10;
        float meshXOld;

        [SerializeField, Range(1, 100)]
        float meshY = 10;
        float meshZOld;

        [SerializeField]
        float tiling = 1;
        float tilingOld;

        [SerializeField]
        int meshResolution = 10;
        int meshResolutionOld;

        [SerializeField, Range(0.1f, 20)]
        float height = 1;
        float heightOld;

        [SerializeField]
        float heightScale;

        [SerializeField]
        int heightOctaves;

        [SerializeField, Range(0, 1)]
        float heightPersistance;

        [SerializeField]
        float heightLacunarity;

        [SerializeField]
        uint seed;

        [SerializeField]    
        Vector2 heightOffset;

        [SerializeField]
        bool debugNoise = false;

        [SerializeField]
        int terrainGranularity = 100;

        [SerializeField]
        float noiseScale = 10f;

        [SerializeField]
        float noiseOffset = 1f;

        //public NativeList<TerrainType> terrainTypes = new(Allocator.Persistent);
        public List<TerrainType> _terrainTypes = new();

        [SerializeField]
        public bool autoUpdate;

        Mesh _mesh;

        NativeArray<float> _heightMap;

        NativeArray<int> _terrainMap;

        NativeArray<float> _maxNoiseValues;

        NativeArray<float> _minNoiseValues;

        public static event EventHandler<MeshGenFinishedEventArgs> MeshFinishedEvent;
        

        public void Init()
        {
            if(_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = "Procedural Mesh"
                };
                GetComponent<MeshFilter>().mesh = _mesh;
            }            
        }


        public void GenerateMesh()
        {
            if (_heightMap.IsCreated)
            {
                _heightMap.Dispose();
            }
            if (_terrainMap.IsCreated)
            {
                _terrainMap.Dispose();
            }
            SharedTriangleGrid triangleGrid = new SharedTriangleGrid(
                meshResolution,
                meshX,
                meshY,
                tiling,
                height
            );
            int numVerticesX = triangleGrid.NumX + 1;
            int numVerticesY = triangleGrid.NumY + 1;
            _heightMap = new(
                numVerticesX * numVerticesY,
                Allocator.Persistent);
            _terrainMap = new(
                numVerticesX * numVerticesY,
                Allocator.Persistent);
            _minNoiseValues = new(numVerticesY, Allocator.Persistent);
            _maxNoiseValues = new(numVerticesY, Allocator.Persistent);
            NativeList<TerrainTypeUnmanaged> types = new(Allocator.Persistent);
            foreach (TerrainType terrainType in _terrainTypes)
            {
                types.Add(TerrainTypeUnmanaged.Convert(terrainType));
            }
            NoiseJob.ScheduleParallel(
                _heightMap,
                _maxNoiseValues,
                _minNoiseValues,
                numVerticesX,
                numVerticesY,
                seed,
                heightScale,
                heightOctaves,
                heightPersistance,
                heightLacunarity,
                heightOffset,
                default,
                debugNoise).Complete();
            NormalizeNoise(_heightMap, numVerticesX, numVerticesY);

            TerrainSegmentator.GetTerrainSegmentation(
                _terrainMap,
                numVerticesX,
                numVerticesY,
                types,
                terrainGranularity,
                noiseOffset,
                noiseScale
                );

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob<SharedTriangleGrid>.ScheduleParallel(
                triangleGrid,
                _heightMap,
                _terrainMap,
                types,
                _mesh,
                meshData,   
                default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);
            //_mesh.RecalculateBounds
            Event.MeshGenFinishedEventArgs args = new (numVerticesX, numVerticesY, _heightMap, _terrainMap, types);
            MeshFinishedEvent?.Invoke(this, args);
            
            _minNoiseValues.Dispose();
            _maxNoiseValues.Dispose();
            types.Dispose();
        }

        void NormalizeNoise(NativeArray<float> noiseMap, int mapWidth, int mapHeight)
        {
            float maxNoiseValue = float.MinValue;
            float minNoiseValue = float.MaxValue;
            for (int i = 0; i < _maxNoiseValues.Length; i++)
            {
                if (maxNoiseValue < _maxNoiseValues[i])
                {
                    maxNoiseValue = _maxNoiseValues[i];
                }
                if (minNoiseValue > _minNoiseValues[i])
                {
                    minNoiseValue = _minNoiseValues[i];
                }
            }
            NormalizeNoiseJob.ScheduleParallel(noiseMap, maxNoiseValue, minNoiseValue, mapWidth, mapHeight, default).Complete();
            
        }

        void OnValidate()
        {
            if (meshX < 1)
            {
                meshX = 1;
            }
            if (meshY < 1)
            {
                meshY = 1;
            }
            if (heightLacunarity < 1)
            {
                heightLacunarity = 1;
            }
            if (heightOctaves < 0)
            {
                heightOctaves = 0;
            }
            if (seed == 0)
            {
                seed = 1;
            }
            if ( _terrainTypes.Count == 0 )
            {
                _terrainTypes.Add(new TerrainType() {
                    _name = "Standard Terrain",
                    _color = Color.green });
            }
            for (int i = 0; i < _terrainTypes.Count; i++) 
            {
                TerrainType type = _terrainTypes[i];
                if (type._name == "" || type._name == null)
                {
                    type._name = "Terrain_" + i;
                }else if (type._name.Length >= 125)
                {
                    type._name = type._name.Substring(0, 125);
                }
            }
        }
        void OnApplicationQuit()
        {
        }
    }
}

