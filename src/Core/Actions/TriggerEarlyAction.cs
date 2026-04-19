using PressureChain.Core.Grid;

namespace PressureChain.Core.Actions;

public sealed record TriggerEarlyAction(HexCoord Target) : PlayerAction;
