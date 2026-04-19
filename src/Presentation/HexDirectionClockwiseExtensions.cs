using PressureChain.Core.Grid;

namespace PressureChain.Presentation;

internal static class HexDirectionClockwiseExtensions
{
    public static HexDirection RotateClockwise(this HexDirection direction)
    {
        return direction switch
        {
            HexDirection.E => HexDirection.SE,
            HexDirection.SE => HexDirection.SW,
            HexDirection.SW => HexDirection.W,
            HexDirection.W => HexDirection.NW,
            HexDirection.NW => HexDirection.NE,
            HexDirection.NE => HexDirection.E,
            _ => direction
        };
    }
}
