using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.Threading;

namespace LiquidPlanet.Helper
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TopKSorter 
    {
        private Int9 _ids;
        private Float9 _values; // Array of float values to sort

        uint _count;

        public TopKSorter(Int9 inputIndices, Float9 inputValues)
        {
            _ids = inputIndices;
            _values = inputValues;
            _count = _ids.Length;
        }

        // Returns the top 4 highest numbers
        public void GetTopFour( out int4 topIds, out float4 topValues)
        {
            topIds = 0;
            topValues = 0;

            BuildMaxHeap();

            int topId;
            float topVal;
            for (int i = 0; i < 4; i++)
            {                
                ExtractMax(out topId, out topVal);
                topIds[i] = topId;
                topValues[i] = topVal;
            }

        }

        // Builds a max heap
        private void BuildMaxHeap()
        {
            for (int i = ((int) _ids.Length / 2) - 1; i >= 0; i--)
            {
                HeapifyDown((uint) i, _count);
            }
        }

        // Extracts the max value from the heap
        private void ExtractMax( out int maxId, out float maxValue)
        {
            maxId = _ids[0];
            maxValue = _values[0];

            _ids[0] = _ids[_count -1];
            _values[0] = _values[_count - 1];
            
            _count--;
            HeapifyDown(0, _count);
        }

        // Maintains the max-heap property
        private void HeapifyDown(uint index, uint size)
        {
            uint largest = index;
            uint left = 2 * index;
            uint right = 2 * index + 1;

            if (left < size && _values[left] > _values[largest])
            {
                largest = left;
            }

            if (right < size && _values[right] > _values[largest])
            {
                largest = right;
            }

            if (largest != index)
            {
                Swap(index, largest);
                HeapifyDown(largest, size);
            }
        }

        // Swaps two elements in the array
        private void Swap(uint i, uint j)
        {
            int tempId = _ids[i];
            float tempValue = _values[i];
            _ids[i] = _ids[j];
            _values[i] = _values[j];
            _ids[j] = tempId;
            _values[j] = tempValue;
        }


    }
}

