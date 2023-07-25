using Unity.Collections;
using UnityEngine;

namespace LiquidPlanet
{

    public interface IMeshGenerator
    {

        int VertexCount { get; }

        int IndexCount { get; }

        int JobLength { get; }

        Bounds Bounds { get; }

        int resolution { get; set; }

        float DimZ { get; set; }

        float DimX { get; set; }

        float Tiling { get; set; }

        float Height { get; set; }

        public NativeArray<float> NoiseMap { set; }

        void Execute<S>(int i,
        VertexStream stream);
    }
}

