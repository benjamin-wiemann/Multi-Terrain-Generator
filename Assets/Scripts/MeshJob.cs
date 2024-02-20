
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using LiquidPlanet.Helper;
using LiquidPlanet.DebugTools;

namespace LiquidPlanet
{


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob : IJobFor
    {

        SharedTriangleGrid _generator;

        [WriteOnly]
        VertexStream _stream;

        [ReadOnly]
        NativeArray<float> _noiseMap;

        [NativeDisableParallelForRestriction]
        NativeArray<int2> _coordinates;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _threadStartIndices;


        public void Execute(int i) => _generator.Execute<VertexStream>(
            i, _stream, _noiseMap, _coordinates, _threadStartIndices);

        public static void ScheduleParallel(
            SharedTriangleGrid generator,
            NativeArray<float> noiseMap,
            NativeList<int> terrainCounters,
            NativeArray<int2> coordinates,
            Mesh mesh,
            Mesh.MeshData meshData
        )
        {
            var job = new MeshJob
            {
                _generator = generator,
                _noiseMap = noiseMap,
                _coordinates = coordinates
            };

            job._stream.Setup(
                meshData,
                mesh.bounds = job._generator.Bounds,
                job._generator.VertexCount,
                job._generator.IndexCount
            );

            int threadCount = MathHelper.Lcm(SystemInfo.processorCount, terrainCounters.Length);
            job._threadStartIndices = new(threadCount + 1, Allocator.Persistent);
            int numTrianglePairs = job._generator.NumX * job._generator.NumZ;
            for ( int i = 0; i < job._threadStartIndices.Length; i++)
            {
                job._threadStartIndices[i] = (int) math.round( i * numTrianglePairs / threadCount);
            }
            //Debug.Log(string.Format("thread start indices: {0}, {1}, {2}, {3}, {4}, {5}", job._threadStartIndices[0], 
            //    job._threadStartIndices[1], job._threadStartIndices[2], job._threadStartIndices[3], job._threadStartIndices[4], job._threadStartIndices[5]));
            
            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(threadCount, (int) JobTools.Get()._batchCountInThread, default).Complete();
            else
                job.Run(threadCount);
            
            //Debug.Log(string.Format("terrain num triangles: {0}, {1}, {2}", terrainTypes[0].NumTrianglePairs, terrainTypes[1].NumTrianglePairs, terrainTypes[2].NumTrianglePairs));

            job._stream.SetSubMeshes(meshData, terrainCounters, generator.Bounds, job._generator.VertexCount);
            job._threadStartIndices.Dispose();
        }
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}