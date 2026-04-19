using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Grid;

public sealed class HexCoordTests
{
    [Fact]
    public void Neighbors_OfOrigin_AreInExpectedOrder()
    {
        var origin = new HexCoord(0, 0);

        var neighbors = origin.Neighbors().ToArray();

        var expected = new[]
        {
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1)
        };

        Assert.Equal(expected, neighbors);
    }

    [Fact]
    public void DistanceTo_FromOriginToThreeZero_IsThree()
    {
        var origin = new HexCoord(0, 0);
        var target = new HexCoord(3, 0);

        var distance = origin.DistanceTo(target);

        Assert.Equal(3, distance);
    }

    [Fact]
    public void DistanceTo_FromOriginToTwoMinusTwo_IsTwo()
    {
        var origin = new HexCoord(0, 0);
        var target = new HexCoord(2, -2);

        var distance = origin.DistanceTo(target);

        Assert.Equal(2, distance);
    }

    [Fact]
    public void DistanceTo_IsSymmetric_ForRandomPairs()
    {
        var random = new Random(12345);

        for (var i = 0; i < 100; i++)
        {
            var first = new HexCoord(random.Next(-20, 21), random.Next(-20, 21));
            var second = new HexCoord(random.Next(-20, 21), random.Next(-20, 21));

            Assert.Equal(first.DistanceTo(second), second.DistanceTo(first));
        }
    }
}
