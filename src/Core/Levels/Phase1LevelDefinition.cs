using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public sealed record Phase1LevelDefinition(
    string Id,
    string DisplayName,
    GameBoard Board,
    int MoveCap,
    LevelObjective Objective,
    int SolverMaxDepth,
    int MinimumDistinctSolutions,
    IReadOnlyList<PressureChain.Core.Actions.PlayerAction> DemonstrationActions,
    int MinimumDemonstratedWaveCount)
{
    public LevelState CreateInitialState()
    {
        return new LevelState(
            Board: Board,
            MovesRemaining: MoveCap,
            Objective: Objective,
            ScoreAccumulated: 0,
            ClearedCoords: Array.Empty<HexCoord>(),
            Status: LevelStatus.InProgress);
    }
}
