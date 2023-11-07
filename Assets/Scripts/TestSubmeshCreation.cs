using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TestSubmeshCreation : MonoBehaviour
{

    public void Generate()
    {
        int vertexCount = 4;
        int indexCount = 6;
        
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        NativeArray<VertexAttributeDescriptor> descriptor = new(
                    4, Allocator.Temp, NativeArrayOptions.UninitializedMemory
                );
        try
        {
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3
            );
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4
            );
            descriptor[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2
            );
            meshData.SetVertexBufferParams(vertexCount, descriptor);
        
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

            meshData.subMeshCount = 1;
            int startIndex = 0;

            var subMeshDescriptor = new SubMeshDescriptor(startIndex, indexCount);
            meshData.SetSubMesh(
                0, subMeshDescriptor
                //MeshUpdateFlags.DontValidateIndices
            );
            var mesh = GetComponent<MeshFilter>().mesh;
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        }
        finally
        {
            meshDataArray.Dispose();
            descriptor.Dispose();
        }

    }

}
