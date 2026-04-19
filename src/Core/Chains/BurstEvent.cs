using PressureChain.Core.Grid;

namespace PressureChain.Core.Chains;

public readonly record struct BurstEvent(HexCoord Origin, int Wave, bool WasChained);
