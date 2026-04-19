using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public static class Phase1LevelCatalog
{
    public static IReadOnlyList<Phase1LevelDefinition> All { get; } =
    [
        CreateAuthorshipPrototype()
    ];

    private static Phase1LevelDefinition CreateAuthorshipPrototype()
    {
        var westVent = new HexCoord(-1, 0);
        var amplifier = new HexCoord(0, 0);
        var eastVent = new HexCoord(1, 0);

        var entries = new (HexCoord coord, Node node)[]
        {
            (westVent, CreateNode(NodeType.Vent, 95, facing: HexDirection.E, connections: OpenOnly(HexDirection.E))),
            (amplifier, CreateNode(NodeType.Amplifier, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
            (eastVent, CreateNode(NodeType.Vent, 95, facing: HexDirection.W, connections: OpenOnly(HexDirection.W))),
            (new HexCoord(0, 1), CreateNode(NodeType.Bulwark, 0)),
            (new HexCoord(-2, 1), CreateNode(NodeType.Cell, 12)),
            (new HexCoord(-1, 1), CreateNode(NodeType.Cell, 8)),
            (new HexCoord(2, -1), CreateNode(NodeType.Cell, 74))
        };

        return new Phase1LevelDefinition(
            Id: "phase1_authorship_prototype",
            DisplayName: "Phase 1 Authorship Prototype",
            Board: CreateBoard(entries),
            MoveCap: 6,
            Objective: new TaggedClusterObjective(
                Name: "Pop the tagged chain cluster",
                TargetCoords: [westVent, amplifier, eastVent]),
            SolverMaxDepth: 6,
            MinimumDistinctSolutions: 2);
    }

    private static GameBoard CreateBoard(IEnumerable<(HexCoord coord, Node node)> entries)
    {
        var entryArray = entries.ToArray();
        var grid = new HexGrid<Node>(entryArray.Select(entry => entry.coord));
        foreach (var (coord, node) in entryArray)
        {
            grid.Set(coord, node);
        }

        return new GameBoard(grid);
    }

    private static Node CreateNode(
        NodeType type,
        int pressure,
        HexDirection? facing = null,
        ConnectionMask? connections = null,
        NodeModifiers modifiers = NodeModifiers.None)
    {
        return new Node(
            type,
            pressure,
            Facing: facing,
            Connections: connections ?? ConnectionMask.AllOpen(),
            modifiers);
    }

    private static ConnectionMask OpenOnly(params HexDirection[] directions)
    {
        var mask = ConnectionMask.AllClosed();
        foreach (var direction in directions)
        {
            mask = mask.With(direction, open: true);
        }

        return mask;
    }
}
