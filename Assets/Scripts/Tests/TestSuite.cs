using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using MultiTerrain;
using MultiTerrain.Helper;
using System;
using MultiTerrain.DebugTools;
using MultiTerrain.Segmentation;


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
        float4 combi4 = new float4(0.9f, 0.4f, 0.2f, 0.1f);
        float4 combi3 = new float4(0.4f, 0.2f, 0.1f, 0);
        float4 combi2 = new float4(0.2f, 0.1f, 0, 0);
        float4 combi1 = new float4(0.1f, 0, 0, 0);
        float4[] segmentation = new float4[] { 
            combi4, combi3, combi2,
            combi2, combi3, combi1, 
            combi4, combi3, combi2 };
        NativeArray<TerrainCombination> terrainSegmentation = new(segmentation.Length, Allocator.Persistent);
        for (int i = 0; i < segmentation.Length; i++)
        {
            int4 ids = 0;         
            terrainSegmentation[i] = new TerrainCombination( ids, segmentation[i]);
        }        
        NativeArray<int> submeshCounters = new(4, Allocator.Persistent);
        submeshCounters[0] = 1;
        submeshCounters[1] = 3;
        submeshCounters[2] = 3;
        submeshCounters[3] = 2;
        NativeArray<int2> coordinates = new(segmentation.Length, Allocator.Persistent);
        int2[] coordinatesExpected = new int2[] {
            new int2(3, 2),
            new int2 (3, 1), new int2 (1,2), new int2(3, 3), 
            new int2 (2,1), new int2(2, 2), new int2(2, 3),
            new int2 (1,1), new int2(1, 3)
        };        

        JobTools.Get()._runParallel = false;
        SortCoordinatesJob.ScheduleParallel(
            terrainSegmentation,
            submeshCounters,
            segmentation.Length / width,
            coordinates);
        Assert.That(coordinates.ToArray(), Is.EqualTo(coordinatesExpected));

        coordinates.Dispose();
        coordinates = new(segmentation.Length, Allocator.Persistent);

        JobTools.Get()._runParallel = true;
        SortCoordinatesJob.ScheduleParallel(
            terrainSegmentation,
            submeshCounters,
            segmentation.Length / width,
            coordinates);
        Assert.That(coordinates.ToArray(), Is.EqualTo(coordinatesExpected));
        terrainSegmentation.Dispose();
        submeshCounters.Dispose();
        coordinates.Dispose();
    }

    [Test]
    public void Int9AddingElementsIncreasesLength()
    {
        Int9 test = new Int9();
        Assert.That(test.Length == 0);        
        test.Add(2);
        Assert.That(test.Length == 1);
        test[1] = 5;
        Assert.That(test.Length == 2);
    }

    [Test]
    public void Int9_Adding_Element_Returns_Correct_Index()
    {
        Int9 test = new Int9();              
        uint index = test.Add(2);
        Assert.That(index, Is.EqualTo(0));
        Assert.That(test[0], Is.EqualTo(2));
        test[1] = 3;
        Assert.That(test[1], Is.EqualTo(3));
    }

    [Test]
    public void Int9AddingAtTooHighIndexRaisesException()
    {
        Int9 test = new Int9();
        Assert.Throws<ArgumentException>(() => test[2] = 2);
        test.Add(0);
        test.Add(1);
        test.Add(2);
        test.Add(3);
        test.Add(4);
        test.Add(5);
        test.Add(6);
        test.Add(7);
        test.Add(8);
        Assert.Throws<ArgumentException>(() => test[9] = 9);
        Assert.Throws<ArgumentException>(() => test.Add(10));
    }

    [Test]
    public void TopKSorterGetsCorrectTopFour()
    {
        Int9 ids = new Int9(1, 2, 3, 4, 5, 6, 7, 8, 9, 9);
        Float9 values = new Float9( 0.4f, 0.3f, 0.1f, 0.8f, 0.9f, 0.2f, 0.7f, 0f, 0.5f);
        TopKSorter sorter = new(ids, values);
        int4 topIds;
        float4 topValues;        
        int numAboveThreshold;
        int k = 4;
        float threshold = 0.1f;
        sorter.GetKHighestValues(k, threshold, out topIds, out topValues, out numAboveThreshold);
        Assert.That(topValues[0], Is.EqualTo(0.9f));
        Assert.That(topValues[1], Is.EqualTo(0.8f));
        Assert.That(topValues[2], Is.EqualTo(0.7f));
        Assert.That(topValues[3], Is.EqualTo(0.5f));

    }

    [Test]
    public void TopKSorterGetsCorrectTopThree()
    {
        Int9 ids = new Int9(0, 2, 1, 0, 0, 0, 0, 0, 0, 3);
        Float9 values = new Float9(0, 0, 1, 0, 0, 0, 0, 0, 0);
        TopKSorter sorter = new(ids, values);
        int4 topIds;
        float4 topValues;
        int numAboveThreshold;
        int k = 3;
        float threshold = 0.0025f;
        sorter.GetKHighestValues(k, threshold, out topIds, out topValues, out numAboveThreshold);
        Assert.That(topValues[0], Is.EqualTo(1));
        Assert.That(topValues[1], Is.EqualTo(0));
        Assert.That(topIds[0], Is.EqualTo(1));
        Assert.That(topIds[1], Is.Not.EqualTo(1));
        Assert.That(numAboveThreshold, Is.EqualTo(1));

    }

    [Test]
    public void TopKSorterSortsFourEntriesInCorrectOrder()
    {
        int4 ids = new (4, 2, 3, 1);
        float4 values = new ( 0.4f, 0.3f, 0.1f, 0.8f);
        int k = 4;
        TopKSorter.SortTopKById(k, ref ids, ref values);
        Assert.That(ids[0], Is.EqualTo(1));
        Assert.That(ids[1], Is.EqualTo(2));
        Assert.That(ids[2], Is.EqualTo(3));
        Assert.That(ids[3], Is.EqualTo(4));
        Assert.That(values[0], Is.EqualTo(0.8f));
        Assert.That(values[1], Is.EqualTo(0.3f));
        Assert.That(values[2], Is.EqualTo(0.1f));
        Assert.That(values[3], Is.EqualTo(0.4f));

    }


}
