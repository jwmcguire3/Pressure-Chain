using PressureChain.Core.Grid;

namespace PressureChain.Core.Actions;

public sealed record VentRedirectAction(HexCoord Target, HexDirection NewFacing) : PlayerAction;
