
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using LiquidPlanet.Helper;
using System.Runtime.ConstrainedExecution;

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

        [NativeDisableParallelForRestriction]
        NativeArray<int2> _coordinates;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _threadStartIndices;


        public void Execute(int i) => _generator.Execute<VertexStream>(
            i, _stream, _noiseMap, _coordinates, _threadStartIndices);

        public static void ScheduleParallel(
            SharedTriangleGrid generator,
            NativeArray<float> noiseMap,
            NativeList<TerrainTypeUnmanaged> terrainTypes,
            NativeArray<int2> coordinates,
            Mesh mesh,
            Mesh.MeshData meshData,
            JobHandle dependency
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

            int threadCount = MathHelper.Lcm(SystemInfo.processorCount, terrainTypes.Length);
            job._threadStartIndices = new(threadCount + 1, Allocator.Persistent);
            int numTrianglePairs = job._generator.NumX * job._generator.NumZ;
            Debug.Log(string.Format("Num triangle pairs: {0} ", numTrianglePairs));
            for ( int i = 0; i < job._threadStartIndices.Length; i++)
            {
                job._threadStartIndices[i] = (int) math.round( i * numTrianglePairs / threadCount);
                Debug.Log(string.Format("Thread start index for thread: {0} is {1}", i, job._threadStartIndices[i]));
            }
            
            //JobHandle handle;
            // Use the least common multiple
            job.Run(threadCount);
            //handle = default;
            //return job.Schedule(job._generator.JobLength, dependency);            

            //job.ScheduleParallel(job._generator.JobLength, 1, dependency).Complete();
            job._stream.SetSubMeshes(meshData, terrainTypes, generator.Bounds, job._generator.VertexCount);
            job._threadStartIndices.Dispose();            
            //Debug.Log(string.Format("Number of vertices: {0}, Number of Triangle Pairs: {1}", job._counter[0], job._counter[1]));
            
        }

        
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}