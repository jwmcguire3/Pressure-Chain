using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public sealed record Phase1LevelDefinition(
    string Id,
    string DisplayName,
    GameBoard Board,
    int MoveCap,
    TaggedClusterObjective Objective,
    int SolverMaxDepth,
    int MinimumDistinctSolutions)
{
    public LevelState CreateInitialState()
    {
        return new LevelState(
            Board: Board,
            MovesRemaining: MoveCap,
            Objective: Objective,
            ScoreAccumulated: 0,
            PoppedTargetCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);
    }
}
