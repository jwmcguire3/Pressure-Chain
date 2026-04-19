using PressureChain.Core.Board;

namespace PressureChain.Core.Chains;

public readonly record struct ChainResolution(
    IReadOnlyList<IReadOnlyList<BurstEvent>> Waves,
    PressureChain.Core.Board.Board FinalBoard,
    int TotalBurstCount);
