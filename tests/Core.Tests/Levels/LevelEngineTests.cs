using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using PressureChain.Core.Telemetry;

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
            Objective: CreateObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        for (var move = 0; move < 5; move++)
        {
            state = _engine.PlayAction(state, new VentRedirectAction(Center, HexDirection.E));
        }

        Assert.Equal(LevelStatus.Lost, state.Status);
        Assert.Equal(0, state.MovesRemaining);
    }

    [Fact]
    public void PlayAction_ClearingAllCellsAcrossActions_WinsImmediatelyAndPreservesRemainingMoves()
    {
        var first = new HexCoord(0, 0);
        var second = new HexCoord(1, 0);
        var third = new HexCoord(4, 0);
        var state = new LevelState(
            Board: CreateBoard(
                (first, CreateNode(NodeType.Cell, 20)),
                (second, CreateNode(NodeType.Cell, 30)),
                (third, CreateNode(NodeType.Cell, 50))),
            MovesRemaining: 3,
            Objective: CreateObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        state = _engine.PlayAction(state, new MergeAction(first, second));
        state = _engine.PlayAction(state, new TriggerEarlyAction(second));
        state = _engine.PlayAction(state, new TriggerEarlyAction(third));

        Assert.Equal(LevelStatus.Won, state.Status);
        Assert.Equal(0, state.MovesRemaining);
        Assert.Equal(3, state.ClearedCoords.Count);
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
            Objective: CreateObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        var result = _engine.PlayAction(state, new TriggerEarlyAction(West));

        Assert.Equal(51, result.ScoreAccumulated);
    }

    [Fact]
    public void PlayAction_LogsExactlyOneActionRecordPerAction()
    {
        var logger = new RecordingActionLogger();
        var engine = new LevelEngine(new ActionResolver(new ChainResolver()), logger);
        var state = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.E))),
                (Center, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
                (East, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W)))),
            MovesRemaining: 5,
            Objective: CreateObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);

        engine.PlayAction(state, new TriggerEarlyAction(West));

        Assert.Single(logger.ActionRecords);
    }

    [Fact]
    public void PlayAction_WithNullActionLogger_MatchesDefaultBehavior()
    {
        var actionResolver = new ActionResolver(new ChainResolver());
        var state = new LevelState(
            Board: CreateBoard(
                (West, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.E))),
                (Center, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W, HexDirection.E))),
                (East, CreateNode(NodeType.Cell, 90, connections: OpenOnly(HexDirection.W)))),
            MovesRemaining: 5,
            Objective: CreateObjective(NodeType.Cell),
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);
        var action = new TriggerEarlyAction(West);

        var defaultResult = new LevelEngine(actionResolver).PlayAction(state, action);
        var nullLoggerResult = new LevelEngine(actionResolver, new NullActionLogger()).PlayAction(state, action);

        Assert.Equal(defaultResult.MovesRemaining, nullLoggerResult.MovesRemaining);
        Assert.Equal(defaultResult.ScoreAccumulated, nullLoggerResult.ScoreAccumulated);
        Assert.Equal(defaultResult.Status, nullLoggerResult.Status);
        Assert.Equal(defaultResult.ClearedCoords, nullLoggerResult.ClearedCoords);
        Assert.Equal(defaultResult.Board.Coords, nullLoggerResult.Board.Coords);
        foreach (var coord in defaultResult.Board.Coords)
        {
            Assert.Equal(defaultResult.Board.NodeAt(coord), nullLoggerResult.Board.NodeAt(coord));
        }
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

    private static ClearAllOfTypeObjective CreateObjective(NodeType targetType)
    {
        return new ClearAllOfTypeObjective(targetType);
    }

    private sealed class RecordingActionLogger : IActionLogger
    {
        public List<(PlayerAction action, LevelState before, LevelState after)> ActionRecords { get; } = [];

        public void LogAction(PlayerAction action, LevelState before, LevelState after)
        {
            ActionRecords.Add((action, before, after));
        }

        public void LogLevelStart(LevelState initial)
        {
        }

        public void LogLevelEnd(LevelStatus outcome, int finalScore)
        {
        }
    }
}
