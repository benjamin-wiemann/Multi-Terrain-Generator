using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

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
        float noiseScale;

        [SerializeField]
        int octaves;

        [SerializeField, Range(0, 1)]
        float persistance;

        [SerializeField]
        float lacunarity;

        [SerializeField]
        uint seed;

        [SerializeField]    
        Vector2 offset;

        [SerializeField]
        bool debugNoise = false;

        public bool autoUpdate;

        private Mesh mesh;
        private bool meshChanged = true;

        NativeArray<float> _noiseMap;

        NativeArray<float> _maxNoiseValues;

        NativeArray<float> _minNoiseValues;

        void Start()
        {
            Init();
        }

        public void Init()
        {
            if(mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Procedural Mesh"
                };
                GetComponent<MeshFilter>().mesh = mesh;
            }            
        }


        public void GenerateMesh()
        {
            if (_noiseMap.IsCreated)
            {
                _noiseMap.Dispose();
            }
            if (_minNoiseValues.IsCreated)
            {
                _minNoiseValues.Dispose();
            }
            if (_maxNoiseValues.IsCreated)
            {
                _maxNoiseValues.Dispose();
            }
            SharedTriangleGrid triangleGrid = new SharedTriangleGrid(
                meshResolution,
                meshX,
                meshZ,
                tiling,
                height
            );
            int numVerticesX = triangleGrid.NumX + 1;
            int numVerticesZ = triangleGrid.NumZ + 1;
            _noiseMap = new(
                numVerticesX * numVerticesZ,
                Allocator.Persistent);
            _minNoiseValues = new(numVerticesZ, Allocator.Persistent);
            _maxNoiseValues = new(numVerticesZ, Allocator.Persistent);
            NoiseJob.ScheduleParallel(
                _noiseMap,
                _maxNoiseValues,
                _minNoiseValues,
                numVerticesX,
                numVerticesZ,
                seed,
                noiseScale,
                octaves,
                persistance,
                lacunarity,
                offset,
                default,
                debugNoise).Complete();        
            NormalizeNoise(numVerticesX, numVerticesZ);
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob<SharedTriangleGrid>.ScheduleParallel(
                triangleGrid,
                _noiseMap,
                mesh,
                meshData,   
                default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
        }

        void NormalizeNoise(int mapWidth, int mapHeight)
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
                    _noiseMap[z * mapWidth + x] = height * unlerp(minNoiseValue, maxNoiseValue, _noiseMap[z * mapWidth + x]);
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
            if (lacunarity < 1)
            {
                lacunarity = 1;
            }
            if (octaves < 0)
            {
                octaves = 0;
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

