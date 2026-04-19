using PressureChain.Core.Board;
using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Board;

public sealed class PressureTickTests
{
    [Fact]
    public void Apply_CellAtPressure30_Becomes40()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Cell, 30)));

        var updated = PressureTick.Apply(board);

        Assert.Equal(40, updated.NodeAt(coord).Pressure);
    }

    [Fact]
    public void Apply_CellAtPressure95_ClampsTo100()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Cell, 95)));

        var updated = PressureTick.Apply(board);

        Assert.Equal(100, updated.NodeAt(coord).Pressure);
    }

    [Fact]
    public void Apply_BulwarkAtPressure0_Stays0()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Bulwark, 0)));

        var updated = PressureTick.Apply(board);

        Assert.Equal(0, updated.NodeAt(coord).Pressure);
    }

    [Fact]
    public void Apply_FrozenCell_StaysAtCurrentPressure()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Cell, 30, NodeModifiers.Frozen)));

        var updated = PressureTick.Apply(board);

        Assert.Equal(30, updated.NodeAt(coord).Pressure);
    }

    [Fact]
    public void Apply_ReturnsDifferentBoardInstance()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Cell, 30)));

        var updated = PressureTick.Apply(board);

        Assert.NotSame(board, updated);
    }

    [Fact]
    public void Apply_DoesNotMutateOriginalBoard()
    {
        var coord = new HexCoord(0, 0);
        var board = CreateBoard((coord, CreateNode(NodeType.Cell, 30)));

        _ = PressureTick.Apply(board);

        Assert.Equal(30, board.NodeAt(coord).Pressure);
    }

    private static PressureChain.Core.Board.Board CreateBoard(params (HexCoord coord, Node node)[] entries)
    {
        var grid = new HexGrid<Node>(entries.Select(entry => entry.coord));
        foreach (var (coord, node) in entries)
        {
            grid.Set(coord, node);
        }

        return new PressureChain.Core.Board.Board(grid);
    }

    private static Node CreateNode(NodeType type, int pressure, NodeModifiers modifiers = NodeModifiers.None)
    {
        return new Node(
            type,
            pressure,
            Facing: null,
            Connections: ConnectionMask.AllOpen(),
            modifiers);
    }
}
