using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public static class Phase1LevelCatalog
{
    public static IReadOnlyList<Phase1LevelDefinition> All { get; } =
    [
        CreateCellBasicsLevel(),
        CreateVentLessonLevel(),
        CreateBulwarkLessonLevel(),
        CreateCombinedChainLevel()
    ];

    private static Phase1LevelDefinition CreateCellBasicsLevel()
    {
        var first = new HexCoord(0, 0);
        var second = new HexCoord(1, 0);
        var third = new HexCoord(4, 0);

        var entries = new (HexCoord coord, Node node)[]
        {
            (first, CreateNode(NodeType.Cell, 20)),
            (second, CreateNode(NodeType.Cell, 30)),
            (third, CreateNode(NodeType.Cell, 50))
        };

        return new Phase1LevelDefinition(
            Id: "phase1_cell_basics",
            DisplayName: "Level 1: Cells",
            Board: CreateBoard(entries),
            MoveCap: 3,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            SolverMaxDepth: 3,
            MinimumDistinctSolutions: 2,
            DemonstrationActions:
            [
                new MergeAction(first, second),
                new TriggerEarlyAction(second),
                new TriggerEarlyAction(third)
            ],
            MinimumDemonstratedWaveCount: 1);
    }

    private static Phase1LevelDefinition CreateVentLessonLevel()
    {
        var source = new HexCoord(-1, 0);
        var vent = new HexCoord(0, 0);
        var target = new HexCoord(1, 0);

        var entries = new (HexCoord coord, Node node)[]
        {
            (source, CreateNode(NodeType.Cell, 95)),
            (vent, CreateNode(NodeType.Vent, 89, facing: HexDirection.NE)),
            (target, CreateNode(NodeType.Cell, 89))
        };

        return new Phase1LevelDefinition(
            Id: "phase1_vent_redirect",
            DisplayName: "Level 2: Redirect",
            Board: CreateBoard(entries),
            MoveCap: 2,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            SolverMaxDepth: 2,
            MinimumDistinctSolutions: 1,
            DemonstrationActions:
            [
                new VentRedirectAction(vent, HexDirection.E),
                new TriggerEarlyAction(source)
            ],
            MinimumDemonstratedWaveCount: 3);
    }

    private static Phase1LevelDefinition CreateBulwarkLessonLevel()
    {
        var source = new HexCoord(0, 0);
        var vent = new HexCoord(1, -1);
        var wall = new HexCoord(1, 0);
        var target = new HexCoord(2, -1);

        var entries = new (HexCoord coord, Node node)[]
        {
            (source, CreateNode(NodeType.Cell, 95)),
            (vent, CreateNode(NodeType.Vent, 89, facing: HexDirection.E)),
            (wall, CreateNode(NodeType.Bulwark, 0)),
            (target, CreateNode(NodeType.Cell, 89))
        };

        return new Phase1LevelDefinition(
            Id: "phase1_bulwark_route",
            DisplayName: "Level 3: Wall",
            Board: CreateBoard(entries),
            MoveCap: 1,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            SolverMaxDepth: 1,
            MinimumDistinctSolutions: 1,
            DemonstrationActions:
            [
                new TriggerEarlyAction(source)
            ],
            MinimumDemonstratedWaveCount: 3);
    }

    private static Phase1LevelDefinition CreateCombinedChainLevel()
    {
        var source = new HexCoord(-1, 0);
        var vent = new HexCoord(0, 0);
        var amplifier = new HexCoord(1, 0);
        var wall = new HexCoord(0, 1);
        var target = new HexCoord(2, 0);

        var entries = new (HexCoord coord, Node node)[]
        {
            (source, CreateNode(NodeType.Cell, 95)),
            (vent, CreateNode(NodeType.Vent, 89, facing: HexDirection.E)),
            (amplifier, CreateNode(NodeType.Amplifier, 89)),
            (wall, CreateNode(NodeType.Bulwark, 0)),
            (target, CreateNode(NodeType.Cell, 89))
        };

        return new Phase1LevelDefinition(
            Id: "phase1_combined_chain",
            DisplayName: "Level 4: Amplify",
            Board: CreateBoard(entries),
            MoveCap: 1,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            SolverMaxDepth: 1,
            MinimumDistinctSolutions: 1,
            DemonstrationActions:
            [
                new TriggerEarlyAction(source)
            ],
            MinimumDemonstratedWaveCount: 4);
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
