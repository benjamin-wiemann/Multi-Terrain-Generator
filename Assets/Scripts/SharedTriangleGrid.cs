using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using Unity.Collections;
using UnityEngine.UIElements;
using Unity.Collections.LowLevel.Unsafe;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using System;

namespace LiquidPlanet
{

    public struct SharedTriangleGrid : IMeshGenerator
    {
        public int VertexCount => (NumX + 1) * (NumZ + 1);

        public int IndexCount => 6 * NumZ * NumX;

        public int JobLength => NumZ + 1;

        public Bounds Bounds => new Bounds(new Vector3(0f, 0f, DimZ/2), new Vector3((1f + 0.5f / Resolution) * DimX, 0f, DimZ)); //dimZ * sqrt(3f) / 2f));

        public int Resolution { get; set; }

        public float DimZ { get; set; }

        public float DimX { get; set; }
        public float Tiling { get; set; }

        public float Height {  get; set; }

        // number of triangle pairs in x direction
        public int NumX => (int)round(Resolution * DimX);

        // number of triangle pairs in z direction is higher, since its height is smaller than its width
        public int NumZ => (int)round(Resolution * DimZ * 2f / sqrt(3f));

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

        
        public void Execute<S>(int z, VertexStream stream, NativeArray<float> noiseMap)
        {

            float triangleWidth = DimX / NumX;
            float triangleHeigth = DimZ / NumZ;

            int vi = (NumX + 1) * z, ti = 2 * NumX * (z - 1);

            float xOffset = -0.25f * triangleWidth;
            float uOffset = 0f;

            int iA = -NumX - 2, iB = -NumX - 1, iC = -1, iD = 0;
            var tA = int3(iA, iC, iD);
            var tB = int3(iA, iD, iB);

            if ((z & 1) == 1)
            {
                xOffset = 0.25f * triangleWidth;
                uOffset = 0.5f / (NumX + 0.5f);
                tA = int3(iA, iC, iB);
                tB = int3(iB, iC, iD);
            }

            xOffset = xOffset - DimX / 2;

            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            vertex.position.x = xOffset;
            vertex.position.z = z * triangleHeigth;
            vertex.position.y = NativeArrayHelper.SampleValueAt(vertex.position.x + DimX / 2, vertex.position.z, Resolution, NumX + 1, noiseMap);

            vertex.texCoord0.x = uOffset / Tiling;
            vertex.texCoord0.y = (vertex.position.z / Tiling);

            stream.SetVertex(vi, vertex);
            vi += 1;

            for (int x = 1; x <= NumX; x++, vi++, ti += 2)
            {
                vertex.position.x = (float) x * triangleWidth + xOffset;
                vertex.position.y = NativeArrayHelper.SampleValueAt(vertex.position.x + DimX / 2, vertex.position.z, Resolution, NumX + 1, noiseMap);

                vertex.texCoord0.x = (vertex.position.x / Tiling);
                stream.SetVertex(vi, vertex);

                if (z > 0)
                {
                    stream.SetTriangle(
                        ti + 0, vi + tA 
                    );
                    stream.SetTriangle(
                        ti + 1, vi + tB 
                    );
                }
            }

        }

 
    }
}