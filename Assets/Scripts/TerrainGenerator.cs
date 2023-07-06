using UnityEngine;

namespace Waterworld
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

        public bool autoUpdate;

        private Mesh mesh;
        private bool meshChanged = true;

        void Start()
        {
            Init();
        }

        public void Init()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = mesh;
        }

        private void Update()
        {
            if (meshX != meshXOld
                || meshZ != meshZOld
                || meshResolution != meshResolutionOld
                || tiling != tilingOld
                || height != heightOld)
            {
                meshChanged = true;
            }
            if (meshChanged)
            {
                GenerateMesh();
                meshChanged = false;
                meshXOld = meshX;
                meshZOld = meshZ;
                meshResolutionOld = meshResolution;
                tilingOld = tiling;   
                heightOld = height;
            }
        }

        public void GenerateMesh()
        {
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
                default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();
        }

    }
}

