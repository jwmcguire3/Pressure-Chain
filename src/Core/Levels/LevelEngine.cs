using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public sealed class LevelEngine
{
    private readonly ActionResolver _actionResolver;

    public LevelEngine(ActionResolver actionResolver)
    {
        _actionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
    }

    public LevelState PlayAction(LevelState state, PlayerAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (state.Status != LevelStatus.InProgress)
        {
            return state;
        }

        if (state.MovesRemaining <= 0)
        {
            return state with { Status = EvaluateObjective(state.Board, state.Objective, chainResolution: null) ? LevelStatus.Won : LevelStatus.Lost };
        }

        var movesRemaining = state.MovesRemaining - 1;
        var actionOutcome = _actionResolver.ApplyDetailed(state.Board, action);
        var objectiveBoard = actionOutcome.ChainResolution?.FinalBoard ?? actionOutcome.Board;
        var objectiveMet = EvaluateObjective(objectiveBoard, state.Objective, actionOutcome.ChainResolution);
        var boardAfterTick = PressureTick.Apply(actionOutcome.Board);
        var scoreAccumulated = state.ScoreAccumulated;
        if (actionOutcome.ChainResolution is not null)
        {
            scoreAccumulated += ChainScorer.Score(actionOutcome.ChainResolution.Value);
        }

        var nextState = state with
        {
            Board = boardAfterTick,
            MovesRemaining = movesRemaining,
            ScoreAccumulated = scoreAccumulated
        };

        return nextState with
        {
            Status = EvaluateStatus(nextState, objectiveMet)
        };
    }

    private static LevelStatus EvaluateStatus(LevelState state, bool objectiveMet)
    {
        if (objectiveMet)
        {
            return LevelStatus.Won;
        }

        return state.MovesRemaining == 0
            ? LevelStatus.Lost
            : LevelStatus.InProgress;
    }

    private static bool EvaluateObjective(GameBoard board, LevelObjective objective, PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        return objective switch
        {
            ClearAllOfTypeObjective clearAllOfTypeObjective => AreAllNodesOfTypeCleared(board, clearAllOfTypeObjective.TargetType, chainResolution),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported level objective.")
        };
    }

    private static bool AreAllNodesOfTypeCleared(GameBoard board, NodeType targetType, PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        var targetCoords = board.Coords
            .Where(coord => board.NodeAt(coord).Type == targetType)
            .ToArray();

        if (chainResolution is not null)
        {
            var burstCoords = chainResolution.Value.Waves
                .SelectMany(wave => wave)
                .Select(burst => burst.Origin)
                .ToHashSet();

            return targetCoords.All(burstCoords.Contains);
        }

        return targetCoords
            .Select(board.NodeAt)
            .All(node => node.Pressure == 0);
    }
}
