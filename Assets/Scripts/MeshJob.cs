
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using MultiTerrain.Helper;
using MultiTerrain.DebugTools;

namespace MultiTerrain
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
            NativeArray<int> submeshCounters,
            NativeArray<int2> coordinates,
            Bounds bounds,
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
                mesh.bounds = bounds,
                job._generator.VertexCount,
                job._generator.IndexCount
            );

            int threadCount = MathHelper.Lcm(SystemInfo.processorCount, submeshCounters.Length);
            job._threadStartIndices = new(threadCount + 1, Allocator.Persistent);
            int numTrianglePairs = job._generator.NumX * job._generator.NumZ;
            for ( int i = 0; i < job._threadStartIndices.Length; i++)
            {
                job._threadStartIndices[i] = (int) math.round( i * numTrianglePairs / threadCount);
            }
                        
            if (JobTools.Get()._runParallel)
                job.ScheduleParallel(threadCount, (int) JobTools.Get()._batchCountInThread, default).Complete();
            else
                job.Run(threadCount);
            
            job._stream.SetSubMeshes(meshData, submeshCounters, bounds, job._generator.VertexCount);
            job._threadStartIndices.Dispose();
        }
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}