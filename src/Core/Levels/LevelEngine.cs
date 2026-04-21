using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using PressureChain.Core.Telemetry;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public sealed class LevelEngine
{
    private readonly ActionResolver _actionResolver;
    private readonly IActionLogger _actionLogger;

    public LevelEngine(ActionResolver actionResolver, IActionLogger? actionLogger = null)
    {
        _actionResolver = actionResolver ?? throw new ArgumentNullException(nameof(actionResolver));
        _actionLogger = actionLogger ?? new NullActionLogger();
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
            return state with
            {
                Status = EvaluateObjective(
                    state.Board,
                    state.Objective,
                    state.ClearedCoords) ? LevelStatus.Won : LevelStatus.Lost
            };
        }

        var movesRemaining = state.MovesRemaining - 1;
        var actionOutcome = _actionResolver.ApplyDetailed(state.Board, action);
        var clearedCoords = UpdateClearedCoords(
            state.Board,
            actionOutcome.Board,
            state.ClearedCoords,
            state.Objective,
            actionOutcome.ChainResolution);
        var objectiveMet = EvaluateObjective(actionOutcome.Board, state.Objective, clearedCoords);
        var boardAfterTick = objectiveMet ? actionOutcome.Board : PressureTick.Apply(actionOutcome.Board);
        var scoreAccumulated = state.ScoreAccumulated;
        if (actionOutcome.ChainResolution is not null)
        {
            scoreAccumulated += ChainScorer.Score(actionOutcome.ChainResolution.Value);
        }

        var nextState = state with
        {
            Board = boardAfterTick,
            MovesRemaining = movesRemaining,
            ScoreAccumulated = scoreAccumulated,
            ClearedCoords = clearedCoords
        };

        nextState = nextState with
        {
            Status = EvaluateStatus(nextState, objectiveMet)
        };

        _actionLogger.LogAction(action, state, nextState);
        if (nextState.Status is LevelStatus.Won or LevelStatus.Lost)
        {
            _actionLogger.LogLevelEnd(nextState.Status, nextState.ScoreAccumulated);
        }

        return nextState;
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

    private static bool EvaluateObjective(
        GameBoard board,
        LevelObjective objective,
        IReadOnlyList<HexCoord> clearedCoords)
    {
        return objective switch
        {
            ClearAllOfTypeObjective clearAllOfTypeObjective => AreTargetNodesCleared(board, clearAllOfTypeObjective, clearedCoords),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported level objective.")
        };
    }

    private static IReadOnlyList<HexCoord> UpdateClearedCoords(
        GameBoard boardBeforeAction,
        GameBoard boardAfterAction,
        IReadOnlyList<HexCoord> existingClearedCoords,
        LevelObjective objective,
        PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        var clearedCoords = existingClearedCoords.ToHashSet();

        if (objective is not ClearAllOfTypeObjective clearAllOfTypeObjective)
        {
            throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported level objective.");
        }

        var targetType = clearAllOfTypeObjective.TargetType;
        foreach (var coord in boardAfterAction.Coords)
        {
            var nodeBefore = boardBeforeAction.NodeAt(coord);
            var nodeAfter = boardAfterAction.NodeAt(coord);
            if (nodeAfter.Type != targetType)
            {
                continue;
            }

            if (nodeAfter.Pressure == 0 && nodeBefore.Pressure > 0)
            {
                clearedCoords.Add(coord);
            }
        }

        if (chainResolution is not null)
        {
            foreach (var burst in chainResolution.Value.Waves.SelectMany(wave => wave))
            {
                if (boardBeforeAction.NodeAt(burst.Origin).Type == targetType ||
                    boardAfterAction.NodeAt(burst.Origin).Type == targetType)
                {
                    clearedCoords.Add(burst.Origin);
                }
            }
        }

        return clearedCoords.ToArray();
    }

    private static bool AreTargetNodesCleared(
        GameBoard board,
        ClearAllOfTypeObjective objective,
        IReadOnlyList<HexCoord> clearedCoords)
    {
        var clearedSet = clearedCoords.ToHashSet();
        return board.Coords
            .Where(coord => board.NodeAt(coord).Type == objective.TargetType)
            .All(clearedSet.Contains);
    }
}
