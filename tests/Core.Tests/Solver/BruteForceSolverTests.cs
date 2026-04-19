using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using PressureChain.Core.Solver;
using System.Diagnostics;

namespace PressureChain.Core.Tests.Solver;

public sealed class BruteForceSolverTests
{
    private static readonly HexCoord West = new(-1, 0);
    private static readonly HexCoord Center = new(0, 0);
    private static readonly HexCoord East = new(1, 0);

    private readonly BruteForceSolver _solver = new(new LevelEngine(new ActionResolver(new ChainResolver())));

    [Fact]
    public void Solve_OneCriticalCellAndOneMove_ReturnsSolvableInOneMove()
    {
        var initial = new LevelState(
            Board: CreateBoard((Center, CreateNode(NodeType.Cell, 95))),
            MovesRemaining: 1,
            Objective: CreateObjective(Center),
            ScoreAccumulated: 0,
            PoppedTargetCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        var result = _solver.Solve(initial, maxDepth: 1);

        Assert.True(result.Solvable);
        Assert.Equal(1, result.MinMovesUsed);
        Assert.Equal([new TriggerEarlyAction(Center)], result.ExampleSolution);
        Assert.Equal(1, result.DistinctSolutionsFound);
    }

    [Fact]
    public void Solve_NoMovesRemaining_ReturnsUnsolvable()
    {
        var initial = new LevelState(
            Board: CreateBoard((Center, CreateNode(NodeType.Cell, 95))),
            MovesRemaining: 0,
            Objective: CreateObjective(Center),
            ScoreAccumulated: 0,
            PoppedTargetCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        var result = _solver.Solve(initial, maxDepth: 1);

        Assert.False(result.Solvable);
        Assert.Equal(0, result.MinMovesUsed);
        Assert.Empty(result.ExampleSolution);
        Assert.Equal(0, result.DistinctSolutionsFound);
    }

    [Fact]
    public void Solve_SymmetricVentChain_FindsTwoDistinctSolutions()
    {
        var initial = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Vent, 95, facing: HexDirection.E, connections: OpenOnly(HexDirection.E))),
                (Center, CreateNode(NodeType.Amplifier, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
                (East, CreateNode(NodeType.Vent, 95, facing: HexDirection.W, connections: OpenOnly(HexDirection.W)))),
            MovesRemaining: 1,
            Objective: CreateObjective(West, Center, East),
            ScoreAccumulated: 0,
            PoppedTargetCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        var result = _solver.Solve(initial, maxDepth: 1);

        Assert.True(result.Solvable);
        Assert.Equal(1, result.MinMovesUsed);
        Assert.True(result.DistinctSolutionsFound >= 2);
        Assert.Contains(result.ExampleSolution.Single(), new PlayerAction[]
        {
            new TriggerEarlyAction(West),
            new TriggerEarlyAction(East)
        });
    }

    [Fact(Timeout = 30000)]
    public async Task Solve_FourByThreeBoard_CompletesWithinThirtySeconds()
    {
        var entries = new List<(HexCoord coord, Node node)>();

        for (var r = 0; r < 3; r++)
        {
            for (var q = 0; q < 4; q++)
            {
                var coord = new HexCoord(q, r);
                var node = (q, r) switch
                {
                    (1, 1) => CreateNode(NodeType.Cell, 95),
                    (3, 2) => CreateNode(NodeType.Amplifier, 35),
                    _ => CreateNode(NodeType.Bulwark, 0)
                };

                entries.Add((coord, node));
            }
        }

        var initial = new LevelState(
            Board: CreateBoard(entries.ToArray()),
            MovesRemaining: 12,
            Objective: CreateObjective(new HexCoord(1, 1)),
            ScoreAccumulated: 0,
            PoppedTargetCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        var stopwatch = Stopwatch.StartNew();
        var result = await Task.Run(() => _solver.Solve(initial, maxDepth: 12));
        stopwatch.Stop();

        Assert.True(result.Solvable);
        Assert.Equal(1, result.MinMovesUsed);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Solver took {stopwatch.Elapsed}.");
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

    private static TaggedClusterObjective CreateObjective(params HexCoord[] coords)
    {
        return new TaggedClusterObjective("Test cluster", coords);
    }
}
