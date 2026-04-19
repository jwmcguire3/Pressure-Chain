using PressureChain.Core.Chains;
using GameBoard = PressureChain.Core.Board.Board;

namespace PressureChain.Core.Actions;

public sealed record ActionOutcome(
    GameBoard Board,
    ChainResolution? ChainResolution);
