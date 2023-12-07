
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using LiquidPlanet.Helper;

namespace LiquidPlanet
{


    //[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob : IJobFor
    {

        SharedTriangleGrid _generator;

        [WriteOnly]
        VertexStream _stream;

        [ReadOnly]
        NativeArray<float> _noiseMap;

        [ReadOnly]
        NativeArray<int> _terrainMap;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<uint> _subMeshIndices;

        NativeArray<int2> _coordinates;

        public void Execute(int i) => _generator.Execute<VertexStream>(i, _stream, _noiseMap, _terrainMap, _coordinates);

        public static JobHandle ScheduleParallel(
            SharedTriangleGrid generator,
            NativeArray<float> noiseMap,
            NativeArray<int> terrainMap,
            NativeList<TerrainTypeUnmanaged> terrainTypes,
            NativeArray<int2> coordinates,
            Mesh mesh,
            Mesh.MeshData meshData,
            JobHandle dependency
        )
        {
            var job = new MeshJob();
            job._generator = generator;
            job._noiseMap = noiseMap;
            job._terrainMap = terrainMap;
            job._coordinates = coordinates;
            
            job._stream.Setup(
                meshData,
                mesh.bounds = job._generator.Bounds,
                job._generator.VertexCount,
                job._generator.IndexCount
            );
            JobHandle handle;
            // Use the least common multiple
            job.Run(MathHelper.Lcm(SystemInfo.processorCount, terrainTypes.Length));
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