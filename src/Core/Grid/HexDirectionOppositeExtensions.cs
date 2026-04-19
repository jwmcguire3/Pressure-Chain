namespace PressureChain.Core.Grid;

public static class HexDirectionOppositeExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return direction switch
        {
            HexDirection.E => HexDirection.W,
            HexDirection.NE => HexDirection.SW,
            HexDirection.NW => HexDirection.SE,
            HexDirection.W => HexDirection.E,
            HexDirection.SW => HexDirection.NE,
            HexDirection.SE => HexDirection.NW,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}
