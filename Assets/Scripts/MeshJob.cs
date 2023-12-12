
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

        [ReadOnly]
        NativeArray<int> _terrainMap;

        [NativeDisableParallelForRestriction]
        NativeList<TerrainTypeUnmanaged> _terrainTypes;

        [NativeDisableParallelForRestriction]
        NativeArray<int2> _coordinates;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _threadStartIndices;

        [NativeDisableParallelForRestriction]
        NativeArray<int> _counter;

        public void Execute(int i) => _generator.Execute<VertexStream>(
            i, _stream, _noiseMap, _terrainMap, _terrainTypes, _coordinates, _threadStartIndices, _counter);

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
            var job = new MeshJob
            {
                _generator = generator,
                _noiseMap = noiseMap,
                _terrainMap = terrainMap,
                _terrainTypes = terrainTypes,
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
            

            job._counter = new(2, Allocator.Persistent);
            job._counter[0] = 0;
            job._counter[1] = 0;
            JobHandle handle;
            // Use the least common multiple
            job.Run(threadCount);
            handle = default;
            //return job.Schedule(job._generator.JobLength, dependency);            

            //handle = job.ScheduleParallel(job._generator.JobLength, 1, dependency);
            job._stream.SetSubMeshes(meshData, terrainTypes, generator.Bounds, job._generator.VertexCount);
            Debug.Log(string.Format("Number of vertices: {0}, Number of Triangle Pairs: {1}", job._counter[0], job._counter[1]));            
            return handle;
        }

        public static void GetChunkSizes( NativeArray<TerrainTypeUnmanaged> types, int threadCount, NativeArray<int> chunkSizes)
        {
            //for (int i = 0; i < chunkCountPerTerrain.Length; i++)
            //{
            //    ter
            //}
        }
        
    }

    public delegate JobHandle MeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int depth, JobHandle dependency
    );

}