using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.XR;

namespace Waterworld
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField]
        float meshX = 10;

        [SerializeField]
        float meshZ = 10;

        [SerializeField]
        int meshResolution = 10;

        private Mesh mesh;

        void Start()
        {
            mesh = new Mesh
            {
                name = "Procedural Mesh"
            };
            GetComponent<MeshFilter>().mesh = mesh;
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];
            MeshJob<SharedTriangleGrid>.ScheduleParallel(mesh, meshData, meshResolution, meshX, meshZ, default).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        }

    }
}

