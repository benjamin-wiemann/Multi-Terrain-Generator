using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

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
        public float persistance;

        [SerializeField]
        public float lacunarity;

        [SerializeField]
        public uint seed;

        [SerializeField]    
        public Vector2 offset;

        public bool autoUpdate;

        private Mesh mesh;
        private bool meshChanged = true;

        NativeArray<float> noiseMap;

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

        //private void Update()
        //{
        //    if (meshX != meshXOld
        //        || meshZ != meshZOld
        //        || meshResolution != meshResolutionOld
        //        || tiling != tilingOld
        //        || height != heightOld)
        //    {
        //        meshChanged = true;
        //    }
        //    if (meshChanged)
        //    {
        //        GenerateMesh();
        //        meshChanged = false;
        //        meshXOld = meshX;
        //        meshZOld = meshZ;
        //        meshResolutionOld = meshResolution;
        //        tilingOld = tiling;   
        //        heightOld = height;
        //    }
        //}

        public void GenerateMesh()
        {
            if (noiseMap.IsCreated)
            {
                noiseMap.Dispose();
            }
            int countZ = Mathf.RoundToInt(meshZ * meshResolution) + 1;
            int countX = Mathf.RoundToInt(meshX * meshResolution) + 1;
            noiseMap = new ( countX * countZ, Allocator.Persistent);
            NoiseJob.ScheduleParallel(
                noiseMap,
                countX,
                countZ,
                seed,
                noiseScale,
                octaves,
                persistance,
                lacunarity,
                offset,
                default).Complete();
            //for (int y = 0; y < countZ; y++)
            //{
            //    for (int x = 0; x < countX; x++)
            //    {
            //        noiseMap[y * countX + x] = math.unlerp(minNoiseHeight, maxNoiseHeight, noiseMap[y * countZ + x]);
            //    }
            //}
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob<SharedTriangleGrid>.ScheduleParallel(
                mesh,
                meshData,
                meshResolution,
                meshX,
                meshZ,
                tiling,
                height,
                noiseMap,
                default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
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

