using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using LiquidPlanet;
using LiquidPlanet.Helper;
using System;

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
        NativeArray<int> terrainSegmentation = new(segmentation, Allocator.Persistent);
        NativeArray<TerrainTypeUnmanaged> terrainTable = new(3, Allocator.Persistent);
        terrainTable[0] = new TerrainTypeUnmanaged("type 1", default, 2);
        terrainTable[1] = new TerrainTypeUnmanaged("type 2", default, 3);
        terrainTable[2] = new TerrainTypeUnmanaged("type 3", default, 4);
        NativeArray<int2> coordinates = new(segmentation.Length, Allocator.Persistent);
        SortCoordinatesJob.ScheduleParallel(
            terrainSegmentation,
            terrainTable,
            segmentation.Length / width,
            coordinates);
        int2[] coordinatesDesired = new int2[] {
            new int2 (0,0), new int2(0, 2),
            new int2 (1,0), new int2(1, 1), new int2(1, 2),
            new int2 (2, 0), new int2 (0,1), new int2(2, 1), new int2(2, 2)};
        //NativeArray<int2> desiredNative = new(coordinatesDesired, Allocator.Persistent);
        Assert.That(coordinates.ToArray(), Is.EqualTo(coordinatesDesired));
        terrainSegmentation.Dispose();
        terrainTable.Dispose();
        coordinates.Dispose();
    }


}
