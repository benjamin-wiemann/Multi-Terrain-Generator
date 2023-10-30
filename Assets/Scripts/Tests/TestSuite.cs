using NUnit.Framework;
using Unity.Collections;
using LiquidPlanet;

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
        int result = NativeArrayHelper.SelectClosest(x, y, resolution, width, intsUnmanaged);

        Assert.That(result, Is.EqualTo(3));

        resolution = 1;
        width = 2;
        x = 1.3f;
        y = 1.2f;
        result = NativeArrayHelper.SelectClosest(x, y, resolution, width, intsUnmanaged);
        Assert.That(result, Is.EqualTo(4));
        intsUnmanaged.Dispose();

        //ints = { }
        //intsUnmanaged = new(ints, Allocator.Persistent);
    }


}
