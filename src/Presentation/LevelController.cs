using Godot;
using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Levels;
using PressureChain.Core.Telemetry;

namespace PressureChain.Presentation;

public partial class LevelController : Node2D
{
    private const int DebugHistoryLimit = 8;

    private readonly ActionResolver _actionResolver = new(new ChainResolver());
    private readonly Queue<string> _debugHistory = new();
    private IActionLogger _actionLogger = null!;
    private LevelEngine _levelEngine = null!;
    private LevelState _levelState = null!;
    private BoardNode _boardNode = null!;
    private Label _statusLabel = null!;
    private Label _debugLabel = null!;
    private bool _isAnimating;
    private bool _debugVisible = true;

    public override void _Ready()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var logPath = ProjectSettings.GlobalizePath($"user://playtest-{sessionId}.ndjson");
        _actionLogger = new JsonFileActionLogger(logPath);
        _levelEngine = new LevelEngine(_actionResolver, _actionLogger);
        _boardNode = GetNode<BoardNode>("BoardNode");
        _statusLabel = GetNode<Label>("CanvasLayer/StatusLabel");
        _debugLabel = GetNode<Label>("CanvasLayer/DebugLabel");
        _boardNode.ActionRequested += OnActionRequested;
        _boardNode.DebugEventRaised += AddDebugEntry;

        _levelState = Phase1TestLevelFactory.Create();
        _actionLogger.LogLevelStart(_levelState);
        _boardNode.DisplayBoard(_levelState.Board);
        AddDebugEntry("Debug overlay active. Press F3 to toggle.");
        AddDebugEntry("Validate inputs with left-click merge, right-click vent rotate, and Shift+click trigger early.");
        AddDebugEntry("Top-right amplifier lane is seeded so triggering near it should extend the chain by one extra cell.");
        AddDebugEntry($"Playtest logging: {logPath}");
        UpdateStatus();
        UpdateDebugLabel();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_text_caret_down") || (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F3))
        {
            _debugVisible = !_debugVisible;
            UpdateDebugLabel();
            AddDebugEntry(_debugVisible ? "Debug overlay shown." : "Debug overlay hidden.");
        }
    }

    private async void OnActionRequested(PlayerAction action)
    {
        if (_isAnimating || _levelState.Status != LevelStatus.InProgress)
        {
            return;
        }

        try
        {
            var actionOutcome = _actionResolver.ApplyDetailed(_levelState.Board, action);
            AddDebugEntry($"Applied {DebugEventFormatter.FormatAction(action)}");
            if (actionOutcome.ChainResolution is not null)
            {
                _isAnimating = true;
                _boardNode.SetInputEnabled(false);
                AddDebugEntry(DebugEventFormatter.FormatChainSummary(actionOutcome.ChainResolution.Value));
                await _boardNode.PlayChainResolutionAsync(actionOutcome.ChainResolution.Value);
            }

            _levelState = _levelEngine.PlayAction(_levelState, action);
            _boardNode.DisplayBoard(_levelState.Board);
            UpdateStatus();
            LogAmplifierLaneSnapshot();
        }
        catch (InvalidActionException exception)
        {
            AddDebugEntry($"Invalid action: {exception.Reason}");
            ShowTransientStatus(exception.Reason);
        }
        finally
        {
            _isAnimating = false;
            _boardNode.SetInputEnabled(true);
        }
    }

    private void UpdateStatus(string? message = null)
    {
        var objectiveText = _levelState.Status switch
        {
            LevelStatus.Won => "Objective cleared",
            LevelStatus.Lost => "Out of moves",
            _ => "Clear all charged Cells"
        };

        var statusText = $"Moves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}   {objectiveText}";
        if (!string.IsNullOrWhiteSpace(message))
        {
            statusText = $"{statusText}\n{message}";
        }

        _statusLabel.Text = statusText;
    }

    private void AddDebugEntry(string message)
    {
        var timestamp = Time.GetTimeStringFromSystem();
        _debugHistory.Enqueue($"[{timestamp}] {message}");
        while (_debugHistory.Count > DebugHistoryLimit)
        {
            _debugHistory.Dequeue();
        }

        GD.Print(message);
        UpdateDebugLabel();
    }

    private void UpdateDebugLabel()
    {
        _debugLabel.Visible = _debugVisible;
        if (!_debugVisible)
        {
            return;
        }

        var lines = new List<string>
        {
            "Debug Overlay (F3)",
            "Manual checks: merge, vent rotate, trigger early, amplifier lane"
        };

        lines.AddRange(_debugHistory);
        _debugLabel.Text = string.Join('\n', lines);
    }

    private void LogAmplifierLaneSnapshot()
    {
        var watchCoords = new[]
        {
            new PressureChain.Core.Grid.HexCoord(7, 0),
            new PressureChain.Core.Grid.HexCoord(8, 0),
            new PressureChain.Core.Grid.HexCoord(9, 0),
            new PressureChain.Core.Grid.HexCoord(9, 1)
        };

        var snapshot = string.Join(
            " | ",
            watchCoords.Select(coord => DebugEventFormatter.FormatNode(coord, _levelState.Board.NodeAt(coord))));

        AddDebugEntry($"Amplifier lane: {snapshot}");
    }

    private async void ShowTransientStatus(string message)
    {
        UpdateStatus(message);
        await ToSignal(GetTree().CreateTimer(1.5d), SceneTreeTimer.SignalName.Timeout);
        if (IsInsideTree())
        {
            UpdateStatus();
        }
    }
}
