using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LiquidPlanet
{


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G> : IJobFor
            where G : struct, IMeshGenerator
    {

        G generator;

        [WriteOnly]
        VertexStream stream;

        public void Execute(int i) => generator.Execute<VertexStream>(i, stream);

        public static JobHandle ScheduleParallel(
            Mesh mesh, 
            Mesh.MeshData meshData, 
            int resolution, 
            float xDim, 
            float zDim,
            float tiling,
            float height,
            NativeArray<float> noiseMap,
            JobHandle dependency
        )
        {
            var job = new MeshJob<G>();
            job.generator.resolution = resolution;
            job.generator.DimZ = zDim;
            job.generator.DimX = xDim;
            job.generator.Tiling = tiling;
            job.generator.Height = height;
            job.generator.NoiseMap = noiseMap;
            job.stream.Setup(
                meshData,
                mesh.bounds = job.generator.Bounds,
                job.generator.VertexCount,
                job.generator.IndexCount
            );
            return job.ScheduleParallel(job.generator.JobLength, 1, dependency);
        }

    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}