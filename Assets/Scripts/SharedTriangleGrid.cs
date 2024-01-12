using UnityEngine;

using static Unity.Mathematics.math;
using Unity.Collections;
using Unity.Mathematics;
using LiquidPlanet.Helper;

namespace LiquidPlanet
{

    public struct SharedTriangleGrid
    {
        public int VertexCount => (NumX + 1) * (NumZ + 1);

        public int IndexCount => 6 * NumZ * NumX;

        public Bounds Bounds => new Bounds(new Vector3(0f, Height, DimZ/2), new Vector3((1f + 0.5f / Resolution) * DimX, Height, DimZ)); 

        public int Resolution { get; set; }

        public float DimZ { get; set; }

        public float DimX { get; set; }
        public float Tiling { get; set; }

        public float Height {  get; set; }

        // number of triangle pairs in x direction
        public int NumX => (int)round(Resolution * DimX);

        // number of triangle pairs in z direction 
        public int NumZ => (int)round(Resolution * DimZ); 

        public SharedTriangleGrid(            
            int resolution,
            float xDim,
            float zDim,
            float tiling,
            float height
        )
        {
            Resolution = resolution;
            DimZ = zDim;
            DimX = xDim;
            Tiling = tiling;
            Height = height;            
        }


        public void Execute<S>(
            int jobNumber,
            VertexStream stream,
            NativeArray<float> noiseMap,
            NativeArray<int2> coordinates,
            NativeArray<int> trianglePairIndices)            
        {
            int iA = -NumX - 2, iB = -NumX - 1, iC = -1, iD = 0;
            var tA = int3(iA, iC, iD);
            var tB = int3(iA, iD, iB);

            for (int tpi = trianglePairIndices[jobNumber]; tpi < trianglePairIndices[jobNumber + 1]; tpi++)
            {
                int2 coordinate = coordinates[tpi];
                int x = coordinate.x;
                int z = coordinate.y;
                int vi = ConfigureAndSetVertex(stream, x, z, noiseMap);

                if (z == 1 || x == 1)
                {
                    int viBorder = vi;
                    // Add vertex for row/column zero
                    if (z == 1 && x == 1)
                    {
                        x = 0;
                        ConfigureAndSetVertex(stream, x, z, noiseMap);
                        x = 1;
                        z = 0;
                        ConfigureAndSetVertex(stream, x, z, noiseMap);
                        x = 0;
                        ConfigureAndSetVertex(stream, x, z, noiseMap);
                    }
                    else
                    {
                        x = x == 1 ? 0 : x;
                        z = z == 1 ? 0 : z;
                        ConfigureAndSetVertex(stream, x, z, noiseMap);
                    }                    
                }
                
                stream.SetTriangle(
                    tpi * 2, vi + tA
                );
                stream.SetTriangle(
                    tpi * 2 + 1, vi + tB
                );
                                
            }

        }

        private int ConfigureAndSetVertex( VertexStream stream, int x, int z, NativeArray<float> noiseMap)
        {
            int vi = (NumX + 1) * z + x;
            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            float triangleWidth = DimX / NumX;
            float triangleHeigth = DimZ / NumZ;

            float xOffset = - DimX / 2;

            vertex.position.x = x * triangleWidth + xOffset;
            vertex.position.z = z * triangleHeigth;
            vertex.position.y = Height * NativeCollectionHelper.SampleValueAt(vertex.position.x - xOffset, vertex.position.z, Resolution, NumX + 1, noiseMap);
            vertex.texCoord0.x = vertex.position.x / Tiling;
            vertex.texCoord0.y = vertex.position.z / Tiling;
            stream.SetVertex(vi, vertex);
            return vi;
        }

 
    }
}