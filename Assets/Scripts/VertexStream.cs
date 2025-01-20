using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;
using static Unity.Mathematics.math;

namespace MultiTerrain
{

    public struct VertexStream
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Stream0
        {
            public float3 position, normal; // normal uses length-4-vector because a length-3-vector with halfs is not supported by mesh API
            public half4 tangent;
        }

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Stream0> stream0;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<int3> triangles;

        public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
        {            
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                3, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3
            );
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, VertexAttributeFormat.Float16, dimension: 4
            );
            // descriptor[3] = new VertexAttributeDescriptor(
            //     VertexAttribute.TexCoord0, dimension: 2
            // );
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose();

            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            

            stream0 = meshData.GetVertexData<Stream0>();
            triangles = meshData.GetIndexData<int>().Reinterpret<int3>(4);
        }

        public void SetSubMeshes( MeshData meshData, NativeList<int> terrainCounters, Bounds bounds, int vertexCount)
        {
            meshData.subMeshCount = terrainCounters.Length;
            int startIndex = 0;
            for (int i = 0; i < terrainCounters.Length; i++)
            {
                var subMeshDescriptor = new SubMeshDescriptor((int) startIndex, (int) terrainCounters[i] * 6)
                {
                    bounds = bounds,
                    vertexCount = vertexCount
                };
                meshData.SetSubMesh(
                    i, subMeshDescriptor,
                    MeshUpdateFlags.DontRecalculateBounds |
                    MeshUpdateFlags.DontValidateIndices
                );
                startIndex += terrainCounters[i] * 6;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, Vertex vertex) => stream0[index] = new Stream0
        {
            position = vertex.position,
            normal = vertex.normal,
            tangent = vertex.tangent
            // texCoord0 = vertex.texCoord0
        };

        public void SetTriangle(int index, int3 triangle)
        {
            triangles[index] = triangle;
        }
    }
}
