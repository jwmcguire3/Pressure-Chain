using PressureChain.Core.Grid;

namespace PressureChain.Core.Actions;

public sealed record MergeAction(HexCoord A, HexCoord B) : PlayerAction;
