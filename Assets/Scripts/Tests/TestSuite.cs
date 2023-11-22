using NUnit.Framework;
using Unity.Collections;
using LiquidPlanet;
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
        NativeList<int> list = new(4, Allocator.Persistent);
        list.AddNoResize(1);
        list.AddNoResize(2);
        list.AddNoResize(3);
        list.AddNoResize(4);
        NativeCollectionHelper.IncrementAt(list, 0);
        NativeCollectionHelper.IncrementAt(list, 3);
        Assert.That(list[0], Is.EqualTo(2));
        Assert.That(list[1], Is.EqualTo(2));
        Assert.That(list[2], Is.EqualTo(3));
        Assert.That(list[3], Is.EqualTo(5));
        list.Dispose();
    }

    [Test]
    public void IncrementThrowsExceptionAtInvalidIndex()
    {
        NativeList<int> list = new(4, Allocator.Persistent);
        list.AddNoResize(1);
        list.AddNoResize(2);
        list.AddNoResize(3);
        list.AddNoResize(4);
        Assert.That( () => NativeCollectionHelper.IncrementAt(list, 4), 
            Throws.Exception.TypeOf<IndexOutOfRangeException>());
        list.Dispose();
    }

}
