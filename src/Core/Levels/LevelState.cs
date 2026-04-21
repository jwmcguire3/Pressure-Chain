using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Levels;

public sealed record LevelState(
    GameBoard Board,
    int MovesRemaining,
    LevelObjective Objective,
    int ScoreAccumulated,
    IReadOnlyList<PressureChain.Core.Grid.HexCoord> ClearedCoords,
    LevelStatus Status);
