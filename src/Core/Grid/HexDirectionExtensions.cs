namespace PressureChain.Core.Grid;

public static class HexDirectionExtensions
{
    public static HexCoord Offset(this HexDirection direction)
    {
        return HexCoord.Directions[(int)direction];
    }
}
