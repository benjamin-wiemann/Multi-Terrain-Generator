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
        int[] segmentation = new int[] { 0, 1, 2, 2, 1, 2, 0, 1, 2 };
        NativeArray<TerrainWeighting> terrainSegmentation = new(segmentation.Length, Allocator.Persistent);
        for (int i = 0; i < segmentation.Length; i++)
        {
            float4 intensities = 0;
            intensities[0] = 1f;
            int4 indices = new int4();
            indices[0] = segmentation[i];
            TerrainWeighting info = new TerrainWeighting(indices, intensities);
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
        Int9 ids = new Int9(1, 2, 3, 4, 5, 6, 7, 8, 9);
        Float9 values = new Float9( 0.4f, 0.3f, 0.1f, 0.8f, 0.9f, 0.2f, 0.7f, 0f, 0.5f);
        TopKSorter sorter = new(ids, values);
        int4 topIds;
        float4 topValues;
        sorter.GetFourHighestValues(out topIds, out topValues);
        Assert.That(topValues[0], Is.EqualTo(0.9f));
        Assert.That(topValues[1], Is.EqualTo(0.8f));
        Assert.That(topValues[2], Is.EqualTo(0.7f));
        Assert.That(topValues[3], Is.EqualTo(0.5f));

    }

    [Test]
    public void TopKSorterSortsFourEntriesInCorrectOrder()
    {
        int4 ids = new (4, 2, 3, 1);
        float4 values = new ( 0.4f, 0.3f, 0.1f, 0.8f);
        TopKSorter.SortTopFourById(ref ids, ref values);
        Assert.That(ids[0], Is.EqualTo(1));
        Assert.That(ids[1], Is.EqualTo(2));
        Assert.That(ids[2], Is.EqualTo(3));
        Assert.That(ids[3], Is.EqualTo(4));
        Assert.That(values[0], Is.EqualTo(0.8f));
        Assert.That(values[1], Is.EqualTo(0.3f));
        Assert.That(values[2], Is.EqualTo(0.1f));
        Assert.That(values[3], Is.EqualTo(0.4f));

    }

    [Test]
    public void IdCombination_MapIdsToIndices_Finds_Correct_Key_Value_Pairs()
    {
        NativeList<TerrainTypeStruct> terrainList = new (4, Allocator.Persistent);
        TerrainType protoType = new();
        protoType._name = "test";
        terrainList.Add(TerrainTypeStruct.Convert(protoType, 2));
        terrainList.Add(TerrainTypeStruct.Convert(protoType, 3));
        terrainList.Add(TerrainTypeStruct.Convert(protoType, 5));
        terrainList.Add(TerrainTypeStruct.Convert(protoType, 7));
        int k = 2;
        int binCoeff = MathHelper.GetBinCoeff(terrainList.Length, k);
        NativeHashMap<int, int> keyValues = new(binCoeff, Allocator.Persistent);
        IdCombination.MapIdsToIndices(terrainList, k, ref keyValues);
        Assert.That(keyValues.ContainsKey(2 * 3), Is.True);
        Assert.That(keyValues.ContainsKey(2 * 5), Is.True);
        Assert.That(keyValues.ContainsKey(2 * 7), Is.True);
        Assert.That(keyValues.ContainsKey(3 * 5), Is.True);
        Assert.That(keyValues.ContainsKey(3 * 7), Is.True);
        Assert.That(keyValues.ContainsKey(5 * 7), Is.True);

        NativeArray<int> values = keyValues.GetValueArray( Allocator.Persistent);
        Assert.That(values.Contains(0), Is.True);
        Assert.That(values.Contains(1), Is.True);
        Assert.That(values.Contains(2), Is.True);
        Assert.That(values.Contains(3), Is.True);
        Assert.That(values.Contains(4), Is.True);
        Assert.That(values.Contains(5), Is.True);

        terrainList.Dispose();
        keyValues.Dispose();
    }



}
