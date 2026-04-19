namespace PressureChain.Core.Board;

public static class PressureTick
{
    public static Board Apply(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var nextBoard = board;
        foreach (var coord in board.Coords)
        {
            var node = board.NodeAt(coord);
            var updated = ShouldTick(node)
                ? node with { Pressure = Math.Min(100, node.Pressure + TickRate(node.Type)) }
                : node;

            nextBoard = nextBoard.WithNode(coord, updated);
        }

        return nextBoard;
    }

    private static bool ShouldTick(Node node)
    {
        return node.Type != NodeType.Bulwark && !node.Modifiers.HasFlag(NodeModifiers.Frozen);
    }

    private static int TickRate(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Cell => 10,
            NodeType.Vent => 8,
            NodeType.Bulwark => 0,
            NodeType.Amplifier => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unsupported node type.")
        };
    }
}
