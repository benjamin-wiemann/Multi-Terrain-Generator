
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace LiquidPlanet
{


    //[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G> : IJobFor
            where G : struct, IMeshGenerator
    {

        G _generator;

        [WriteOnly]
        VertexStream _stream;

        [ReadOnly]
        NativeArray<float> _noiseMap;

        [ReadOnly]
        NativeArray<int> _terrainMap;

        [NativeDisableContainerSafetyRestriction]
        NativeList<int> _subMeshTriangleIndices;

        public void Execute(int i) => _generator.Execute<VertexStream>(i, _stream, _noiseMap, _terrainMap, _subMeshTriangleIndices);

        public static JobHandle ScheduleParallel(
            G generator,
            NativeArray<float> noiseMap,
            NativeArray<int> terrainMap,
            NativeList<TerrainTypeUnmanaged> terrainTypes,
            Mesh mesh,
            Mesh.MeshData meshData,
            JobHandle dependency
        )
        {
            var job = new MeshJob<G>();
            job._generator = generator;
            job._noiseMap = noiseMap;
            job._terrainMap = terrainMap;
            job._subMeshTriangleIndices = new( terrainTypes.Length, Allocator.Persistent);
            job._subMeshTriangleIndices.AddNoResize(0);
            for (int i = 1; i < terrainTypes.Length; i++)
            {
                job._subMeshTriangleIndices.AddNoResize( terrainTypes[i-1].NumTrianglePairs + job._subMeshTriangleIndices[i-1]);
            }
            job._stream.Setup(
                meshData,
                mesh.bounds = job._generator.Bounds,
                job._generator.VertexCount,
                job._generator.IndexCount,
                terrainTypes
            );
            JobHandle handle;
            job.Run(job._generator.JobLength);
            handle = default;
            //return job.Schedule(job._generator.JobLength, dependency);            

            //handle = job.ScheduleParallel(job._generator.JobLength, 1, dependency);
            job._stream.SetSubMeshes(meshData, terrainTypes, generator.Bounds, job._generator.VertexCount);
            return handle;
        }

    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}