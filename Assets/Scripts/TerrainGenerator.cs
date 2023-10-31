using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using LiquidPlanet.Event;

namespace LiquidPlanet
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Mesh Properties")]
        [SerializeField, 
            Range(1, 100)]      float   _meshX = 10;
        [SerializeField, 
            Range(1, 100)]      float   _meshY = 10;
        [SerializeField]        float   _tiling = 1;
        [SerializeField]        int     _meshResolution = 10;

        [Header("Height Map")]
        [SerializeField, 
            Range(0.1f, 20)]    float   _height = 1;
        [SerializeField]        float   _heightScale;
        [SerializeField]        int     _heightOctaves;
        [SerializeField, 
            Range(0, 1)]        float   _heightPersistance;
        [SerializeField]        float   _heightLacunarity;
        [SerializeField]        uint    _heigthSeed;
        [SerializeField]        Vector2 _heightOffset;
        [SerializeField]        bool    _debugNoise = false;

        [Header("Terrain Segmentation")]
        [SerializeField]        uint    _terrainSeed;
        [SerializeField]        int     _terrainGranularity = 100;
        [SerializeField]        float   _noiseScale = 10f;
        [SerializeField]        float   _noiseOffset = 1f;
        [SerializeField]        float   _borderGranularity = 1;
        [SerializeField]        List<TerrainType> _terrainTypes = new();

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
                _meshResolution,
                _meshX,
                _meshY,
                _tiling,
                _height
            );
            int numVerticesX = triangleGrid.NumX + 1;
            int numVerticesY = triangleGrid.NumY + 1;
            _heightMap = new(
                numVerticesX * numVerticesY,
                Allocator.Persistent);            
            _minNoiseValues = new(numVerticesY, Allocator.Persistent);
            _maxNoiseValues = new(numVerticesY, Allocator.Persistent);            
            NoiseJob.ScheduleParallel(
                _heightMap,
                _maxNoiseValues,
                _minNoiseValues,
                numVerticesX,
                numVerticesY,
                _heigthSeed,
                _heightScale,
                _heightOctaves,
                _heightPersistance,
                _heightLacunarity,
                _heightOffset,
                default,
                _debugNoise).Complete();
            NormalizeNoise(_heightMap, numVerticesX, numVerticesY);

            _terrainMap = new(
                numVerticesX * numVerticesY,
                Allocator.Persistent);
            NativeList<TerrainTypeUnmanaged> types = new(_terrainTypes.Count, Allocator.Persistent);            
            foreach (TerrainType terrainType in _terrainTypes)
            {
                types.Add(TerrainTypeUnmanaged.Convert(terrainType));
            }
            
            TerrainSegmentator.GetTerrainSegmentation(
                _terrainMap,
                numVerticesX,
                numVerticesY,
                _terrainSeed,
                types,
                _terrainGranularity,
                _noiseOffset,
                _noiseScale,
                _borderGranularity
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
            Event.MeshGenFinishedEventArgs args = new (numVerticesX, numVerticesY, _heightMap, _terrainMap, types.ToArray());
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
            if (_meshX < 1)
            {
                _meshX = 1;
            }
            if (_meshY < 1)
            {
                _meshY = 1;
            }
            if (_heightLacunarity < 1)
            {
                _heightLacunarity = 1;
            }
            if (_heightOctaves < 0)
            {
                _heightOctaves = 0;
            }
            if (_heigthSeed == 0)
            {
                _heigthSeed = 1;
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

