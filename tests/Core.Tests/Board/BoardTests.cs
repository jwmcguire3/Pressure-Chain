using PressureChain.Core.Board;
using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Board;

public sealed class BoardTests
{
    [Fact]
    public void WithNode_ReturnsNewBoardWithReplacementNode()
    {
        var coord = new HexCoord(0, 0);
        var original = CreateBoard((coord, CreateNode(NodeType.Cell, 30)));

        var updated = original.WithNode(coord, CreateNode(NodeType.Vent, 38));

        Assert.NotSame(original, updated);
        Assert.Equal(30, original.NodeAt(coord).Pressure);
        Assert.Equal(NodeType.Vent, updated.NodeAt(coord).Type);
        Assert.Equal(38, updated.NodeAt(coord).Pressure);
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
