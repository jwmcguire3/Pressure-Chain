using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Grid;

public sealed class HexGridTests
{
    [Fact]
    public void Get_ForCoordinateNotInGrid_ReturnsNull()
    {
        var grid = new HexGrid<string>(new[]
        {
            new HexCoord(0, 0)
        });

        var value = grid.Get(new HexCoord(1, 0));

        Assert.Null(value);
    }

    [Fact]
    public void Set_ThenGet_RoundTripsValue()
    {
        var coord = new HexCoord(0, 0);
        var grid = new HexGrid<string>(new[] { coord });

        grid.Set(coord, "node");

        var value = grid.Get(coord);

        Assert.Equal("node", value);
    }

    [Fact]
    public void AllCoords_ReturnsEveryAddedCoordinateExactlyOnce()
    {
        var coords = new[]
        {
            new HexCoord(0, 0),
            new HexCoord(1, 0),
            new HexCoord(0, 1),
            new HexCoord(0, 0)
        };
        var grid = new HexGrid<string>(coords);

        var actual = grid.AllCoords().ToArray();

        Assert.Equal(3, actual.Length);
        Assert.Equal(3, actual.Distinct().Count());
        Assert.Contains(new HexCoord(0, 0), actual);
        Assert.Contains(new HexCoord(1, 0), actual);
        Assert.Contains(new HexCoord(0, 1), actual);
    }

    [Fact]
    public void RectangularGrid_SevenByNine_HasExactlySixtyThreeCells()
    {
        var coords = new List<HexCoord>();

        for (var q = 0; q < 7; q++)
        {
            for (var r = 0; r < 9; r++)
            {
                coords.Add(new HexCoord(q, r));
            }
        }

        var grid = new HexGrid<string>(coords);

        Assert.Equal(63, grid.AllCoords().Count());
    }
}
