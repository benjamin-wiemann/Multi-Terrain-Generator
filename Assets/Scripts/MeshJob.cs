
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

        public void Execute(int i) => _generator.Execute<VertexStream>(i, _stream, _noiseMap, _terrainMap, _terrainTypes, _coordinates, _threadStartIndices);

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
            float avgTriangles = (coordinates.Length - job._generator.NumX - job._generator.NumY +1) / threadCount;
            job._threadStartIndices = new(threadCount + 1, Allocator.Persistent);
            for( int i = 0; i < job._threadStartIndices.Length; i++)
            {
                job._threadStartIndices[i] = (int) math.round( i * avgTriangles);
            }
            

            JobHandle handle;
            // Use the least common multiple
            job.Run(threadCount);
            handle = default;
            //return job.Schedule(job._generator.JobLength, dependency);            

            //handle = job.ScheduleParallel(job._generator.JobLength, 1, dependency);
            job._stream.SetSubMeshes(meshData, terrainTypes, generator.Bounds, job._generator.VertexCount);
            
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