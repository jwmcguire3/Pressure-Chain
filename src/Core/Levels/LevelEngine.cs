using PressureChain.Core.Actions;
using PressureChain.Core.Board;
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
                    state.Objective,
                    state.PoppedTargetCoords,
                    chainResolution: null) ? LevelStatus.Won : LevelStatus.Lost
            };
        }

        var movesRemaining = state.MovesRemaining - 1;
        var actionOutcome = _actionResolver.ApplyDetailed(state.Board, action);
        var poppedTargetCoords = UpdatePoppedTargets(state.PoppedTargetCoords, state.Objective, actionOutcome.ChainResolution);
        var objectiveMet = EvaluateObjective(state.Objective, poppedTargetCoords, actionOutcome.ChainResolution);
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
            ScoreAccumulated = scoreAccumulated,
            PoppedTargetCoords = poppedTargetCoords
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
        LevelObjective objective,
        IReadOnlyList<PressureChain.Core.Grid.HexCoord> poppedTargetCoords,
        PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        return objective switch
        {
            TaggedClusterObjective taggedClusterObjective => AreTaggedClustersPopped(taggedClusterObjective, poppedTargetCoords, chainResolution),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported level objective.")
        };
    }

    private static IReadOnlyList<PressureChain.Core.Grid.HexCoord> UpdatePoppedTargets(
        IReadOnlyList<PressureChain.Core.Grid.HexCoord> existingPoppedTargetCoords,
        LevelObjective objective,
        PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        if (chainResolution is null)
        {
            return existingPoppedTargetCoords;
        }

        return objective switch
        {
            TaggedClusterObjective taggedClusterObjective => existingPoppedTargetCoords
                .Concat(chainResolution.Value.Waves
                    .SelectMany(wave => wave)
                    .Select(burst => burst.Origin)
                    .Where(taggedClusterObjective.TargetCoords.Contains))
                .Distinct()
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported level objective.")
        };
    }

    private static bool AreTaggedClustersPopped(
        TaggedClusterObjective objective,
        IReadOnlyList<PressureChain.Core.Grid.HexCoord> poppedTargetCoords,
        PressureChain.Core.Chains.ChainResolution? chainResolution)
    {
        _ = chainResolution;
        var poppedTargetSet = poppedTargetCoords.ToHashSet();
        return objective.TargetCoords.All(poppedTargetSet.Contains);
    }
}
