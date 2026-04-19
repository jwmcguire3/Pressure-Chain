using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Tests.Levels;

public sealed class LevelEngineTests
{
    private static readonly HexCoord West = new(-1, 0);
    private static readonly HexCoord Center = new(0, 0);
    private static readonly HexCoord East = new(1, 0);

    private readonly LevelEngine _engine = new(new ActionResolver(new ChainResolver()));

    [Fact]
    public void PlayAction_FiveMovesSpentWithoutClearingObjective_BecomesLost()
    {
        var state = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Bulwark, 0)),
                (Center, CreateNode(NodeType.Vent, 0, facing: HexDirection.E)),
                (East, CreateNode(NodeType.Cell, 10))),
            MovesRemaining: 5,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            Status: LevelStatus.InProgress);

        for (var move = 0; move < 5; move++)
        {
            state = _engine.PlayAction(state, new VentRedirectAction(Center, HexDirection.E));
        }

        Assert.Equal(LevelStatus.Lost, state.Status);
        Assert.Equal(0, state.MovesRemaining);
    }

    [Fact]
    public void PlayAction_ObjectiveClearedInThreeMoves_WinsImmediatelyAndPreservesSevenMoves()
    {
        var state = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Vent, 70, facing: HexDirection.E, connections: OpenOnly(HexDirection.E))),
                (Center, CreateNode(NodeType.Cell, 70, connections: OpenOnly(HexDirection.W, HexDirection.E))),
                (East, CreateNode(NodeType.Cell, 70, connections: OpenOnly(HexDirection.W)))),
            MovesRemaining: 10,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            Status: LevelStatus.InProgress);

        state = _engine.PlayAction(state, new VentRedirectAction(West, HexDirection.E));
        state = _engine.PlayAction(state, new VentRedirectAction(West, HexDirection.E));
        state = _engine.PlayAction(state, new TriggerEarlyAction(West));

        Assert.Equal(LevelStatus.Won, state.Status);
        Assert.Equal(7, state.MovesRemaining);
    }

    [Fact]
    public void PlayAction_AddsChainScoreToAccumulatedScore()
    {
        var state = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.E))),
                (Center, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
                (East, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W)))),
            MovesRemaining: 5,
            Objective: new ClearAllOfTypeObjective(NodeType.Vent),
            ScoreAccumulated: 0,
            Status: LevelStatus.InProgress);

        var result = _engine.PlayAction(state, new TriggerEarlyAction(West));

        Assert.Equal(51, result.ScoreAccumulated);
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
}
