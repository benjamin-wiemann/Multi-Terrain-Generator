
using System.Reflection.Emit;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TerrainUtils;

namespace LiquidPlanet
{


    //[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct VertexJob<G> : IJobFor
            where G : struct, IMeshGenerator
    {

        [ReadOnly]
        NativeArray<float> _noiseMap;

        [ReadOnly]
        NativeArray<int> _terrainMap;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<int> _subMeshTriangleIndices;

        public static JobHandle ScheduleParallel(
            NativeArray<float> noiseMap,
            NativeArray<int> terrainMap,
            NativeList<TerrainTypeStruct> terrainTypes,            
            JobHandle dependency
        )
        {
            var job = new VertexJob<G>();
            job._noiseMap = noiseMap;
            job._terrainMap = terrainMap;
            //job._subMeshTriangleIndices = new( terrainTypes.Length, Allocator.Persistent);
            //job._subMeshTriangleIndices[0] = 0;
            //for (int i = 1; i < terrainTypes.Length; i++)
            //{
            //    job._subMeshTriangleIndices[i] = terrainTypes[i-1].NumTrianglePairs + job._subMeshTriangleIndices[i-1];
            //}
            
            JobHandle handle;
            job.Run(10);
            handle = default;            
            return handle;
        }

        public void Execute(int i)
        {

        }
    }
    
    

}