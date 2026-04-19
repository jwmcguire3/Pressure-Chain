using PressureChain.Core.Board;

namespace PressureChain.Core.Levels;

public sealed record ClearAllOfTypeObjective(NodeType TargetType) : LevelObjective;
