using System.Globalization;
using System.Text;
using System.Text.Json;
using PressureChain.Core.Actions;
using PressureChain.Core.Levels;

namespace PressureChain.Core.Telemetry;

public sealed class JsonFileActionLogger : IActionLogger
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly object _syncRoot = new();
    private LevelState? _lastKnownState;

    public JsonFileActionLogger(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path must not be empty.", nameof(filePath));
        }

        _filePath = filePath;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void LogAction(PlayerAction action, LevelState before, LevelState after)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        lock (_syncRoot)
        {
            AppendRecord(new ActionLogRecord(
                Timestamp: CreateTimestamp(),
                EventType: "action",
                Action: CreateActionSnapshot(action),
                Before: CreateStateSnapshot(before),
                After: CreateStateSnapshot(after)));

            _lastKnownState = after;
        }
    }

    public void LogLevelStart(LevelState initial)
    {
        ArgumentNullException.ThrowIfNull(initial);

        lock (_syncRoot)
        {
            AppendRecord(new LevelStartLogRecord(
                Timestamp: CreateTimestamp(),
                EventType: "level_start",
                State: CreateStateSnapshot(initial)));

            _lastKnownState = initial;
        }
    }

    public void LogLevelEnd(LevelStatus outcome, int finalScore)
    {
        lock (_syncRoot)
        {
            AppendRecord(new LevelEndLogRecord(
                Timestamp: CreateTimestamp(),
                EventType: "level_end",
                Outcome: outcome.ToString(),
                FinalScore: finalScore,
                State: _lastKnownState is null ? null : CreateStateSnapshot(_lastKnownState)));
        }
    }

    private static string CreateTimestamp()
    {
        return DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    }

    private static ActionSnapshot CreateActionSnapshot(PlayerAction action)
    {
        return action switch
        {
            MergeAction mergeAction => new ActionSnapshot(
                Type: "merge",
                Coordinates: [FormatCoord(mergeAction.A), FormatCoord(mergeAction.B)],
                Facing: null),
            TriggerEarlyAction triggerEarlyAction => new ActionSnapshot(
                Type: "trigger_early",
                Coordinates: [FormatCoord(triggerEarlyAction.Target)],
                Facing: null),
            VentRedirectAction ventRedirectAction => new ActionSnapshot(
                Type: "vent_redirect",
                Coordinates: [FormatCoord(ventRedirectAction.Target)],
                Facing: ventRedirectAction.NewFacing.ToString()),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported action type.")
        };
    }

    private static LevelStateSnapshot CreateStateSnapshot(LevelState state)
    {
        return new LevelStateSnapshot(
            MovesRemaining: state.MovesRemaining,
            ScoreAccumulated: state.ScoreAccumulated,
            Status: state.Status.ToString(),
            Objective: state.Objective.ToString() ?? state.Objective.GetType().Name,
            Board: state.Board.Coords
                .Select(coord =>
                {
                    var node = state.Board.NodeAt(coord);
                    return new NodeSnapshot(
                        Coord: FormatCoord(coord),
                        Type: node.Type.ToString(),
                        Pressure: node.Pressure);
                })
                .ToArray());
    }

    private static string FormatCoord(PressureChain.Core.Grid.HexCoord coord)
    {
        return $"{coord.Q},{coord.R}";
    }

    private void AppendRecord<TRecord>(TRecord record)
    {
        var json = JsonSerializer.Serialize(record, SerializerOptions);
        using var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.WriteLine(json);
    }

    private sealed record ActionLogRecord(
        string Timestamp,
        string EventType,
        ActionSnapshot Action,
        LevelStateSnapshot Before,
        LevelStateSnapshot After);

    private sealed record LevelStartLogRecord(
        string Timestamp,
        string EventType,
        LevelStateSnapshot State);

    private sealed record LevelEndLogRecord(
        string Timestamp,
        string EventType,
        string Outcome,
        int FinalScore,
        LevelStateSnapshot? State);

    private sealed record ActionSnapshot(
        string Type,
        string[] Coordinates,
        string? Facing);

    private sealed record LevelStateSnapshot(
        int MovesRemaining,
        int ScoreAccumulated,
        string Status,
        string Objective,
        NodeSnapshot[] Board);

    private sealed record NodeSnapshot(
        string Coord,
        string Type,
        int Pressure);
}
