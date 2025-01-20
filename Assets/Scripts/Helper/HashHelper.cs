using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace MultiTerrain.Helper
{
    public readonly struct SmallXXHash
    {

        const uint primeA = 0b10011110001101110111100110110001;
        const uint primeB = 0b10000101111010111100101001110111;
        const uint primeC = 0b11000010101100101010111000111101;
        const uint primeD = 0b00100111110101001110101100101111;
        const uint primeE = 0b00010110010101100110011110110001;

        readonly uint accumulator;

        public SmallXXHash(uint accumulator)
        {
            this.accumulator = accumulator;
        }

        public static implicit operator uint(SmallXXHash hash)
        {
            uint avalanche = hash.accumulator;
            avalanche ^= avalanche >> 15;
            avalanche *= primeB;
            avalanche ^= avalanche >> 13;
            avalanche *= primeC;
            avalanche ^= avalanche >> 16;
            return avalanche;
        }

        //public static implicit operator float(SmallXXHash hash)
        //{
        //    return BitConverter.ToSingle(BitConverter.GetBytes((uint) hash), 0);
        //}

        public static implicit operator SmallXXHash(uint accumulator) =>
            new SmallXXHash(accumulator);

        public static SmallXXHash Seed(uint seed) => seed + primeE;

        static uint RotateLeft(uint data, int steps) => (data << steps) | (data >> 32 - steps);

        public SmallXXHash Eat(int data) =>
            RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

        //public SmallXXHash Eat(float data)
        //{
        //    int intData = BitConverter.ToInt32(BitConverter.GetBytes(data), 0);
        //    return Eat(intData);
        //}

        public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;

    }

    public class HashHelper
    {
        // 2-component hash function borrowed from Inigo Quilez's
        // "Voronoi - smooth" ShaderToy demo:
        // https://www.shadertoy.com/view/ldB3zc
        public static float2 Hash2(float2 p)
        {
            p = float2(dot(p, float2(127.1f, 311.7f)),
                                    dot(p, float2(269.5f, 183.3f)));
            return frac(sin(p) * 43758.5453f);
        }
    }    
}
