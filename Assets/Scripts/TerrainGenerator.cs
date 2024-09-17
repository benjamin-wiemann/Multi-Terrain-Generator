using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using LiquidPlanet.Event;
using UnityEngine.Events;
using Unity.Mathematics;

namespace LiquidPlanet
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Mesh Properties")]
        [SerializeField,
            Range(1, 100)] float _meshX = 10;
        [SerializeField,
            Range(1, 100)] float _meshZ = 10;
        [SerializeField] float _tiling = 1;
        [SerializeField] int _meshResolution = 10;

        [Header("Height Map")]
        [SerializeField,
            Range(0.1f, 20)] float _height = 1;
        [SerializeField] float _heightScale;
        [SerializeField] int _heightOctaves;
        [SerializeField,
            Range(0, 1)] float _heightPersistance;
        [SerializeField] float _heightLacunarity;
        [SerializeField] bool _normalize = false;

        [Header("Terrain Segmentation")]
        [SerializeField] uint _terrainSeed;
        [SerializeField,
            Range(0.05f, 0.2f)] float _seedPointDensity = 0.1f;
        [SerializeField,
            Range(0f, 1f)] float _noiseScale = 0.5f;
        [SerializeField] float _noiseOffset = 1f;
        [SerializeField] float _borderGranularity = 1;
        [SerializeField] float _borderSmoothness = 1f;
        [SerializeField] List<TerrainType> _terrainTypes = new();

        [SerializeField]
        public bool autoUpdate;
        [SerializeField]
        MeshFinishedEvent _onMeshFinished;

        Mesh _mesh;

        NativeArray<float> _heightMap;

        NativeArray<TerrainInfo> _terrainMap;

        NativeArray<float> _maxNoiseValues;

        NativeArray<float> _minNoiseValues;

        [System.Serializable]
        public class MeshFinishedEvent : UnityEvent<MeshGenFinishedEventArgs> { }


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
            SharedTriangleGrid triangleGrid = new SharedTriangleGrid(
                _meshResolution,
                _meshX,
                _meshZ,
                _tiling,
                _height
            );
            List<Material> materials = new();
            for (int i = 0; i < _terrainTypes.Count; i++)
            {
                var type = _terrainTypes[i];
                if (type._active)
                {
                    materials.Add(type._material);
                }
            }
            GetComponent<Renderer>().SetSharedMaterials(materials);
            _terrainMap = new(
                triangleGrid.NumX * triangleGrid.NumZ,
                Allocator.Persistent);
            NativeArray<int2> coordinates = new(
                triangleGrid.NumX * triangleGrid.NumZ,
                Allocator.Persistent);
            NativeList<int> terrainCounters = new (_terrainTypes.Count, Allocator.Persistent);
            NativeList<TerrainTypeStruct> types = new(_terrainTypes.Count, Allocator.Persistent);
            foreach (TerrainType terrainType in _terrainTypes)
            {
                if (terrainType._active)
                {
                    types.Add(TerrainTypeStruct.Convert(terrainType));
                    terrainCounters.Add(0);
                }
            }
            TerrainSegmentator.GetTerrainSegmentation(
                triangleGrid.NumX,
                triangleGrid.NumZ,
                _meshX,
                _meshZ,
                _meshResolution,
                _terrainSeed,
                _seedPointDensity,
                _noiseOffset,
                _noiseScale,
                _borderGranularity,
                _borderSmoothness,
                types,
                _terrainMap,
                coordinates,
                terrainCounters
                );
                        
            int numVerticesX = triangleGrid.NumX + 1;
            int numVerticesY = triangleGrid.NumZ + 1;
            _heightMap = new(
                numVerticesX * numVerticesY,
                Allocator.Persistent);            
            _minNoiseValues = new(numVerticesY, Allocator.Persistent);
            _maxNoiseValues = new(numVerticesY, Allocator.Persistent);            
            NoiseJob.ScheduleParallel(   
                _terrainMap,
                types,
                numVerticesX,
                numVerticesY,
                _heightScale,
                _heightOctaves,
                _heightPersistance,
                _heightLacunarity,
                _meshResolution,
                _heightMap,
                _maxNoiseValues,
                _minNoiseValues);
            if (_normalize)
            {
                NormalizeNoiseJob.ScheduleParallel(
                    _heightMap,
                    _maxNoiseValues,
                    _minNoiseValues,
                    numVerticesX,
                    numVerticesY);
            }

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob.ScheduleParallel(
                triangleGrid,
                _heightMap,
                terrainCounters,
                coordinates,
                _mesh,
                meshData);
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);
            Event.MeshGenFinishedEventArgs args = new (numVerticesX, numVerticesY, _heightMap, _terrainMap, types.ToArray());
            _onMeshFinished?.Invoke(args);
            
            _minNoiseValues.Dispose();
            _maxNoiseValues.Dispose();
            _heightMap.Dispose();
            _terrainMap.Dispose();            
            coordinates.Dispose();
            terrainCounters.Dispose();
            types.Dispose();
        }


        void OnValidate()
        {
            if (_meshX < 1)
            {
                _meshX = 1;
            }
            if (_meshZ < 1)
            {
                _meshZ = 1;
            }
            if (_heightLacunarity < 1)
            {
                _heightLacunarity = 1;
            }
            if (_heightOctaves < 0)
            {
                _heightOctaves = 0;
            }            
            if ( _terrainTypes.Count == 0 )
            {
                _terrainTypes.Add(new TerrainType() {
                    _name = "Standard Terrain",
                    _color = Color.green });
            }
            for (int i = 0; i < _terrainTypes.Count; i++)
            {
                var type = _terrainTypes[i];
                if (type._name == "" || type._name == null)
                {
                    type._name = "Terrain_" + i;
                }
                else if (type._name.Length >= 125)
                {
                    type._name = type._name.Substring(0, 125);
                }
            }
        }

    }
}

