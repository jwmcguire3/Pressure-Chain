using Godot;
using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using PressureChain.Core.Telemetry;

namespace PressureChain.Presentation;

public partial class LevelController : Node2D
{
    private readonly IReadOnlyList<Phase1LevelDefinition> _levels = Phase1LevelCatalog.All;
    private readonly ActionResolver _actionResolver = new(new ChainResolver());
    private IActionLogger _actionLogger = null!;
    private LevelEngine _levelEngine = null!;
    private LevelState _levelState = null!;
    private BoardNode _boardNode = null!;
    private Label _statusLabel = null!;
    private int _levelIndex;
    private bool _isAnimating;

    public override void _Ready()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var logPath = ProjectSettings.GlobalizePath($"user://playtest-{sessionId}.ndjson");
        _actionLogger = new JsonFileActionLogger(logPath);
        _levelEngine = new LevelEngine(_actionResolver, _actionLogger);
        _boardNode = GetNode<BoardNode>("BoardNode");
        _statusLabel = GetNode<Label>("CanvasLayer/StatusLabel");
        _boardNode.ActionRequested += OnActionRequested;

        LoadLevel(_levelIndex);
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
            if (actionOutcome.ChainResolution is not null)
            {
                _isAnimating = true;
                _boardNode.SetInputEnabled(false);
                await _boardNode.PlayChainResolutionAsync(actionOutcome.ChainResolution.Value);
            }

            _levelState = _levelEngine.PlayAction(_levelState, action);
            _boardNode.DisplayBoard(_levelState.Board, GetObjectiveCoords(_levelState), _levelState.ClearedCoords);
            UpdateStatus();

            if (_levelState.Status == LevelStatus.Won)
            {
                await AdvanceAfterWinAsync();
            }
            else if (_levelState.Status == LevelStatus.Lost)
            {
                await RestartAfterLossAsync();
            }
        }
        catch (InvalidActionException)
        {
        }
        finally
        {
            _isAnimating = false;
            _boardNode.SetInputEnabled(true);
        }
    }

    private void UpdateStatus(string? message = null)
    {
        var level = _levels[_levelIndex];
        var objectiveSummary = GetObjectiveSummary(_levelState);

        var statusText = _levelState.Status switch
        {
            LevelStatus.Won => $"Level {_levelIndex + 1}/{_levels.Count}: {level.DisplayName}\nMoves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}\n{objectiveSummary}\nBoard cleared. Loading next level...",
            LevelStatus.Lost => $"Level {_levelIndex + 1}/{_levels.Count}: {level.DisplayName}\nMoves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}\n{objectiveSummary}\nOut of moves. Resetting level...",
            _ => $"Level {_levelIndex + 1}/{_levels.Count}: {level.DisplayName}\nMoves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}\n{objectiveSummary}\nMerge adjacent matches. Right-click Vents. Shift+click 50+ pressure."
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            statusText = $"{statusText}\n{message}";
        }

        _statusLabel.Text = statusText;
    }

    private void LoadLevel(int levelIndex)
    {
        _levelIndex = levelIndex;
        _levelState = _levels[levelIndex].CreateInitialState();
        _actionLogger.LogLevelStart(_levelState);
        _boardNode.SetInputEnabled(true);
        _boardNode.DisplayBoard(_levelState.Board, GetObjectiveCoords(_levelState), _levelState.ClearedCoords);
        UpdateStatus();
    }

    private async Task AdvanceAfterWinAsync()
    {
        _isAnimating = true;
        _boardNode.SetInputEnabled(false);
        await ToSignal(GetTree().CreateTimer(0.9d), SceneTreeTimer.SignalName.Timeout);

        if (_levelIndex + 1 < _levels.Count)
        {
            LoadLevel(_levelIndex + 1);
        }
        else
        {
            _boardNode.SetInputEnabled(false);
            UpdateStatus("Phase 1 rescue slice complete.");
        }

        _isAnimating = false;
    }

    private async Task RestartAfterLossAsync()
    {
        _isAnimating = true;
        _boardNode.SetInputEnabled(false);
        await ToSignal(GetTree().CreateTimer(1.1d), SceneTreeTimer.SignalName.Timeout);
        LoadLevel(_levelIndex);
        _isAnimating = false;
    }

    private static IReadOnlyList<HexCoord> GetObjectiveCoords(LevelState state)
    {
        return state.Objective switch
        {
            ClearAllOfTypeObjective clearAllOfTypeObjective => state.Board.Coords
                .Where(coord => state.Board.NodeAt(coord).Type == clearAllOfTypeObjective.TargetType)
                .ToArray(),
            _ => Array.Empty<HexCoord>()
        };
    }

    private static string GetObjectiveSummary(LevelState state)
    {
        return state.Objective switch
        {
            ClearAllOfTypeObjective clearAllOfTypeObjective => BuildClearAllSummary(state, clearAllOfTypeObjective),
            _ => "Objective unavailable"
        };
    }

    private static string BuildClearAllSummary(LevelState state, ClearAllOfTypeObjective objective)
    {
        var objectiveCoords = state.Board.Coords
            .Where(coord => state.Board.NodeAt(coord).Type == objective.TargetType)
            .ToArray();
        var cleared = objectiveCoords.Count(coord => state.ClearedCoords.Contains(coord));
        return $"Objective: Clear all {objective.TargetType} nodes ({cleared}/{objectiveCoords.Length})";
    }
}
