using Godot;
using PressureChain.Core.Actions;
using PressureChain.Core.Chains;
using PressureChain.Core.Levels;

namespace PressureChain.Presentation;

public partial class LevelController : Node2D
{
    private readonly ActionResolver _actionResolver = new(new ChainResolver());
    private LevelEngine _levelEngine = null!;
    private LevelState _levelState = null!;
    private BoardNode _boardNode = null!;
    private Label _statusLabel = null!;
    private bool _isAnimating;

    public override void _Ready()
    {
        _levelEngine = new LevelEngine(_actionResolver);
        _boardNode = GetNode<BoardNode>("BoardNode");
        _statusLabel = GetNode<Label>("CanvasLayer/StatusLabel");
        _boardNode.ActionRequested += OnActionRequested;

        _levelState = Phase1TestLevelFactory.Create();
        _boardNode.DisplayBoard(_levelState.Board);
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
            _boardNode.DisplayBoard(_levelState.Board);
            UpdateStatus();
        }
        catch (InvalidActionException exception)
        {
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
