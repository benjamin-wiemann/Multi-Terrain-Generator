using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using LiquidPlanet;
using LiquidPlanet.Helper;
using System;
using LiquidPlanet.DebugTools;

public class TestSuite
{

    [Test]
    public void SelectClosestFindsCorrectValue()
    {
        float x = 0.3f;
        float y = 0.7f;
        int resolution = 2;
        int width = 1;
        int[] ints = { 1, 2, 3, 4 };
        NativeArray<int> intsUnmanaged = new(ints, Allocator.Persistent);
        int result = NativeCollectionHelper.SelectClosest(x, y, resolution, width, intsUnmanaged);

        Assert.That(result, Is.EqualTo(3));

        resolution = 1;
        width = 2;
        x = 1.3f;
        y = 1.2f;
        result = NativeCollectionHelper.SelectClosest(x, y, resolution, width, intsUnmanaged);
        Assert.That(result, Is.EqualTo(4));
        intsUnmanaged.Dispose();

        //ints = { }
        //intsUnmanaged = new(ints, Allocator.Persistent);
    }

    [Test]
    public void IncrementAtChangesCorrectElement()
    {
        NativeArray<int> array = new(4, Allocator.Persistent);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;
        array[3] = 4;
        NativeCollectionHelper.IncrementAt(array, 0);
        NativeCollectionHelper.IncrementAt(array, 3);
        Assert.That(array[0], Is.EqualTo(2));
        Assert.That(array[1], Is.EqualTo(2));
        Assert.That(array[2], Is.EqualTo(3));
        Assert.That(array[3], Is.EqualTo(5));
        array.Dispose();
    }

    [Test]
    public void IncrementThrowsExceptionAtInvalidIndex()
    {
        NativeArray<int> array = new(4, Allocator.Persistent);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;
        array[3] = 4;
        Assert.That(() => NativeCollectionHelper.IncrementAt(array, 4),
            Throws.Exception.TypeOf<IndexOutOfRangeException>());
        array.Dispose();
    }

    [Test]
    public void SortCoordinatesJobSortsCorrectly()
    {
        int width = 3;
        int[] segmentation = new int[] { 0, 1, 2, 2, 1, 2, 0, 1, 2 };
        NativeArray<TerrainInfo> terrainSegmentation = new(segmentation.Length, Allocator.Persistent);
        for (int i = 0; i < segmentation.Length; i++)
        {
            Float9 intensities = Float9.zero;
            intensities[0] = 1f;
            Int9 indices = Int9.zero;
            indices[0] = segmentation[i];
            TerrainInfo info = new TerrainInfo(indices, intensities);
            terrainSegmentation[i] = info;
        }        
        NativeArray<int> terrainCounters = new(3, Allocator.Persistent);
        terrainCounters[0] = 2;
        terrainCounters[1] = 3;
        terrainCounters[2] = 4;
        NativeArray<int2> coordinates = new(segmentation.Length, Allocator.Persistent);
        int2[] coordinatesExpected = new int2[] {
            new int2 (1,1), new int2(1, 3),
            new int2 (2,1), new int2(2, 2), new int2(2, 3),
            new int2 (3, 1), new int2 (1,2), new int2(3, 2), new int2(3, 3)};
        JobTools.Get()._runParallel = false;
        SortCoordinatesJob.ScheduleParallel(
            terrainSegmentation,
            terrainCounters,
            segmentation.Length / width,
            coordinates);
        Assert.That(coordinates.ToArray(), Is.EqualTo(coordinatesExpected));
        JobTools.Get()._runParallel = true;
        SortCoordinatesJob.ScheduleParallel(
            terrainSegmentation,
            terrainCounters,
            segmentation.Length / width,
            coordinates);
        NativeArray<int2> terrain0 = coordinates.GetSubArray(0, 2);
        Assert.That(terrain0.ToArray(), Contains.Item(coordinatesExpected[0]));
        Assert.That(terrain0.ToArray(), Contains.Item(coordinatesExpected[1]));
        NativeArray<int2> terrain1 = coordinates.GetSubArray(2, 3);
        Assert.That(terrain1.ToArray(), Contains.Item(coordinatesExpected[2]));
        Assert.That(terrain1.ToArray(), Contains.Item(coordinatesExpected[3]));
        Assert.That(terrain1.ToArray(), Contains.Item(coordinatesExpected[4]));
        NativeArray<int2> terrain2 = coordinates.GetSubArray(5, 4);
        Assert.That(terrain2.ToArray(), Contains.Item(coordinatesExpected[5]));
        Assert.That(terrain2.ToArray(), Contains.Item(coordinatesExpected[6]));
        Assert.That(terrain2.ToArray(), Contains.Item(coordinatesExpected[7]));
        Assert.That(terrain2.ToArray(), Contains.Item(coordinatesExpected[8]));
        terrainSegmentation.Dispose();
        terrainCounters.Dispose();
        coordinates.Dispose();
    }


}
