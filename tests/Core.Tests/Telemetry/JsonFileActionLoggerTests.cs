using System.Text.Json;
using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using PressureChain.Core.Telemetry;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Tests.Telemetry;

public sealed class JsonFileActionLoggerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"pressure-chain-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Logger_WritesParseableNdjsonRecords()
    {
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "session.ndjson");
        var logger = new JsonFileActionLogger(filePath);
        var initial = CreateState(movesRemaining: 3, scoreAccumulated: 0, status: LevelStatus.InProgress, centerPressure: 10);
        var after = initial with { MovesRemaining = 2, ScoreAccumulated = 25, Board = CreateBoard((Center, CreateNode(NodeType.Cell, 0))) };

        logger.LogLevelStart(initial);
        logger.LogAction(new TriggerEarlyAction(Center), initial, after);
        logger.LogLevelEnd(LevelStatus.Won, after.ScoreAccumulated);

        var lines = File.ReadAllLines(filePath);

        Assert.Equal(3, lines.Length);

        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            Assert.True(document.RootElement.TryGetProperty("timestamp", out _));
            Assert.True(document.RootElement.TryGetProperty("eventType", out _));
        }

        using var actionDocument = JsonDocument.Parse(lines[1]);
        var actionRecord = actionDocument.RootElement;
        Assert.Equal("action", actionRecord.GetProperty("eventType").GetString());
        Assert.Equal("trigger_early", actionRecord.GetProperty("action").GetProperty("type").GetString());
        Assert.Equal("0,0", actionRecord.GetProperty("after").GetProperty("board")[0].GetProperty("coord").GetString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static readonly HexCoord Center = new(0, 0);

    private static LevelState CreateState(int movesRemaining, int scoreAccumulated, LevelStatus status, int centerPressure)
    {
        return new LevelState(
            Board: CreateBoard((Center, CreateNode(NodeType.Cell, centerPressure))),
            MovesRemaining: movesRemaining,
            Objective: new ClearAllOfTypeObjective(NodeType.Cell),
            ScoreAccumulated: scoreAccumulated,
            Status: status);
    }

    private static GameBoard CreateBoard(params (HexCoord coord, Node node)[] entries)
    {
        var grid = new HexGrid<Node>(entries.Select(entry => entry.coord));
        foreach (var (coord, node) in entries)
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
}
