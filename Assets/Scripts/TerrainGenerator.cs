using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using System.Collections.Generic;

namespace LiquidPlanet
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField, Range(1, 100)]
        float meshX = 10;
        float meshXOld;

        [SerializeField, Range(1, 100)]
        float meshZ = 10;
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

        public List<TerrainType> terrainTypes;

        [SerializeField]
        public bool autoUpdate;

        Mesh _mesh;

        [HideInInspector]
        public TerrainSegmentator Segmentator { get; }

        public delegate void MeshGenerationFinishedEvent();
        public static event MeshGenerationFinishedEvent OnMeshFinishedEvent;


        NativeArray<float> _heightMap;

        NativeArray<float> _terrainMap;

        NativeArray<float> _maxNoiseValues;

        NativeArray<float> _minNoiseValues;

        //void Start()
        //{
        //    Init();
        //}

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
            if (_minNoiseValues.IsCreated)
            {
                _minNoiseValues.Dispose();
            }
            if (_maxNoiseValues.IsCreated)
            {
                _maxNoiseValues.Dispose();
            }
            Segmentator.Dispose();

            SharedTriangleGrid triangleGrid = new SharedTriangleGrid(
                meshResolution,
                meshX,
                meshZ,
                tiling,
                height
            );
            int numVerticesX = triangleGrid.NumX + 1;
            int numVerticesZ = triangleGrid.NumZ + 1;
            _heightMap = new(
                numVerticesX * numVerticesZ,
                Allocator.Persistent);
            _terrainMap = new(
                numVerticesX * numVerticesZ,
                Allocator.Persistent);
            _minNoiseValues = new(numVerticesZ, Allocator.Persistent);
            _maxNoiseValues = new(numVerticesZ, Allocator.Persistent);
            NoiseJob.ScheduleParallel(
                _heightMap,
                _maxNoiseValues,
                _minNoiseValues,
                numVerticesX,
                numVerticesZ,
                seed,
                heightScale,
                heightOctaves,
                heightPersistance,
                heightLacunarity,
                heightOffset,
                default,
                debugNoise).Complete();
            NormalizeNoise(_heightMap, numVerticesX, numVerticesZ);

            var segmentation = Segmentator.GetTerrainSegmentation(
                numVerticesX,
                numVerticesZ,
                terrainTypes,
                terrainGranularity,
                noiseOffset,
                noiseScale
                );

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob<SharedTriangleGrid>.ScheduleParallel(
                triangleGrid,
                _heightMap,
                _mesh,
                meshData,   
                default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, _mesh);
            //_mesh.RecalculateBounds();
            OnMeshFinishedEvent?.Invoke();
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
            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[z * mapWidth + x] = height * unlerp(minNoiseValue, maxNoiseValue, noiseMap[z * mapWidth + x]);
                }
            }
        }

        void OnValidate()
        {
            if (meshX < 1)
            {
                meshX = 1;
            }
            if (meshZ < 1)
            {
                meshZ = 1;
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
        }

        void OnApplicationQuit()
        {
            //if(noise)
            //noiseMap.Dispose();
        }

    }
}

