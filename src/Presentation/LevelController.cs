using Godot;
using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using PressureChain.Core.Levels;
using PressureChain.Core.Telemetry;

namespace PressureChain.Presentation;

public partial class LevelController : Node2D
{
    private readonly ActionResolver _actionResolver = new(new ChainResolver());
    private IActionLogger _actionLogger = null!;
    private LevelEngine _levelEngine = null!;
    private LevelState _levelState = null!;
    private BoardNode _boardNode = null!;
    private Label _statusLabel = null!;
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

        _levelState = Phase1TestLevelFactory.Create();
        _actionLogger.LogLevelStart(_levelState);
        _boardNode.DisplayBoard(_levelState.Board, GetTaggedCoords(_levelState.Objective), _levelState.PoppedTargetCoords);
        UpdateStatus();
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
            _boardNode.DisplayBoard(_levelState.Board, GetTaggedCoords(_levelState.Objective), _levelState.PoppedTargetCoords);
            UpdateStatus();
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
        var objectiveProgress = _levelState.Objective is TaggedClusterObjective taggedClusterObjective
            ? $"{_levelState.PoppedTargetCoords.Count(coord => taggedClusterObjective.TargetCoords.Contains(coord))}/{taggedClusterObjective.TargetCoords.Count}"
            : "?";

        var statusText = _levelState.Status switch
        {
            LevelStatus.Won => $"Moves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}   Cluster: {objectiveProgress}\nLevel complete",
            LevelStatus.Lost => $"Moves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}   Cluster: {objectiveProgress}\nNo moves left",
            _ => $"Moves: {_levelState.MovesRemaining}   Score: {_levelState.ScoreAccumulated}   Cluster: {objectiveProgress}"
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            statusText = $"{statusText}\n{message}";
        }

        _statusLabel.Text = statusText;
    }

    private static IReadOnlyList<HexCoord> GetTaggedCoords(LevelObjective objective)
    {
        return objective switch
        {
            TaggedClusterObjective taggedClusterObjective => taggedClusterObjective.TargetCoords,
            _ => Array.Empty<HexCoord>()
        };
    }
}
