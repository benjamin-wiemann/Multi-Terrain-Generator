using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace Waterworld
{

    public struct SharedTriangleGrid : IMeshGenerator
    {
        public int VertexCount => (NumX + 1) * (NumZ + 1);

        public int IndexCount => 6 * NumZ * NumX;

        public int JobLength => NumZ + 1;

        public Bounds Bounds => new Bounds(new Vector3(-0.5f * dimX, 0f, 0f), new Vector3((0.5f + 0.5f / Resolution) * dimX, 0f, dimZ)); //dimZ * sqrt(3f) / 2f));

        public int Resolution { get; set; }

        public float dimZ { get; set; }

        public float dimX { get; set; }

        // number of triangle pairs in x direction
        int NumX => (int)round(Resolution * dimX);

        // number of triangle pairs in z direction is higher, since its height is smaller than its width
        int NumZ => (int)round(Resolution * dimZ * 2f / sqrt(3f));

        public void Execute<S>(int z, VertexStream stream)
        {

            float triangleWidth = dimX / NumX;
            float triangleHeigth = dimZ / NumZ;

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

            xOffset = xOffset - dimX / 2;

            var vertex = new Vertex();
            vertex.normal.y = 1f;
            vertex.tangent.xw = float2(1f, -1f);

            vertex.position.x = xOffset;
            vertex.position.z = z * triangleHeigth;

            vertex.texCoord0.x = uOffset;
            vertex.texCoord0.y = vertex.position.z / dimZ;

            stream.SetVertex(vi, vertex);
            vi += 1;

            for (int x = 1; x <= NumX; x++, vi++, ti += 2)
            {
                vertex.position.x = (float)x * triangleWidth + xOffset;

                // Texture gets stretched over entire grid no matter the dimensions
                vertex.texCoord0.x = x / (NumX + 0.5f) + uOffset;
                stream.SetVertex(vi, vertex);

                if (z > 0)
                {
                    stream.SetTriangle(
                        ti + 0, vi + tA //int3(-NumX - 2, -1, -NumX - 1)
                    );
                    stream.SetTriangle(
                        ti + 1, vi + tB //int3(-NumX - 1, -1, 0)
                    );
                }
            }

        }
    }
}